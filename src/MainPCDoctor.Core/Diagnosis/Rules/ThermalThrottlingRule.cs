using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class ThermalThrottlingRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.ThermalThrottling;
    public int            Priority       => 1;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 3) return null;

        // Gracefully disabled when temperature sensor unavailable
        if (!window.Any(s => s.Cpu.TemperatureCelsius.HasValue && s.Cpu.BaseClockMhz.HasValue
                              && s.Cpu.CurrentClockMhz.HasValue))
            return null;

        int requiredSamples = SecondsToSamples(thresholds.ThermalConfirmSeconds, window);
        int sustainedCount  = 0;

        foreach (var s in window)
        {
            if (!s.Cpu.TemperatureCelsius.HasValue || !s.Cpu.BaseClockMhz.HasValue
                || !s.Cpu.CurrentClockMhz.HasValue)
            { sustainedCount = 0; continue; }

            bool tempHigh   = s.Cpu.TemperatureCelsius.Value > thresholds.CpuThermalCelsius;
            float clockDrop = 1f - s.Cpu.CurrentClockMhz.Value / s.Cpu.BaseClockMhz.Value;
            bool clockLow   = clockDrop > thresholds.CpuClockDegradationRatio;

            if (tempHigh && clockLow) sustainedCount++;
            else                      sustainedCount = 0;
        }

        if (sustainedCount < requiredSamples) return null;

        return new RuleFinding(BottleneckType.ThermalThrottling, DiagnosisOutcome.Confirmed, 0);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
