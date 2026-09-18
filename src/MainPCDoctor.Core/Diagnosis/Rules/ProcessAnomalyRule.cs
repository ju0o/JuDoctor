using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis.Rules;

public sealed class ProcessAnomalyRule : IDiagnosisRule
{
    public BottleneckType BottleneckType => BottleneckType.ProcessAnomaly;
    public int            Priority       => 7;

    public RuleFinding? Evaluate(
        IReadOnlyList<SystemSnapshot> window,
        DiagnosisThresholds           thresholds,
        float                         totalPhysicalRamGb)
    {
        if (window.Count < 6) return null;
        if (!window.Any(s => s.Processes != null)) return null;

        int requiredSamples = SecondsToSamples(thresholds.ProcessAnomalyConfirmSeconds, window);

        // Track per-process: count of samples where both resource AND growth fire
        var processData = new Dictionary<string, (float firstMem, float lastMem, int andCount)>(StringComparer.OrdinalIgnoreCase);

        var samplesWithProcesses = window.Where(s => s.Processes != null).ToList();
        if (samplesWithProcesses.Count < 2) return null;

        foreach (var s in samplesWithProcesses)
        {
            var allProcs = s.Processes!.TopByCpu.Concat(s.Processes.TopByRam)
                            .DistinctBy(p => p.Name);

            foreach (var proc in allProcs)
            {
                bool resourceHigh = proc.CpuPercent / 100f > thresholds.ProcessCpuThreshold
                                    || proc.MemoryMb > thresholds.ProcessMemoryThresholdMb;

                if (!processData.TryGetValue(proc.Name, out var state))
                    state = (proc.MemoryMb, proc.MemoryMb, 0);

                // Growth pattern: memory higher than first observation or flat-high
                bool growing = proc.MemoryMb >= state.firstMem;

                int andCount = resourceHigh && growing ? state.andCount + 1 : 0;
                processData[proc.Name] = (state.firstMem, proc.MemoryMb, andCount);
            }
        }

        var offender = processData
            .Where(kv => kv.Value.andCount >= requiredSamples)
            .OrderByDescending(kv => kv.Value.andCount)
            .Select(kv => kv.Key)
            .FirstOrDefault();

        if (offender is null) return null;

        return new RuleFinding(BottleneckType.ProcessAnomaly, DiagnosisOutcome.Confirmed, 0,
            ProcessName: offender);
    }

    private static int SecondsToSamples(int seconds, IReadOnlyList<SystemSnapshot> window)
    {
        if (window.Count < 2) return seconds / 10;
        var avgInterval = (window[^1].CollectedAt - window[0].CollectedAt).TotalSeconds / (window.Count - 1);
        return Math.Max(1, (int)(seconds / Math.Max(1, avgInterval)));
    }
}
