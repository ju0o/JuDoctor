using MainPCDoctor.Core.Diagnosis;
using MainPCDoctor.Core.Diagnosis.Rules;
using MainPCDoctor.Core.Models;
using Xunit;

namespace MainPCDoctor.Core.Tests;

public class DiagnosisEngineTests
{
    private static SystemSnapshot MakeSnapshot(float cpu, float ramFree, float ramTotal, DateTimeOffset? at = null)
    {
        var cpuMetrics = new CpuMetrics(cpu, Array.Empty<float>(), 8, null, null, null);
        var mem        = new MemoryMetrics(ramTotal, ramFree, ramTotal - ramFree, ramTotal * 1.5f, false);
        return new SystemSnapshot(at ?? DateTimeOffset.UtcNow, cpuMetrics, mem, [], null, null);
    }

    [Fact]
    public void Clean_WhenNoSignals()
    {
        var engine = new DiagnosisEngine();
        engine.SetTotalPhysicalRam(16f);

        var window = Enumerable.Range(0, 30)
            .Select(i => MakeSnapshot(20f, 8f, 16f, DateTimeOffset.UtcNow.AddSeconds(-i * 10)))
            .ToList();

        var result = engine.Evaluate(window);
        Assert.Null(result.Primary);
    }

    [Fact]
    public void RamPressure_Confirmed_WhenBelowThresholdFor120s()
    {
        var thresholds = DiagnosisThresholds.Default;
        var rule       = new RamPressureRule();
        float total    = 8f;
        // Free = 0.3 GB (below max(2.0, 8×0.05=0.4) = 2.0 GB threshold)
        var now = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i =>
            {
                var cpu = new CpuMetrics(10f, Array.Empty<float>(), 8, null, null, null);
                var mem = new MemoryMetrics(total, 0.3f, total - 0.3f, total * 1.5f, true);
                // newest-last: i=0 is oldest (now-140s), i=14 is newest (now)
                return new SystemSnapshot(now.AddSeconds((i - 14) * 10), cpu, mem, [], null, null);
            })
            .ToList();

        var finding = rule.Evaluate(window, thresholds, total);
        Assert.NotNull(finding);
        Assert.Equal(DiagnosisOutcome.Confirmed, finding!.Outcome);
        Assert.Equal(2, finding.NotificationLevel);
    }

    [Fact]
    public void CpuBottleneck_RequiresFull300sWindow()
    {
        var rule   = new CpuBottleneckRule();
        var thresholds = DiagnosisThresholds.Default;
        // Only 60s of data — should not confirm
        var window = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var cpu = new CpuMetrics(97f, Enumerable.Repeat(97f, 8).ToArray(), 8, null, null, null);
                var mem = new MemoryMetrics(16f, 12f, 4f, 24f, false);
                var disk = new DiskMetrics("0 C:", 10f, 2f, 0f);
                return new SystemSnapshot(DateTimeOffset.UtcNow.AddSeconds(-i * 10), cpu, mem, [disk], null, null);
            })
            .ToList();

        var finding = rule.Evaluate(window, thresholds, 16f);
        // 70s window < 300s gate — should not confirm
        Assert.True(finding == null || finding.Outcome != DiagnosisOutcome.Confirmed);
    }
}
