using MainPCDoctor.Core.Diagnosis;
using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Models;
using Xunit;

namespace MainPCDoctor.Integration.Tests;

public class MonitoringPipelineTests
{
    [Fact]
    public void Buffer_HoldsUpTo600Snapshots()
    {
        var buffer = new RollingMetricsBuffer(capacity: 600);
        for (int i = 0; i < 700; i++)
        {
            var snap = MakeSnapshot(DateTimeOffset.UtcNow.AddSeconds(-i));
            buffer.Add(snap);
        }
        Assert.Equal(600, buffer.GetAll().Count);
    }

    [Fact]
    public void GetRecent_ReturnsWindowByAge()
    {
        var buffer = new RollingMetricsBuffer(capacity: 600);
        var now    = DateTimeOffset.UtcNow;
        for (int i = 0; i < 30; i++)
            buffer.Add(MakeSnapshot(now.AddSeconds(-i * 10)));

        var recent = buffer.GetRecent(seconds: 60);
        Assert.True(recent.Count <= 7);   // at most 7 snapshots in 60s at 10s cadence
        Assert.All(recent, s => Assert.True((now - s.CollectedAt).TotalSeconds <= 60 + 10));
    }

    private static SystemSnapshot MakeSnapshot(DateTimeOffset at)
    {
        var cpu = new CpuMetrics(10f, Array.Empty<float>(), 4, null, null, null);
        var mem = new MemoryMetrics(16f, 8f, 8f, 24f, false);
        return new SystemSnapshot(at, cpu, mem, [], null, null);
    }
}
