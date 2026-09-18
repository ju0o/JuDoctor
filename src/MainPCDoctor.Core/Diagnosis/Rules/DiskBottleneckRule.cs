using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class DiskBottleneckRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.DiskBottleneck;
    public int            Priority       => 4;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 3) return null;

        // If no disk in window has latency data, we cannot make a determination
        bool anyLatencyData = window.SelectMany(s => s.Disks).Any(d => d.AverageLatencyMs.HasValue);
        if (!anyLatencyData)
            return new RuleFinding(BottleneckType.DiskBottleneck, DiagnosisOutcome.InsufficientEvidence, 0,
                Note: "Disk latency data unavailable");

        int requiredSamples = SecondsToSamples(thresholds.DiskBottleneckConfirmSeconds, window);
        int sustainedCount  = 0;

        foreach (var s in window)
        {
            bool utilHigh  = s.Disks.Any(d => d.UtilizationPercent / 100f >= thresholds.DiskUtilizationThreshold);
            bool latHigh   = s.Disks.Any(d => d.AverageLatencyMs.HasValue
                                              && d.AverageLatencyMs.Value >= thresholds.DiskLatencyThresholdMs);
            bool queueHigh = s.Disks.Any(d => d.QueueDepth >= thresholds.DiskQueueDepthThreshold);

            if (utilHigh && latHigh && queueHigh) sustainedCount++;
            else                                   sustainedCount = 0;
        }

        if (sustainedCount < requiredSamples) return null;

        return new RuleFinding(BottleneckType.DiskBottleneck, DiagnosisOutcome.Confirmed, 0);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
