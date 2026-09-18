using MainPCDoctor.Core.Models;
using MainPCDoctor.Core.Recommendations;
using Xunit;

namespace MainPCDoctor.Core.Tests;

/// <summary>
/// V1 upgrade recommendation safety gate.
/// Only CPU and RAM may produce recommendations in V1.
/// </summary>
public class UpgradeRecommendationSafetyTests
{
    private static readonly UpgradeRecommendationEngine _engine = new();

    // Build confirmed incidents of a given type spanning the requested number of days
    private static List<Incident> BuildIncidents(BottleneckType type, int dayCount,
        int incidentsPerDay = 3, int durationEach = 900)
    {
        var list = new List<Incident>();
        var base_ = DateTimeOffset.UtcNow.AddDays(-dayCount);
        for (int d = 0; d < dayCount; d++)
        {
            for (int j = 0; j < incidentsPerDay; j++)
            {
                list.Add(new Incident
                {
                    BottleneckType  = type,
                    Status          = IncidentStatus.Resolved,
                    StartedAt       = base_.AddDays(d).AddHours(j * 3),
                    ConfirmedAt     = base_.AddDays(d).AddHours(j * 3 + 1),
                    ResolvedAt      = base_.AddDays(d).AddHours(j * 3 + 2),
                    DurationSeconds = durationEach,
                });
            }
        }
        return list;
    }

    // ─── Scope: only CPU and RAM allowed ───────────────────────────────────

    [Fact]
    public void NoRecommendation_ForGpuIncidents()
    {
        // GPU incidents — V1 plan: GPU is diagnostic-only, no upgrade recommendation
        var history  = BuildIncidents(BottleneckType.GpuCompute, 30);
        var window   = DateTimeOffset.UtcNow.AddDays(-30);

        // UpgradeRecommendationEngine only has EvaluateCpu/EvaluateRam in V1
        // Calling either with GPU history must return null
        Assert.Null(_engine.EvaluateCpu(history, window));
        Assert.Null(_engine.EvaluateRam(history, window));
    }

    [Fact]
    public void NoRecommendation_ForVramIncidents()
    {
        var history = BuildIncidents(BottleneckType.VramPressure, 30);
        var window  = DateTimeOffset.UtcNow.AddDays(-30);

        Assert.Null(_engine.EvaluateCpu(history, window));
        Assert.Null(_engine.EvaluateRam(history, window));
    }

    [Fact]
    public void NoRecommendation_ForDiskIncidents()
    {
        var history = BuildIncidents(BottleneckType.DiskBottleneck, 30);
        var window  = DateTimeOffset.UtcNow.AddDays(-30);

        Assert.Null(_engine.EvaluateCpu(history, window));
        Assert.Null(_engine.EvaluateRam(history, window));
    }

    [Fact]
    public void NoRecommendation_ForThermalIncidents()
    {
        var history = BuildIncidents(BottleneckType.ThermalThrottling, 30);
        var window  = DateTimeOffset.UtcNow.AddDays(-30);

        Assert.Null(_engine.EvaluateCpu(history, window));
        Assert.Null(_engine.EvaluateRam(history, window));
    }

    [Fact]
    public void NoRecommendation_ForProcessAnomalyIncidents()
    {
        var history = BuildIncidents(BottleneckType.ProcessAnomaly, 30);
        var window  = DateTimeOffset.UtcNow.AddDays(-30);

        Assert.Null(_engine.EvaluateCpu(history, window));
        Assert.Null(_engine.EvaluateRam(history, window));
    }

    // ─── Observation period gate ───────────────────────────────────────────

    [Fact]
    public void NoRecommendation_WhenObservationBelow7Days()
    {
        // 6 days with heavy CPU incidents — below 7-day minimum
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 6, 5, 1200);
        var window  = DateTimeOffset.UtcNow.AddDays(-6);

        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    [Fact]
    public void NoRecommendation_WhenWindowStartIsRecent()
    {
        // History is old but window start is only 5 days ago — observation = 5d < 7d
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 30, 5, 1200);
        var window  = DateTimeOffset.UtcNow.AddDays(-5);  // narrow window

        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    [Fact]
    public void NoRecommendation_WhenObservationBetween7And13Days()
    {
        // Gate contract: observation period <14 days → NO Level 4 recommendation (any confidence).
        // Current code: only gates at <7 days; Low confidence fires at 7-13 days — BUG.
        // This test documents the required behavior. EXPECT: null. ACTUAL (bug): Low confidence rec.
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 10, 5, 1500);
        var window  = DateTimeOffset.UtcNow.AddDays(-10);

        // V1 contract requires null here. If this assertion fails, the 7-vs-14-day
        // gate bug (V1_ACCEPTANCE_BLOCKER_RECOMMENDATION_GATE) is confirmed.
        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    // ─── Score threshold gate ──────────────────────────────────────────────

    [Fact]
    public void NoRecommendation_WhenScoreBelowInsufficient()
    {
        // 7 days, only 1 incident total — score too low
        var history = new List<Incident>
        {
            new()
            {
                BottleneckType  = BottleneckType.CpuBottleneck,
                Status          = IncidentStatus.Resolved,
                StartedAt       = DateTimeOffset.UtcNow.AddDays(-7),
                ConfirmedAt     = DateTimeOffset.UtcNow.AddDays(-7).AddHours(1),
                DurationSeconds = 300,
            }
        };
        var window = DateTimeOffset.UtcNow.AddDays(-7);

        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    // ─── 14-day boundary coverage ──────────────────────────────────────────

    [Fact]
    public void NoRecommendation_WhenExactly13Days()
    {
        // 13 days = one day short of the 14-day gate — even with heavy incident history
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 13, 5, 1500);
        var window  = DateTimeOffset.UtcNow.AddDays(-13);

        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    [Fact]
    public void NoRecommendation_WhenExactly14DaysButScoreTooLow()
    {
        // 14 days passes the time gate but a single low-severity incident is not enough score
        var history = new List<Incident>
        {
            new()
            {
                BottleneckType  = BottleneckType.CpuBottleneck,
                Status          = IncidentStatus.Resolved,
                StartedAt       = DateTimeOffset.UtcNow.AddDays(-14),
                ConfirmedAt     = DateTimeOffset.UtcNow.AddDays(-14).AddHours(1),
                DurationSeconds = 100,   // short, low-severity
            }
        };
        var window = DateTimeOffset.UtcNow.AddDays(-14);

        // score = 1×2 (distinctDays) + 1×3 (distinctSessions) - 5 (distinctDays<3) = 0  → Insufficient
        Assert.Null(_engine.EvaluateCpu(history, window));
    }

    // ─── V1 positive cases ─────────────────────────────────────────────────

    [Fact]
    public void CpuRecommendation_EligibleWith14DaysAndHighScore()
    {
        // 14 days, 4 incidents/day, each 900s — should produce at least Low confidence
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 14, 4, 900);
        var window  = DateTimeOffset.UtcNow.AddDays(-14);

        var rec = _engine.EvaluateCpu(history, window);
        Assert.NotNull(rec);
        Assert.Equal("CPU", rec!.Component);
        Assert.True(rec.EvidenceScore >= 20f);
        Assert.NotEqual(ConfidenceLevel.Insufficient, rec.Confidence);
    }

    [Fact]
    public void RamRecommendation_EligibleWith14DaysAndHighScore()
    {
        var history = BuildIncidents(BottleneckType.RamPressure, 14, 4, 900);
        var window  = DateTimeOffset.UtcNow.AddDays(-14);

        var rec = _engine.EvaluateRam(history, window);
        Assert.NotNull(rec);
        Assert.Equal("RAM", rec!.Component);
        Assert.NotEqual(ConfidenceLevel.Insufficient, rec.Confidence);
    }

    [Fact]
    public void HighConfidence_RequiresAtLeast14DayObservation()
    {
        // Score ≥ 50 but only 10 days — should not be High confidence
        var history = BuildIncidents(BottleneckType.CpuBottleneck, 10, 6, 1500);
        var window  = DateTimeOffset.UtcNow.AddDays(-10);

        var rec = _engine.EvaluateCpu(history, window);
        if (rec != null)
            Assert.NotEqual(ConfidenceLevel.High, rec.Confidence);
    }
}
