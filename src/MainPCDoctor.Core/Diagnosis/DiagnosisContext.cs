using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis;

/// <summary>Pre-computed aggregates for the evaluation window, shared across rules.</summary>
internal record DiagnosisContext(
    IReadOnlyList<SystemSnapshot> Window,
    float TotalPhysicalRamGb,
    float FreeRamThresholdGb,

    // CPU
    float AvgCpuUtil,
    float MaxCpuUtil,
    int   AffectedCoreCount,

    // RAM
    float AvgAvailableRamGb,
    float MinAvailableRamGb,
    float AvgCommitChargeRatio,
    bool  PagefilePressureAny,

    // Disk
    float MaxDiskUtil,
    float? MaxDiskLatencyMs,
    float MaxDiskQueue,

    // GPU
    float? AvgGpuUtil,
    float? AvgVramRatio,

    // CPU clock (for thermal)
    float? AvgCpuTemp,
    float? AvgCurrentClockMhz,
    float? BaseClockMhz,

    int SampleCount,
    TimeSpan WindowDuration
)
{
    public static DiagnosisContext From(
        IReadOnlyList<SystemSnapshot> window,
        float totalPhysicalRamGb,
        float freeRamThresholdGb)
    {
        if (window.Count == 0)
        {
            return new DiagnosisContext(window, totalPhysicalRamGb, freeRamThresholdGb,
                0, 0, 0, 0, 0, 0, false, 0, null, 0, null, null, null, null, null,
                0, TimeSpan.Zero);
        }

        int n = window.Count;

        float avgCpu    = window.Average(s => s.Cpu.TotalUtilizationPercent);
        float maxCpu    = window.Max(s => s.Cpu.TotalUtilizationPercent);
        int   coreCount = window[0].Cpu.LogicalCoreCount;
        int   affectedCores = coreCount > 0
            ? (int)window.Average(s =>
                s.Cpu.PerCoreUtilizationPercent.Count(c => c > 90f))
            : 0;

        float avgAvailRam = window.Average(s => s.Memory.AvailableGb);
        float minAvailRam = window.Min(s => s.Memory.AvailableGb);
        float avgCommit   = window.Average(s => s.Memory.CommitChargeRatio);
        bool  pagefile    = window.Any(s => s.Memory.PagefilePressureProxy);

        float maxDiskUtil  = window.Max(s => s.Disks.Count > 0 ? s.Disks.Max(d => d.UtilizationPercent) : 0f);
        float? maxLatency  = window.SelectMany(s => s.Disks)
                                    .Select(d => d.AverageLatencyMs)
                                    .Where(v => v.HasValue)
                                    .Select(v => v!.Value)
                                    .DefaultIfEmpty(float.NaN)
                                    .Max() is float m && !float.IsNaN(m) ? m : null;
        float maxQueue     = window.Max(s => s.Disks.Count > 0 ? s.Disks.Max(d => d.QueueDepth) : 0f);

        float? avgGpuUtil  = window.Any(s => s.Gpu != null)
            ? window.Where(s => s.Gpu != null).Average(s => s.Gpu!.UtilizationPercent)
            : null;
        float? avgVramRatio = window.Any(s => s.Gpu?.VramTotalGb > 0)
            ? window.Where(s => s.Gpu?.VramTotalGb > 0)
                    .Average(s => s.Gpu!.VramUsedGb / s.Gpu.VramTotalGb)
            : null;

        float? avgTemp   = window.Any(s => s.Cpu.TemperatureCelsius.HasValue)
            ? window.Where(s => s.Cpu.TemperatureCelsius.HasValue)
                    .Average(s => s.Cpu.TemperatureCelsius!.Value)
            : null;
        float? avgClock  = window.Any(s => s.Cpu.CurrentClockMhz.HasValue)
            ? window.Where(s => s.Cpu.CurrentClockMhz.HasValue)
                    .Average(s => s.Cpu.CurrentClockMhz!.Value)
            : null;
        float? baseClock = window.LastOrDefault()?.Cpu.BaseClockMhz;

        var duration = n >= 2
            ? window[^1].CollectedAt - window[0].CollectedAt
            : TimeSpan.Zero;

        return new DiagnosisContext(
            window, totalPhysicalRamGb, freeRamThresholdGb,
            avgCpu, maxCpu, affectedCores,
            avgAvailRam, minAvailRam, avgCommit, pagefile,
            maxDiskUtil, maxLatency, maxQueue,
            avgGpuUtil, avgVramRatio,
            avgTemp, avgClock, baseClock,
            n, duration);
    }
}
