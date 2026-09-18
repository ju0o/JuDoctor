# 08 — Incident Model
*Revised: 2026-09-17 — Gate Review 01*

---

## Incident Lifecycle

```
[DiagnosisEngine fires rule candidate]
          │
          ▼
      CANDIDATE
   (no DB write yet)
   Confirmation gate timing begins
          │
          │ gate seconds elapsed (see rule thresholds)
          │ with sustained signals
          ▼
      CONFIRMED → SQLiteIncidentStore.Save() [status = Active]
          │         NotificationService.Notify() [Level 2 for RAM only; silent otherwise]
          │
          │ signals clear / utilization drops below threshold
          ▼
      RESOLVED → SQLiteIncidentStore.Update(status = Resolved, resolved_at = now)
```

**No CRITICAL or ESCALATED states in V1.** The severity of an incident is expressed by its `peak_*` values and `duration_seconds`, not by a lifecycle state.

---

## Status Values

| Status | Meaning |
|---|---|
| `Active` | Confirmed, ongoing |
| `Resolved` | Condition cleared (natural resolution) |

`OrphanClosed` is recorded in `resolve_reason`, not status. All orphans transition to `Resolved` with `resolve_reason = OrphanClosed` on next startup.

---

## Notification Behavior Per Type

| Incident Type | Notification Level | Toast Fired? |
|---|---|---|
| RamPressure | 2 | Yes — once, on confirmation |
| CpuBottleneck | 0 | No |
| DiskBottleneck | 0 | No |
| VramPressure | 0 | No |
| GpuCompute | 0 | No |
| ThermalThrottling | 0 | No |
| ProcessAnomaly | 0 | No |

GPU compute incidents are always silent. They are stored and appear in history and "Why Slow?" analysis. They do not change the tray icon.

---

## Deduplication and Merging

**Same-type incident within 10 minutes:** The existing Active incident is updated (`last_seen_at` extended) rather than creating a new incident record. This prevents incident storms from repeated brief spikes.

**Different-type incidents:** Multiple bottleneck types can be Active simultaneously. They are stored as separate incident records. The DiagnosisEngine applies priority ordering when reporting — the highest-priority active incident is the "headline" in the dashboard.

---

## Incident Data Model

```csharp
public record Incident
{
    public string         Id                 { get; init; }  // GUID
    public BottleneckType BottleneckType     { get; init; }
    public IncidentStatus Status             { get; set; }
    public DateTimeOffset StartedAt          { get; init; }
    public DateTimeOffset? ConfirmedAt       { get; set; }
    public DateTimeOffset? ResolvedAt        { get; set; }
    public string?        ResolveReason      { get; set; }
    public float?         PeakCpuPercent     { get; set; }
    public float?         PeakMemUsedGb      { get; set; }
    public float?         PeakDiskLatencyMs  { get; set; }
    public float?         PeakGpuUtilPercent { get; set; }
    public int?           DurationSeconds    { get; set; }   // set on resolve
    public int            NotificationLevel  { get; init; }
    public DateTimeOffset? NotifiedAt        { get; set; }
    public string?        EvidenceJson       { get; set; }   // signals that confirmed it
}
```

---

## Process Attribution

When an incident is confirmed, the current `ProcessSummary` (top 5 by CPU, top 5 by RAM) is saved to `incident_processes`. These are the processes active at confirmation time.

```csharp
public record IncidentProcess
{
    public int    Id          { get; init; }
    public string IncidentId  { get; init; }
    public string ProcessName { get; init; }   // name only — no path, no arguments
    public float  CpuPercent  { get; init; }
    public float  MemoryMb    { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
}
```

**No disk I/O field.** `System.Diagnostics.Process` does not expose disk I/O properties. "Top by Disk" process ranking is not implemented in V1.

---

## Evidence JSON

The `evidence_json` field stores the signals that triggered confirmation, for display in Incident Detail and "Why Slow?":

```json
{
  "signals": [
    { "name": "MemAvailableGb", "value": 0.9, "threshold": 1.6 },
    { "name": "CommitChargeRatio", "value": 0.87, "threshold": 0.80 },
    { "name": "PagefilePressure", "value": true }
  ],
  "confirmationSeconds": 120,
  "sampleCount": 12
}
```

---

## Display Format

In the Dashboard recent incidents list:

```
[AMBER] RAM Pressure — 14 min — Today 2:34 PM
[GREY]  CPU Bottleneck — 8 min — Yesterday 11:02 AM
```

In Incident Detail:
```
RAM PRESSURE
──────────────────────────────────────
Duration:     14 minutes
Peak RAM Used: 14.8 / 16.0 GB (92.5%)
Available at peak: 1.2 GB (below 1.6 GB threshold)
Commit charge: 88% of limit
Pagefile pressure: Active

Top processes at confirmation:
  1. chrome.exe         CPU 12%   RAM 4,200 MB
  2. slack.exe          CPU 4%    RAM 1,800 MB
  3. devenv.exe         CPU 3%    RAM 1,200 MB

Evidence:
  ✓ Available RAM below threshold for 120 seconds
  ✓ Commit charge > 80% simultaneously
  ✓ Pagefile pressure active
```

---

## Retention

Incidents are retained for 90 days by default. `RetentionManager` purges on startup:

```sql
DELETE FROM incidents WHERE started_at < datetime('now', '-90 days');
```

`incident_processes` rows cascade-delete with the parent incident (ON DELETE CASCADE).
