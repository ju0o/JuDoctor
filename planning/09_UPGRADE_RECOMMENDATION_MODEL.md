# 09 — Upgrade Recommendation Model
*Revised: 2026-09-17 — Gate Review 01*

---

## V1 Scope

**V1 upgrade recommendations: CPU and RAM only.**

| Component | V1 Upgrade Recommendation | V1 Diagnostic Role |
|---|---|---|
| CPU | Yes | Incident history, "Why Slow?" |
| RAM | Yes | Incident history, "Why Slow?", Level 2 notification |
| GPU | No | Incident history, "Why Slow?" only |
| VRAM | No | Incident history, "Why Slow?" only |
| Disk | No | Incident history, "Why Slow?" only |
| Thermal | No | Incident history, "Why Slow?" only |

GPU, VRAM, Disk, and Thermal incidents are recorded and analyzed. They appear in history, "Why Slow?" results, and System Capacity monitoring status. They do not produce upgrade recommendations in V1.

**Rationale:** GPU recommendations require real-time market data to be actionable (VRAM requirements change per title). Disk recommendations require knowing whether the issue is drive speed, controller capacity, or file system fragmentation. CPU and RAM recommendations have stable, evidence-based heuristics that work without external data.

---

## Evidence Scoring Model

The `UpgradeRecommendationEngine` runs on a 30-day schedule (not every sample). It reads stored incident history and computes an evidence score per component.

### Score Calculation

```
Score = (IncidentDays × 2)
      + (AffectedSessions × 3)
      + (HighSeverityIncidentCount × 5)
      + (DurationBonusPoints)
      - PenaltyPoints

where:
  IncidentDays           = number of distinct calendar days with ≥ 1 confirmed incident
  AffectedSessions       = number of distinct OS sessions with ≥ 1 confirmed incident
  HighSeverityIncident   = incident lasting > 10 minutes
  DurationBonusPoints    = +2 per incident with duration > 20 minutes
  PenaltyPoints          = 10 if total observation window < 7 days
                           5 if incidents cluster in < 3 calendar days
```

### Confidence Thresholds

| Confidence | Score | Observation | Behavior |
|---|---|---|---|
| Insufficient | < 20 | < 7 days | No recommendation generated |
| Low | 20–34 | ≥ 7 days | Shown in System Capacity only (no notification) |
| Medium | 35–49 | ≥ 14 days | Shown in System Capacity + Level 4 toast |
| High | ≥ 50 | ≥ 14 days | Shown in System Capacity + Level 4 toast |

**A recommendation notification fires at most once per 30 days per component.**

---

## CPU Upgrade Recommendation

### Score Context

CPU incidents must pass the 300-second gate (see Rule 02) before entering the scoring model. Short build spikes never accumulate evidence.

### Corroborating Evidence Required

The engine checks for corroborating evidence before finalizing a CPU recommendation:
- `RamPressure` must NOT be simultaneously common — if RAM incidents occur on the same days as CPU incidents, RAM is the primary constraint; CPU recommendation is suppressed.
- Disk bottleneck must NOT be simultaneously common — if disk incidents co-occur, disk is the more likely constraint.

If either corroborator fires, the recommendation is downgraded one confidence level.

### Recommendation Text

**High confidence:**
```
Your CPU has been the sustained bottleneck on N days out of the last O days,
across P sessions. Memory and disk headroom were available.

Evidence:
• N days with confirmed CPU saturation (5+ minutes each)
• P affected sessions
• Peak sustained duration: X minutes

This pattern suggests your CPU cannot keep up with your typical workload.
A processor upgrade may significantly improve responsiveness.
```

**Medium confidence:**
```
Your CPU has shown sustained saturation on N days. Evidence is accumulating.
Continue monitoring before acting on this.
```

---

## RAM Upgrade Recommendation

### Score Context

RAM incidents require 2-of-3 signals for 120 seconds to confirm. Each confirmed RAM incident contributes to the score.

### Recommendation Text

**High confidence:**
```
Your system has been under significant RAM pressure on N days out of the last O days.

Evidence:
• N days with confirmed memory pressure
• P sessions affected
• System reached near-total physical memory on Q occasions
• Pagefile pressure detected on R occasions

RAM pressure degrades system responsiveness. Adding memory (currently X GB
installed) is likely to reduce these incidents.
```

**Medium confidence:**
```
RAM pressure has been confirmed on N days. The pattern is building.
Continue monitoring — adding RAM may help if this persists.
```

---

## System Capacity View — Non-Upgrade Components

GPU, VRAM, Disk, and Thermal have monitoring status entries on the System Capacity screen. They do **not** have recommendation badges.

| Component | Display |
|---|---|
| GPU | Utilization over last 7 days (avg / peak). Status: Healthy / Busy / Saturated |
| VRAM | Average / peak VRAM usage percentage. Status: Healthy / High / Saturated |
| Disk | Average / peak latency and utilization. Status: Healthy / Slow / Bottlenecked |
| CPU temp | Average / peak temperature if sensor available. Status: Normal / Warm / Hot |

These inform the user but never produce a recommendation badge or upgrade notification.

---

## Data Freshness Requirement

The recommendation engine only runs if the most recent observation window includes at least 7 days of actual monitoring data. It skips and logs if:
- Database is empty
- Fewer than 7 days of `metrics_samples_1min` records exist
- `UpgradeRecommendationStore` shows a recommendation was sent within the last 30 days for this component

---

## Persistence

`upgrade_recommendations` table (see `14_DATA_MODEL.md`):
- One row per component per evaluation run that crosses a confidence threshold
- Stores: component, confidence, score, evidence_json, generated_at, notification_sent_at
- V1 rows: component IN ('CPU', 'RAM') only
- Retained indefinitely (small table, high informational value)
