using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class VramPressureRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.VramPressure;
    public int            Priority       => 5;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 3) return null;
        if (!window.Any(s => s.Gpu?.VramTotalGb > 0)) return null;

        int requiredSamples = SecondsToSamples(thresholds.VramPressureConfirmSeconds, window);
        int sustainedCount  = 0;

        foreach (var s in window)
        {
            if (s.Gpu is null || s.Gpu.VramTotalGb <= 0) { sustainedCount = 0; continue; }
            float ratio = s.Gpu.VramUsedGb / s.Gpu.VramTotalGb;
            if (ratio >= thresholds.VramUtilizationThreshold) sustainedCount++;
            else                                                sustainedCount = 0;
        }

        if (sustainedCount < requiredSamples) return null;

        return new RuleFinding(BottleneckType.VramPressure, DiagnosisOutcome.Confirmed, 0);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
