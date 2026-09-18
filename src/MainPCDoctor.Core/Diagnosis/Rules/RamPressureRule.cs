using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class RamPressureRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.RamPressure;
    public int            Priority       => 2;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 6) return null;

        float freeThreshold = MathF.Max(
            thresholds.RamFreeThresholdMinGb,
            totalPhysicalRamGb * thresholds.RamFreeThresholdRatio);

        // Count samples where each signal fires
        int samplesWithSignal = 0;
        int requiredSamples   = SecondsToSamples(thresholds.RamPressureConfirmSeconds, window);

        foreach (var s in window)
        {
            int signals = 0;
            if (s.Memory.AvailableGb < freeThreshold)         signals++;
            if (s.Memory.CommitChargeRatio > thresholds.RamCommitChargeRatio) signals++;
            if (s.Memory.PagefilePressureProxy)                signals++;

            if (signals >= thresholds.RamPressureMinSignals) samplesWithSignal++;
            else                                              samplesWithSignal = 0; // reset on break
        }

        if (samplesWithSignal < requiredSamples) return null;

        return new RuleFinding(BottleneckType.RamPressure, DiagnosisOutcome.Confirmed, 2);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
