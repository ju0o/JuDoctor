using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Diagnosis.Rules;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Diagnosis;

public sealed class DiagnosisEngine : IDiagnosisEngine
{
    private readonly IReadOnlyList<IDiagnosisRule> _rules;
    private readonly DiagnosisThresholds           _thresholds;
    private float                                  _totalPhysicalRamGb;

    public DiagnosisEngine(DiagnosisThresholds? thresholds = null)
    {
        _thresholds = thresholds ?? DiagnosisThresholds.Default;
        _rules = new IDiagnosisRule[]
        {
            new ThermalThrottlingRule(),  // priority 1
            new RamPressureRule(),         // priority 2
            new CpuBottleneckRule(),       // priority 3
            new DiskBottleneckRule(),      // priority 4
            new VramPressureRule(),        // priority 5
            new GpuComputeRule(),          // priority 6
            new ProcessAnomalyRule(),      // priority 7
        }.OrderBy(r => r.Priority).ToArray();
    }

    public void SetTotalPhysicalRam(float totalGb) => _totalPhysicalRamGb = totalGb;

    public DiagnosisResult Evaluate(IReadOnlyList<SystemSnapshot> recentWindow)
    {
        var now = DateTimeOffset.UtcNow;

        if (recentWindow.Count < 3)
            return DiagnosisResult.Clean(now);

        float ramGb = _totalPhysicalRamGb > 0
            ? _totalPhysicalRamGb
            : recentWindow[^1].Memory.TotalPhysicalGb;

        var findings = new List<RuleFinding>();

        foreach (var rule in _rules)
        {
            var finding = rule.Evaluate(recentWindow, _thresholds, ramGb);
            if (finding is not null)
                findings.Add(finding);
        }

        if (findings.Count == 0)
            return DiagnosisResult.Clean(now);

        // InsufficientEvidence and ProcessAnomaly are always secondary
        var primary   = findings.FirstOrDefault(f =>
            f.BottleneckType != BottleneckType.ProcessAnomaly &&
            f.Outcome        != DiagnosisOutcome.InsufficientEvidence);
        var secondary = findings.Where(f => f != primary).ToArray();

        return new DiagnosisResult(now, primary, secondary);
    }
}
