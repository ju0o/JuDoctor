using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class CpuBottleneckRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.CpuBottleneck;
    public int            Priority       => 3;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 6) return null;

        float freeRamThreshold = MathF.Max(
            thresholds.RamFreeThresholdMinGb,
            totalPhysicalRamGb * thresholds.RamFreeThresholdRatio);

        int requiredSamples = SecondsToSamples(thresholds.CpuBottleneckConfirmSeconds, window);
        int sustainedCount  = 0;

        foreach (var s in window)
        {
            int coreCount     = s.Cpu.LogicalCoreCount;
            int hotCores      = s.Cpu.PerCoreUtilizationPercent.Count(c => c > 90f);
            float coreRatio   = coreCount > 0 ? (float)hotCores / coreCount : 0f;

            bool cpuHigh      = s.Cpu.TotalUtilizationPercent / 100f >= thresholds.CpuUtilizationThreshold;
            bool coresSat     = coreRatio >= thresholds.CpuAffectedCoreRatio;
            bool ramFree      = s.Memory.AvailableGb > freeRamThreshold;
            bool diskFree     = s.Disks.All(d => d.QueueDepth < thresholds.DiskQueueDepthThreshold);

            if (cpuHigh && coresSat && ramFree && diskFree) sustainedCount++;
            else                                             sustainedCount = 0;
        }

        if (sustainedCount < requiredSamples) return null;

        // Always silent — Level 0
        return new RuleFinding(BottleneckType.CpuBottleneck, DiagnosisOutcome.Confirmed, 0);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
