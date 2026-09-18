using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Incidents;
using MainPCDoctor.Core.Models;
using MainPCDoctor.Core.Recommendations;
using MainPCDoctor.Desktop.Background;
using MainPCDoctor.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace MainPCDoctor.Integration.Tests;

public class ResourceGovernorWorkerTests
{
    [Theory]
    [InlineData("normal")]
    [InlineData("sustained")]
    [InlineData("recovered")]
    [InlineData("failure")]
    public async Task WorkerPersistsHealthAcrossMultipleMinutes(string scenario)
    {
        var path = Path.Combine(Path.GetTempPath(), $"governor-{Guid.NewGuid():N}.db");
        var clock = new ManualClock();
        var db = new DatabaseFactory(path);
        var buffer = new RollingMetricsBuffer(timeProvider: clock);
        var collector = Substitute.For<ISystemMetricsCollector>();
        int cycles = 0, full = 0, minimum = 0;
        const int cycleLimit = 12;
        var governor = new ResourceGovernor(() =>
        {
            if (scenario == "failure" && cycles < 7) throw new InvalidOperationException("Controlled read failure");
            bool high = scenario == "sustained" || (scenario == "recovered" && cycles >= 2 && cycles < 7);
            return (high ? 450L : 200L) * 1_048_576;
        });
        SystemSnapshot Sample(bool minimal)
        {
            cycles++;
            if (minimal) minimum++; else full++;
            return new(clock.GetUtcNow(), new CpuMetrics(10, [], 4, null, null, null),
                new MemoryMetrics(16, 8, 8, 24, false), [], null, null);
        }
        collector.CollectAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(Sample(false)));
        collector.CollectMinimumAsync(Arg.Any<CancellationToken>()).Returns(_ => Task.FromResult(Sample(true)));
        var diagnosis = Substitute.For<IDiagnosisEngine>();
        diagnosis.Evaluate(Arg.Any<IReadOnlyList<SystemSnapshot>>())
            .Returns(_ => DiagnosisResult.Clean(clock.GetUtcNow()));
        var incidents = Substitute.For<IIncidentStore>();
        var intervals = new List<TimeSpan>();
        var reachedWait = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerNotifications = Substitute.For<INotificationService>();
        var upgradeStateDir = Path.Combine(Path.GetTempPath(), $"upg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(upgradeStateDir);
        using var worker = new MonitoringWorker(collector, diagnosis, buffer,
            new IncidentTracker(incidents, Substitute.For<INotificationService>(), NullLogger<IncidentTracker>.Instance),
            new MetricsAggregator(new SQLiteMetricsStore(db), buffer, NullLogger<MetricsAggregator>.Instance, clock),
            new RetentionManager(db, incidents, NullLogger<RetentionManager>.Instance),
            incidents,
            new UpgradeRecommendationEngine(),
            workerNotifications,
            new UpgradeNotificationState(upgradeStateDir),
            NullLogger<MonitoringWorker>.Instance)
        {
            ReadGovernor = governor.Read,
            DelayAsync = (interval, ct) =>
            {
                if (cycles == cycleLimit)
                {
                    reachedWait.TrySetResult();
                    return Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                intervals.Add(interval);
                clock.Advance(interval);
                return Task.CompletedTask;
            }
        };
        try
        {
            await worker.StartAsync(CancellationToken.None);
            await reachedWait.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(worker.ExecuteTask!.IsCompletedSuccessfully);
            Assert.Equal(cycleLimit, buffer.Count);
            Assert.Equal(cycleLimit, full + minimum);
            diagnosis.Received(full).Evaluate(Arg.Any<IReadOnlyList<SystemSnapshot>>());
            if (scenario == "normal")
            {
                Assert.Equal(0, minimum);
                Assert.Equal(TimeSpan.FromSeconds(10), intervals[0]);
                Assert.All(intervals.Skip(1), x => Assert.Equal(TimeSpan.FromSeconds(30), x));
            }
            else if (scenario == "sustained")
            {
                Assert.Equal(0, full);
                Assert.All(intervals, x => Assert.Equal(TimeSpan.FromSeconds(60), x));
            }
            else
            {
                Assert.True(minimum >= 5);
                Assert.True(full > 0);
                Assert.Equal(TimeSpan.FromSeconds(30), intervals[^1]);
                Assert.Equal("normal", buffer.GetAll()[^1].MonitoringState);
            }
            using var conn = db.Open();
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT sampled_at, heartbeat_at, monitoring_state, self_private_bytes, mem_total_gb, mem_available_gb_avg, cpu_avg_pct FROM metrics_samples_1min ORDER BY id";
            using var reader = command.ExecuteReader();
            var times = new List<DateTimeOffset>();
            var states = new List<string>();
            while (reader.Read())
            {
                var at = DateTimeOffset.Parse(reader.GetString(0));
                times.Add(at);
                Assert.Equal(at, DateTimeOffset.Parse(reader.GetString(1)));
                string state = reader.GetString(2);
                states.Add(state);
                Assert.Equal(state == "governor_read_failed", reader.IsDBNull(3));
                Assert.Equal(16d, reader.GetDouble(4));
                Assert.Equal(8d, reader.GetDouble(5));
                Assert.Equal(10d, reader.GetDouble(6));
            }
            Assert.True(times.Count >= 5);
            Assert.Equal(times.Count, times.Distinct().Count());
            Assert.True((clock.GetUtcNow() - times[^1]).TotalSeconds <= 30);
            Assert.All(times.Zip(times.Skip(1)), pair => Assert.InRange((pair.Second - pair.First).TotalSeconds, 60, 90));
            if (scenario == "sustained") Assert.All(states, s => Assert.Equal("throttled", s));
            if (scenario == "failure") Assert.Contains("governor_read_failed", states);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            foreach (var suffix in new[] { "", "-wal", "-shm" }) File.Delete(path + suffix);
        }
    }

    [Fact]
    public void BoundaryHysteresisAndFailureRecovery()
    {
        long bytes = 400L * 1_048_576;
        bool fail = false;
        var governor = new ResourceGovernor(() => fail ? throw new IOException() : bytes);
        Assert.False(governor.Read().Throttled);
        bytes++;
        Assert.True(governor.Read().Throttled);
        foreach (var mib in new[] { 400, 399, 401, 390, 350, 400 })
        {
            bytes = mib * 1_048_576L;
            Assert.True(governor.Read().Throttled);
        }
        bytes = 350L * 1_048_576 - 1;
        Assert.False(governor.Read().Throttled);
        fail = true;
        var fault = governor.Read();
        Assert.True(fault.Throttled);
        Assert.True(fault.ReadFailed);
        Assert.Null(fault.PrivateBytes);
        fail = false;
        Assert.False(governor.Read().Throttled);
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 10)]
    [InlineData(2, 3)]
    public void NormalCadencesUnchanged(int state, int seconds)
        => Assert.Equal(TimeSpan.FromSeconds(seconds), SamplingScheduler.GetInterval((SamplingState)state));

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan interval) => _now += interval;
    }
}
