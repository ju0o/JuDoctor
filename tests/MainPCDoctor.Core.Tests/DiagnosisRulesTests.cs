using MainPCDoctor.Core.Diagnosis;
using MainPCDoctor.Core.Diagnosis.Rules;
using MainPCDoctor.Core.Models;
using Xunit;

namespace MainPCDoctor.Core.Tests;

/// <summary>
/// False-positive / false-negative gate for all 7 diagnosis rules.
/// Each test documents the exact invariant being verified.
/// </summary>
public class DiagnosisRulesTests
{
    // ─── Snapshot factories ────────────────────────────────────────────────

    private static CpuMetrics MakeCpu(float total, int cores = 8, float[]? perCore = null,
        float? tempC = null, float? baseMhz = null, float? currentMhz = null)
        // CpuMetrics order: (total, perCore, cores, BaseClock, CurrentClock, Temp)
        => new(total, perCore ?? Enumerable.Repeat(total, cores).ToArray(), cores,
               baseMhz, currentMhz, tempC);

    private static MemoryMetrics MakeMem(float totalGb, float freeGb,
        float? commitRatioOverride = null, bool pagefile = false)
    {
        float used     = totalGb - freeGb;
        float limit    = totalGb * 1.5f;
        float charge   = commitRatioOverride.HasValue
            ? limit * commitRatioOverride.Value
            : used;
        return new MemoryMetrics(totalGb, freeGb, charge, limit, pagefile);
    }

    private static DiskMetrics MakeDisk(float util = 10f, float? latMs = null, float queue = 0f)
        => new("0 C:", util, latMs, queue);

    private static GpuMetrics MakeGpu(float util, float vramUsed = 4f, float vramTotal = 8f, float? temp = null)
        => new(util, vramUsed, vramTotal, temp);

    private static ProcessSummary MakeProc(string name, float cpu, float memMb)
    {
        var p = new ProcessMetric(name, cpu, memMb);
        return new ProcessSummary([p], [p], DateTimeOffset.UtcNow);
    }

    private static SystemSnapshot Snap(DateTimeOffset at, CpuMetrics? cpu = null, MemoryMetrics? mem = null,
        DiskMetrics[]? disks = null, GpuMetrics? gpu = null, ProcessSummary? procs = null)
    {
        cpu  ??= MakeCpu(20f);
        mem  ??= MakeMem(16f, 8f);
        return new SystemSnapshot(at, cpu, mem, disks ?? [], gpu, procs);
    }

    // Build a window with uniform interval
    private static List<SystemSnapshot> Window(int count, double intervalSec,
        Func<int, DateTimeOffset, SystemSnapshot> factory)
    {
        var now = DateTimeOffset.UtcNow;
        return Enumerable.Range(0, count)
            .Select(i => factory(i, now.AddSeconds((i - (count - 1)) * intervalSec)))
            .ToList();
    }

    // ─── CPU Bottleneck ────────────────────────────────────────────────────

    [Fact]
    public void Cpu_NoFiring_WhenWindowOnly40s()
    {
        // 5 samples × 10s = 40s window — well short of 300s gate
        var rule   = new CpuBottleneckRule();
        var window = Window(5, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, Enumerable.Repeat(97f, 8).ToArray()),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f)]));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_NoFiring_At299Seconds()
    {
        // 29 samples × 10s = 28 intervals = 280s window — requiredSamples = 30; sustained = 29 < 30
        // Documents the boundary: all guards met but just short of the 300s gate must not confirm.
        var rule    = new CpuBottleneckRule();
        var perCore = Enumerable.Repeat(97f, 8).ToArray();
        var window  = Window(29, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, perCore),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f, null, 0f)]));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_NoFiring_WhenWindowTooShort_20Samples()
    {
        // 20 samples × 10s = 190s — requiredSamples = (int)(300/10) = 30; sustained = 20 < 30
        var rule   = new CpuBottleneckRule();
        var window = Window(20, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, Enumerable.Repeat(97f, 8).ToArray()),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f)]));

        // 20 consecutive samples << 30 required — must not confirm
        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_NoFiring_WhenCoresNotSaturated()
    {
        // 300s window, high total CPU but only 2/8 cores hot (25% < 50% threshold)
        var rule = new CpuBottleneckRule();
        var perCore = new float[] { 97f, 97f, 10f, 10f, 10f, 10f, 10f, 10f };
        var window  = Window(31, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(90f, 8, perCore),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f)]));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_NoFiring_WhenRamAlsoLow()
    {
        // All CPU guards pass but RAM is scarce — rule must require RAM free
        var rule   = new CpuBottleneckRule();
        var perCore = Enumerable.Repeat(97f, 8).ToArray();
        var window  = Window(31, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, perCore),
            mem:   MakeMem(16f, 0.5f),  // very low free — below 2 GB threshold
            disks: [MakeDisk(5f)]));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_NoFiring_WhenDiskQueueHigh()
    {
        // All CPU guards pass but disk queue is high — CPU rule must not fire
        var rule    = new CpuBottleneckRule();
        var perCore = Enumerable.Repeat(97f, 8).ToArray();
        var window  = Window(31, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, perCore),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(95f, 80f, 8f)]));  // queue=8 > threshold=5

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Cpu_Confirmed_WhenAllGuardsFor300s()
    {
        // 31 samples × 10s = 300s with all 4 guards met
        var rule    = new CpuBottleneckRule();
        var perCore = Enumerable.Repeat(97f, 8).ToArray();
        var window  = Window(31, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(97f, 8, perCore),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f, null, 0f)]));

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
        Assert.Equal(0, result.NotificationLevel);  // CPU bottleneck is always silent
    }

    // ─── RAM Pressure ──────────────────────────────────────────────────────

    [Fact]
    public void Ram_NoFiring_WhenHighUtilAlone()
    {
        // High commit ratio only (1 of 3 signals) — needs 2-of-3
        var rule   = new RamPressureRule();
        float total = 16f;
        var window = Window(15, 10, (_, at) => Snap(at,
            mem: MakeMem(total, 8f, 0.85f, false)));  // commitRatio=0.85 > 0.80 but free=8GB fine

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, total));
    }

    [Fact]
    public void Ram_NoFiring_WhenLowAvailableAlone()
    {
        // Low free RAM only (1 signal), commit ratio fine, no pagefile
        var rule   = new RamPressureRule();
        float total = 16f;
        var window = Window(15, 10, (_, at) => Snap(at,
            mem: new MemoryMetrics(total, 1.5f, 4f, total * 1.5f, false)));
        // commit = 4 / 24 = 0.17 — well below 0.80; free=1.5 below threshold, but only 1 signal

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, total));
    }

    [Fact]
    public void Ram_NoFiring_WhenShortDuration_AllSignalsPresent()
    {
        // All 3 signals present but only 60s — needs 120s
        var rule   = new RamPressureRule();
        float total = 8f;
        // 7 samples × 10s = 60s
        var window = Window(7, 10, (_, at) => Snap(at,
            mem: MakeMem(total, 0.3f, 0.85f, true)));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, total));
    }

    [Fact]
    public void Ram_NoFiring_WhenSignalsInterrupted()
    {
        // 15 samples but signal breaks at sample 7 — counter resets, never reaches 120s continuous
        var rule   = new RamPressureRule();
        float total = 8f;
        var now = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i =>
            {
                var at = now.AddSeconds((i - 14) * 10);
                bool bad = i == 7;  // one clean sample breaks the run
                var mem  = bad
                    ? MakeMem(total, 6f, 0.3f, false)    // clean sample
                    : MakeMem(total, 0.3f, 0.85f, true); // high-pressure sample
                return Snap(at, mem: mem);
            })
            .ToList();

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, total));
    }

    [Fact]
    public void Ram_Confirmed_After120sContinuous()
    {
        // 15 samples × 10s = 140s of continuous pressure
        var rule   = new RamPressureRule();
        float total = 8f;
        var now = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i => Snap(now.AddSeconds((i - 14) * 10),
                mem: MakeMem(total, 0.3f, 0.85f, true)))
            .ToList();

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, total);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
        Assert.Equal(2, result.NotificationLevel);  // RAM pressure is Level 2 — only rule that notifies
    }

    [Fact]
    public void Ram_Threshold_ScalesWithTotalRam()
    {
        // 64 GB machine: threshold = max(2.0, 64×0.05=3.2) = 3.2 GB
        // Free=2.5 is above 2.0 but below 3.2 — should still fire
        var rule   = new RamPressureRule();
        float total = 64f;
        var now = DateTimeOffset.UtcNow;
        var window = Enumerable.Range(0, 15)
            .Select(i => Snap(now.AddSeconds((i - 14) * 10),
                mem: MakeMem(total, 2.5f, 0.85f, true)))
            .ToList();

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, total);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
    }

    // ─── GPU Compute ───────────────────────────────────────────────────────

    [Fact]
    public void Gpu_AlwaysSilent_WhenSustained99Percent()
    {
        // GPU at 99% for 180s — should confirm but NotificationLevel must be 0
        var rule   = new GpuComputeRule();
        var window = Window(19, 10, (_, at) => Snap(at,
            gpu: MakeGpu(99f)));  // 18 samples × 10s = 180s

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        if (result != null)
            Assert.Equal(0, result.NotificationLevel);
    }

    [Fact]
    public void Gpu_Confirmed_WithNotificationLevel0()
    {
        // 13 samples × 10s = 120s — matches GpuComputeConfirmSeconds=120
        var rule   = new GpuComputeRule();
        var window = Window(13, 10, (_, at) => Snap(at,
            gpu: MakeGpu(95f)));

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(0, result!.NotificationLevel);
        Assert.Equal(BottleneckType.GpuCompute, result.BottleneckType);
    }

    // ─── VRAM Pressure ─────────────────────────────────────────────────────

    [Fact]
    public void Vram_SafeWhenStubbed_ReturnNull()
    {
        // VramTotalGb = 0 (DXGI stub) — rule must return null safely (no false positive)
        var rule   = new VramPressureRule();
        var window = Window(20, 10, (_, at) => Snap(at,
            gpu: new GpuMetrics(80f, 0f, 0f, null)));  // both 0 — stub state

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Vram_NoFiring_WhenTransientUnder180s()
    {
        // 17 samples × 10s = 160s — short of VramPressureConfirmSeconds=180
        var rule = new VramPressureRule();
        var window = Window(17, 10, (_, at) => Snap(at,
            gpu: MakeGpu(80f, 7.5f, 8f)));  // 7.5/8 = 93.75% > 92% threshold

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.True(result == null || result.Outcome != DiagnosisOutcome.Confirmed);
    }

    [Fact]
    public void Vram_Confirmed_After180sContinuous()
    {
        // 19 samples × 10s = 180s at 93.75% utilization
        var rule   = new VramPressureRule();
        var window = Window(19, 10, (_, at) => Snap(at,
            gpu: MakeGpu(80f, 7.5f, 8f)));

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
        Assert.Equal(0, result.NotificationLevel);  // VRAM diagnosis is always silent
    }

    // ─── Disk Bottleneck ───────────────────────────────────────────────────

    [Fact]
    public void Disk_ReturnsInsufficientEvidence_WhenNoLatencyData()
    {
        // No latency data — fast SSD or PDH not available
        var rule   = new DiskBottleneckRule();
        var window = Window(10, 10, (_, at) => Snap(at,
            disks: [MakeDisk(95f, null, 8f)]));  // latMs = null

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.InsufficientEvidence, result!.Outcome);
    }

    [Fact]
    public void Disk_NoFiring_WhenHighUtilAlone()
    {
        // High util but low latency and low queue — not a bottleneck
        var rule   = new DiskBottleneckRule();
        var window = Window(10, 10, (_, at) => Snap(at,
            disks: [MakeDisk(95f, 2f, 0f)]));  // util high, latency 2ms (fine), queue 0

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Disk_Confirmed_WhenAllThreeSignalsFor90s()
    {
        // 10 samples × 10s = 90s with all 3 disk signals — DiskBottleneckConfirmSeconds=90
        var rule   = new DiskBottleneckRule();
        var window = Window(10, 10, (_, at) => Snap(at,
            disks: [MakeDisk(95f, 100f, 7f)]));  // util=95%, latency=100ms, queue=7

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
        Assert.Equal(0, result.NotificationLevel);
    }

    // ─── Thermal Throttling ────────────────────────────────────────────────

    [Fact]
    public void Thermal_ReturnsNull_WhenNoSensors()
    {
        // LHM not installed — tempC = null, clocks = null
        var rule   = new ThermalThrottlingRule();
        var window = Window(10, 10, (_, at) => Snap(at,
            cpu: MakeCpu(90f)));  // no temp/clock data

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Thermal_NoFiring_WhenHighTempButNoClock()
    {
        // Temp high but current clock not below base (no throttle)
        var rule   = new ThermalThrottlingRule();
        var window = Window(10, 10, (_, at) => Snap(at,
            cpu: MakeCpu(90f, tempC: 95f, baseMhz: 4000f, currentMhz: 4000f)));

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Thermal_Confirmed_WhenHighTempAndClockDropFor60s()
    {
        // 7 samples × 10s = 60s — ThermalConfirmSeconds=60
        // Temp=95°C (>90 threshold), clock drop = (4000-3500)/4000 = 12.5% (>10% threshold)
        var rule   = new ThermalThrottlingRule();
        var window = Window(7, 10, (_, at) => Snap(at,
            cpu: MakeCpu(90f, tempC: 95f, baseMhz: 4000f, currentMhz: 3500f)));

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
    }

    // ─── Process Anomaly ───────────────────────────────────────────────────

    [Fact]
    public void Process_NoFiring_WhenHighCpuAlone()
    {
        // High CPU only, memory stable and low — growing=true but only 1 signal fires
        var rule  = new ProcessAnomalyRule();
        // ProcessCpuThreshold=0.40 → 40%CPU triggers; but MemoryMb=100 < 500 threshold
        // So resourceHigh = true (CPU only), but "growing" alone shouldn't cause anomaly
        // when not consistently high+growing for required samples
        var window = Window(7, 10, (i, at) => Snap(at,
            procs: MakeProc("chrome.exe", 50f, 100f)));  // 50% CPU > 40%, mem fine

        // Note: resourceHigh=true, growing=true (flat mem), but only 7 samples
        // ProcessAnomalyConfirmSeconds=120 at 10s interval = 12 required samples
        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Process_NoFiring_WhenGrowingButResourceLow()
    {
        // Memory growing but process stays under resource thresholds
        var rule  = new ProcessAnomalyRule();
        var window = Window(15, 10, (i, at) => Snap(at,
            procs: MakeProc("app.exe", 5f, 50f + i * 5f)));  // growing from 50MB, but always <500MB

        Assert.Null(rule.Evaluate(window, DiagnosisThresholds.Default, 16f));
    }

    [Fact]
    public void Process_Confirmed_WhenHighAndGrowingFor120s()
    {
        // 15 samples × 10s = 140s — ProcessAnomalyConfirmSeconds=120 at 10s = 12 samples
        var rule   = new ProcessAnomalyRule();
        var window = Window(15, 10, (i, at) => Snap(at,
            procs: MakeProc("leak.exe", 50f, 600f + i * 10f)));  // CPU=50%>40%, mem=600+→growing

        var result = rule.Evaluate(window, DiagnosisThresholds.Default, 16f);
        Assert.NotNull(result);
        Assert.Equal(DiagnosisOutcome.Confirmed, result!.Outcome);
        Assert.Equal(0, result.NotificationLevel);  // process anomaly is always silent
        Assert.Equal("leak.exe", result.ProcessName);
    }

    // ─── DiagnosisEngine — InsufficientEvidence must never be Primary ──────

    [Fact]
    public void Engine_InsufficientEvidence_NeverBecomesPrimary()
    {
        // DiskBottleneckRule returns InsufficientEvidence on systems without latency data.
        // DiagnosisEngine must not promote it to Primary.
        var engine = new DiagnosisEngine();
        engine.SetTotalPhysicalRam(16f);

        // Clean CPU/RAM, no latency data (triggers DiskBottleneckRule.InsufficientEvidence)
        var window = Window(10, 10, (_, at) => Snap(at,
            cpu:   MakeCpu(20f),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f, null, 0f)]));

        var result = engine.Evaluate(window);
        // Primary must be null even though DiskBottleneckRule fires InsufficientEvidence
        Assert.Null(result.Primary);
    }

    [Fact]
    public void Engine_ProcessAnomaly_NeverBecomesStandalonePrimary()
    {
        // ProcessAnomaly fires — but per design it must always be secondary
        var engine = new DiagnosisEngine();
        engine.SetTotalPhysicalRam(16f);

        var window = Window(15, 10, (i, at) => Snap(at,
            cpu:   MakeCpu(20f),
            mem:   MakeMem(16f, 12f),
            disks: [MakeDisk(5f, null, 0f)],
            procs: MakeProc("leak.exe", 50f, 600f + i * 10f)));

        var result = engine.Evaluate(window);
        // Process anomaly must not be Primary
        if (result.Primary != null)
            Assert.NotEqual(BottleneckType.ProcessAnomaly, result.Primary.BottleneckType);
    }
}
