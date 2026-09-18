using System.Text.Json;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Core.Recommendations;

public sealed class UpgradeRecommendationEngine
{
    private static readonly string[] V1Components = { "CPU", "RAM" };

    public UpgradeRecommendation? EvaluateCpu(IReadOnlyList<Incident> history, DateTimeOffset windowStart)
    {
        return Evaluate("CPU", history, windowStart,
            i => i.BottleneckType == BottleneckType.CpuBottleneck);
    }

    public UpgradeRecommendation? EvaluateRam(IReadOnlyList<Incident> history, DateTimeOffset windowStart)
    {
        return Evaluate("RAM", history, windowStart,
            i => i.BottleneckType == BottleneckType.RamPressure);
    }

    private static UpgradeRecommendation? Evaluate(
        string component,
        IReadOnlyList<Incident> history,
        DateTimeOffset windowStart,
        Func<Incident, bool> filter)
    {
        var relevant = history
            .Where(i => filter(i) && i.ConfirmedAt.HasValue && i.StartedAt >= windowStart)
            .ToList();

        if (relevant.Count == 0) return null;

        int observationDays = (int)(DateTimeOffset.UtcNow - windowStart).TotalDays;
        if (observationDays < 14) return null;   // V1 gate: minimum 14-day observation window

        var distinctDays     = relevant.Select(i => i.StartedAt.Date).Distinct().Count();
        var distinctSessions = relevant.Select(i => i.StartedAt.Date).Distinct().Count(); // proxy
        var highSeverity     = relevant.Count(i => i.DurationSeconds > 600);
        var durationBonus    = relevant.Count(i => i.DurationSeconds > 1200) * 2;

        float score = distinctDays * 2
                    + distinctSessions * 3
                    + highSeverity * 5
                    + durationBonus;

        if (distinctDays < 3) score -= 5;

        var confidence = score switch
        {
            >= 50 when observationDays >= 14 => ConfidenceLevel.High,
            >= 35 when observationDays >= 14 => ConfidenceLevel.Medium,
            >= 20                             => ConfidenceLevel.Low,
            _                                 => ConfidenceLevel.Insufficient
        };

        if (confidence == ConfidenceLevel.Insufficient) return null;

        var evidence = new
        {
            incidentDays    = distinctDays,
            totalIncidents  = relevant.Count,
            highSeverity,
            observationDays,
            score
        };

        return new UpgradeRecommendation(
            Id: Guid.NewGuid().ToString(),
            Component: component,
            Confidence: confidence,
            EvidenceScore: score,
            ObservationDays: observationDays,
            EvidenceJson: JsonSerializer.Serialize(evidence),
            GeneratedAt: DateTimeOffset.UtcNow,
            NotificationSentAt: null);
    }
}
