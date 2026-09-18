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

public class UpgradeNotificationWorkerTests : IDisposable
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private readonly string _stateDir =
        Path.Combine(Path.GetTempPath(), $"upg-test-{Guid.NewGuid():N}");

    public UpgradeNotificationWorkerTests() => Directory.CreateDirectory(_stateDir);

    public void Dispose()
    {
        try { Directory.Delete(_stateDir, recursive: true); } catch { }
    }

    private UpgradeNotificationState NewState() => new(_stateDir);

    private static IReadOnlyList<Incident> MakeCpuIncidents(int days = 5) =>
        Enumerable.Range(0, days).Select(i => new Incident
        {
            BottleneckType    = BottleneckType.CpuBottleneck,
            Status            = IncidentStatus.Resolved,
            StartedAt         = DateTimeOffset.UtcNow.AddDays(-30 - i),
            ConfirmedAt       = DateTimeOffset.UtcNow.AddDays(-30 - i),
            DurationSeconds   = 1500,     // > 1200 → durationBonus ×2
            NotificationLevel = 0
        }).ToList();

    private static IReadOnlyList<Incident> MakeRamIncidents(int days = 5) =>
        Enumerable.Range(0, days).Select(i => new Incident
        {
            BottleneckType    = BottleneckType.RamPressure,
            Status            = IncidentStatus.Resolved,
            StartedAt         = DateTimeOffset.UtcNow.AddDays(-30 - i),
            ConfirmedAt       = DateTimeOffset.UtcNow.AddDays(-30 - i),
            DurationSeconds   = 1500,
            NotificationLevel = 2
        }).ToList();

    private MonitoringWorker BuildWorker(
        IIncidentStore      store,
        INotificationService notifications,
        UpgradeNotificationState? state = null)
    {
        var collector  = Substitute.For<ISystemMetricsCollector>();
        var diagnosis  = Substitute.For<IDiagnosisEngine>();
        var buffer     = new RollingMetricsBuffer(capacity: 600);
        var tracker    = new IncidentTracker(store, Substitute.For<INotificationService>(), NullLogger<IncidentTracker>.Instance);
        var dbFactory  = new DatabaseFactory(Path.Combine(_stateDir, $"{Guid.NewGuid():N}.db"));
        var aggregator = new MetricsAggregator(new SQLiteMetricsStore(dbFactory), buffer, NullLogger<MetricsAggregator>.Instance);
        var retention  = new RetentionManager(dbFactory, store, NullLogger<RetentionManager>.Instance);

        return new MonitoringWorker(
            collector, diagnosis, buffer, tracker, aggregator, retention,
            store,
            new UpgradeRecommendationEngine(),
            notifications,
            state ?? NewState(),
            NullLogger<MonitoringWorker>.Instance);
    }

    // ── Section 1: notification fires correctly ──────────────────────────────

    [Fact]
    public async Task CheckAndNotify_CallsLevel4_WhenCpuRecommendationAvailable()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>());
        notifications.DidNotReceive().NotifyLevel4Upgrade("RAM", Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAndNotify_CallsLevel4_WhenRamRecommendationAvailable()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeRamIncidents());
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        notifications.Received(1).NotifyLevel4Upgrade("RAM", Arg.Any<string>());
        notifications.DidNotReceive().NotifyLevel4Upgrade("CPU", Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAndNotify_CallsBoth_WhenBothComponentsRecommend()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents().Concat(MakeRamIncidents()).ToList());
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        // Both components eligible, both should fire — they are distinct, not spam.
        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>());
        notifications.Received(1).NotifyLevel4Upgrade("RAM", Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAndNotify_DoesNotNotify_WhenNoIncidents()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Incident>());
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        notifications.DidNotReceive().NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAndNotify_DoesNotNotify_WhenScoreTooLow()
    {
        // Single incident on 1 day, score < 20 → Insufficient
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<Incident>
            {
                new() { BottleneckType = BottleneckType.CpuBottleneck,
                         Status = IncidentStatus.Resolved,
                         StartedAt = DateTimeOffset.UtcNow.AddDays(-20),
                         ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-20),
                         DurationSeconds = 300 }
            });
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        notifications.DidNotReceive().NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Section 2: failure resilience ───────────────────────────────────────

    [Fact]
    public async Task CheckAndNotify_DoesNotCrash_WhenStoreThrows()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<Incident>>(_ => throw new InvalidOperationException("DB error"));
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        // Must not throw; error is logged and swallowed.
        await worker.CheckAndNotifyUpgradeAsync();

        notifications.DidNotReceive().NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task CheckAndNotify_DoesNotCrash_WhenNotificationServiceThrows()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();
        notifications.When(n => n.NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>()))
            .Do(_ => throw new InvalidOperationException("tray error"));
        using var worker = BuildWorker(store, notifications);

        // Notification service failure must not propagate.
        await worker.CheckAndNotifyUpgradeAsync();
    }

    // ── Section 3: deduplication — same recommendation does not repeat ───────

    [Fact]
    public async Task SameRecommendation_DoesNotRenotify_WithinReminderInterval()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();
        var state = NewState();
        using var worker  = BuildWorker(store, notifications, state);

        // First check: notifies.
        await worker.CheckAndNotifyUpgradeAsync();
        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>());

        // Second check same day (e.g. same instances reused): must not notify again.
        await worker.CheckAndNotifyUpgradeAsync();
        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>()); // still exactly 1
    }

    [Fact]
    public async Task SameRecommendation_Renotifies_AfterReminderInterval()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();
        var state = NewState();

        // Seed state as if notification was sent 8 days ago.
        state.RecordNotification("CPU", ConfidenceLevel.High);
        // Manually age the notification by writing a backdated entry.
        var oldDate = DateTimeOffset.UtcNow.AddDays(-8);
        var stateFile = Path.Combine(_stateDir, "upgrade-state.json");
        var json = File.ReadAllText(stateFile);
        json = json.Replace(
            DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            oldDate.ToString("yyyy-MM-dd"));
        // Replace notifiedAt timestamp to 8 days ago:
        json = System.Text.RegularExpressions.Regex.Replace(
            json, @"""notifiedAt""\s*:\s*""[^""]+""",
            $"\"notifiedAt\":\"{oldDate:O}\"");
        File.WriteAllText(stateFile, json);

        // Reload state from the updated file.
        var agedState = NewState(); // re-reads from same dir
        using var worker = BuildWorker(store, notifications, agedState);

        await worker.CheckAndNotifyUpgradeAsync();

        // 8 days > 7-day reminder interval → should notify again.
        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>());
    }

    [Fact]
    public void ConfidenceIncrease_AlwaysTriggersRenotification()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();
        var state = NewState();

        // Seed state as if CPU was notified at Low confidence just now.
        state.RecordNotification("CPU", ConfidenceLevel.Low);

        // Any strictly-higher confidence triggers immediate re-notification.
        Assert.True(state.ShouldNotify("CPU", ConfidenceLevel.Medium));  // Medium > Low
        Assert.True(state.ShouldNotify("CPU", ConfidenceLevel.High));    // High > Low

        // Same or lower confidence → blocked within the reminder interval.
        Assert.False(state.ShouldNotify("CPU", ConfidenceLevel.Low));
        Assert.False(state.ShouldNotify("CPU", ConfidenceLevel.Insufficient));
    }

    // ── Section 4: restart does not bypass deduplication ────────────────────

    [Fact]
    public async Task Restart_DoesNotRenotify_WhenAlreadyNotifiedToday()
    {
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(MakeCpuIncidents());
        var notifications = Substitute.For<INotificationService>();

        // Simulate first process run.
        using (var worker = BuildWorker(store, notifications))
            await worker.CheckAndNotifyUpgradeAsync();

        notifications.Received(1).NotifyLevel4Upgrade("CPU", Arg.Any<string>());
        notifications.ClearReceivedCalls();

        // Simulate process restart: new worker, same state dir → state persisted on disk.
        using (var worker = BuildWorker(store, notifications))
            await worker.CheckAndNotifyUpgradeAsync();

        // Persistent deduplication must prevent a second notification on same day.
        notifications.DidNotReceive().NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>());
    }

    // ── Section 5: daily scheduling state ────────────────────────────────────

    [Fact]
    public void UpgradeState_WasCheckedToday_FalseInitially()
    {
        var state = NewState();
        Assert.False(state.WasCheckedToday());
    }

    [Fact]
    public void UpgradeState_WasCheckedToday_TrueAfterRecordCheck()
    {
        var state = NewState();
        state.RecordCheck();
        Assert.True(state.WasCheckedToday());
    }

    [Fact]
    public void UpgradeState_WasCheckedToday_FalseForYesterday()
    {
        var state = NewState();
        // Record as if check happened yesterday.
        state.RecordCheck(today: DateTime.Today.AddDays(-1));
        // From today's perspective, check was NOT done today.
        Assert.False(state.WasCheckedToday(today: DateTime.Today));
    }

    [Fact]
    public void UpgradeState_WasCheckedToday_PersistsAcrossReload()
    {
        NewState().RecordCheck(); // write to disk
        var reloaded = NewState(); // re-read from same dir
        Assert.True(reloaded.WasCheckedToday());
    }

    [Fact]
    public void UpgradeState_CorruptFile_ReturnsCleanState()
    {
        var stateFile = Path.Combine(_stateDir, "upgrade-state.json");
        File.WriteAllText(stateFile, "{ not valid json !!!");

        var state = NewState(); // must not throw
        Assert.False(state.WasCheckedToday());
        Assert.True(state.ShouldNotify("CPU", ConfidenceLevel.Low));
    }

    // ── Section 6: 14-day gate remains enforced ──────────────────────────────

    [Fact]
    public async Task Gate14Days_NeverNotifies_WithObservationWindowUnder14Days()
    {
        // Observation window starts 90 days ago, but all incidents are from yesterday.
        // distinctDays=1 → score too low → Insufficient → no recommendation.
        var store = Substitute.For<IIncidentStore>();
        store.GetByDateRangeAsync(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new List<Incident>
            {
                new() { BottleneckType = BottleneckType.CpuBottleneck,
                         Status = IncidentStatus.Resolved,
                         StartedAt = DateTimeOffset.UtcNow.AddDays(-1),
                         ConfirmedAt = DateTimeOffset.UtcNow.AddDays(-1),
                         DurationSeconds = 1500 }
            });
        var notifications = Substitute.For<INotificationService>();
        using var worker  = BuildWorker(store, notifications);

        await worker.CheckAndNotifyUpgradeAsync();

        notifications.DidNotReceive().NotifyLevel4Upgrade(Arg.Any<string>(), Arg.Any<string>());
    }
}
