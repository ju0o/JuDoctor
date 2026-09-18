using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class GpuComputeRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.GpuCompute;
    public int            Priority       => 6;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 3) return null;
        if (!window.Any(s => s.Gpu != null)) return null;

        int requiredSamples = SecondsToSamples(thresholds.GpuComputeConfirmSeconds, window);
        int sustainedCount  = 0;

        foreach (var s in window)
        {
            if (s.Gpu is null) { sustainedCount = 0; continue; }
            if (s.Gpu.UtilizationPercent / 100f >= thresholds.GpuUtilizationThreshold) sustainedCount++;
            else sustainedCount = 0;
        }

        if (sustainedCount < requiredSamples) return null;

        // Always silent — gaming GPU saturation is expected
        return new RuleFinding(BottleneckType.GpuCompute, DiagnosisOutcome.Confirmed, 0);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
