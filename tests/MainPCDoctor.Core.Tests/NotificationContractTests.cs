using MainPCDoctor.Core.Diagnosis.Rules;
using MainPCDoctor.Core.Models;
using Xunit;

namespace MainPCDoctor.Core.Tests;

/// <summary>
/// Notification contract: verifies which rules can produce which notification levels.
/// V1 contract: only RamPressureRule may produce Level > 0 for operational alerts.
/// Level 4 (upgrade recommendation) is controlled by UpgradeRecommendationEngine, not rules.
/// </summary>
public class NotificationContractTests
{
    // CpuMetrics order: (total, perCore, cores, BaseClock, CurrentClock, Temp)
    private static CpuMetrics HotCpu()
        => new(97f, Enumerable.Repeat(97f, 8).ToArray(), 8, 4000f, 3500f, 95f);

    private static MemoryMetrics LowMem(float total = 16f)
        => new(total, 0.3f, total * 1.2f, total * 1.5f, true);

    private static List<SystemSnapshot> Window31()
    {
        var now = DateTimeOffset.UtcNow;
        return Enumerable.Range(0, 31)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 30) * 10),
                HotCpu(), LowMem(), [], null, null))
            .ToList();
    }

    [Fact]
    public void CpuBottleneckRule_AlwaysLevel0()
    {
        var rule    = new CpuBottleneckRule();
        var perCore = Enumerable.Repeat(97f, 8).ToArray();
        var now     = DateTimeOffset.UtcNow;
        var window  = Enumerable.Range(0, 31)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 30) * 10),
                new CpuMetrics(97f, perCore, 8, null, null, null),
                new MemoryMetrics(16f, 12f, 4f, 24f, false),
                [new DiskMetrics("0 C:", 5f, null, 0f)],
                null, null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void RamPressureRule_Level2()
    {
        var rule   = new RamPressureRule();
        float total = 8f;
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 14) * 10),
                new CpuMetrics(20f, [], 8, null, null, null),
                new MemoryMetrics(total, 0.3f, total * 1.2f, total * 1.5f, true),
                [], null, null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, total);
        Assert.NotNull(result);
        Assert.Equal(2, result!.NotificationLevel);
    }

    [Fact]
    public void DiskBottleneckRule_AlwaysLevel0_WhenConfirmed()
    {
        var rule   = new DiskBottleneckRule();
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 10)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 9) * 10),
                new CpuMetrics(20f, [], 8, null, null, null),
                new MemoryMetrics(16f, 12f, 4f, 24f, false),
                [new DiskMetrics("0 C:", 95f, 100f, 7f)],
                null, null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null && result.Outcome == DiagnosisOutcome.Confirmed)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void GpuComputeRule_AlwaysLevel0()
    {
        var rule   = new GpuComputeRule();
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 13)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 12) * 10),
                new CpuMetrics(20f, [], 8, null, null, null),
                new MemoryMetrics(16f, 12f, 4f, 24f, false),
                [],
                new GpuMetrics(95f, 4f, 8f, null),
                null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void VramPressureRule_AlwaysLevel0_WhenConfirmed()
    {
        var rule   = new VramPressureRule();
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 19)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 18) * 10),
                new CpuMetrics(20f, [], 8, null, null, null),
                new MemoryMetrics(16f, 12f, 4f, 24f, false),
                [],
                new GpuMetrics(80f, 7.5f, 8f, null),
                null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void ThermalThrottlingRule_AlwaysLevel0_WhenConfirmed()
    {
        var rule   = new ThermalThrottlingRule();
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 7)
            .Select(i => new SystemSnapshot(
                now.AddSeconds((i - 6) * 10),
                new CpuMetrics(90f, [], 8, 4000f, 3500f, 95f),  // BaseClock, CurrentClock, Temp
                new MemoryMetrics(16f, 12f, 4f, 24f, false),
                [], null, null))
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void ProcessAnomalyRule_AlwaysLevel0()
    {
        var rule   = new ProcessAnomalyRule();
        var now    = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i =>
            {
                var proc = new ProcessMetric("leak.exe", 50f, 600f + i * 10f);
                var procs = new ProcessSummary([proc], [proc], now.AddSeconds((i - 14) * 10));
                return new SystemSnapshot(
                    now.AddSeconds((i - 14) * 10),
                    new CpuMetrics(20f, [], 8, null, null, null),
                    new MemoryMetrics(16f, 12f, 4f, 24f, false),
                    [], null, procs);
            })
            .ToList();

        var result = rule.Evaluate(window, Core.Diagnosis.DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }
}
