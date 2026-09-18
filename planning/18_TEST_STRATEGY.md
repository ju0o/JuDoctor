# 18 — Test Strategy
*Revised: 2026-09-17 — Gate Review 01*

---

## Test Pyramid

```
          [Integration Tests]
        BackgroundWorkerSmokeTests
       ─────────────────────────────
        [Unit Tests — Storage]
        MetricsStore, IncidentStore
       ─────────────────────────────
        [Unit Tests — Core]
        All diagnosis rules
        DiagnosisEngine
        IncidentTracker
        UpgradeRecommendationEngine
```

Unit tests for Core run on `net8.0` — no Windows required. Unit tests for Storage run against SQLite in-memory. Integration tests require Windows and real PDH counters.

---

## Unit Tests — Core (`MainPCDoctor.Core.Tests`)

### RamPressureRuleTests

```csharp
// Boundary tests for RAM Pressure rule
[Fact] void NoIncident_WhenOnlyOneSignalPresent()
// Single signal (e.g. only low available RAM, but commit ratio OK)
// → No candidate; rule requires 2-of-3

[Fact] void Candidate_WhenTwoSignalsFire()
// Both low available AND commit > 0.80
// → Candidate created; confirmation gate starts

[Fact] void Confirmed_WhenTwoSignalsSustained_For120Seconds()
// 2-of-3 signals sustained for 120s
// → Confirmed incident

[Fact] void NoIncident_OnBriefSpike()
// Signals fire for 60s then clear
// → Candidate created but gate not reached; no incident

[Fact] void ThresholdIsCapacityAware_LargeRam()
// System with 64 GB RAM: threshold = max(2.0, 64*0.05) = 3.2 GB
// → Available 3.0 GB should trigger; 3.5 GB should not

[Fact] void ThresholdIsCapacityAware_SmallRam()
// System with 4 GB RAM: threshold = max(2.0, 4*0.05) = 2.0 GB
// → Cap applies; threshold = 2.0 GB
```

### CpuBottleneckRuleTests

```csharp
[Fact] void NoIncident_WhenDurationBelowGate()
// CPU at 90% for 240 seconds (below 300s gate)
// → No confirmation

[Fact] void Confirmed_WhenDurationMeetsGate()
// CPU at 90%, all 4 conditions met, sustained 300 seconds
// → Confirmed incident

[Fact] void NoIncident_WhenRamAlsoConstrained()
// CPU high, but RAM also below FreeRamThreshold
// → RAM guard fires; CPU rule does not confirm

[Fact] void NoIncident_WhenDiskAlsoBottlenecked()
// CPU high, but disk queue > 5 simultaneously
// → Disk guard fires; CPU rule does not confirm

[Fact] void CpuRule_IsAlwaysSilent_Level0()
// Even fully confirmed CPU incident
// → NotificationLevel = 0; no notification
```

### DiskBottleneckRuleTests

```csharp
[Fact] void ReturnsInsufficientEvidence_WhenLatencyUnavailable()
// Disk utilization 95%, queue 8, but latency data is null
// → INSUFFICIENT_EVIDENCE result; no incident created

[Fact] void Confirmed_WhenAllThreeSignalsPresent()
// Util 95%, latency 65ms, queue 8, sustained 90s
// → Confirmed incident

[Fact] void NoIncident_HighUtilizationWithLowLatency()
// Util 95%, latency 2ms (NVMe), queue 2
// → Latency threshold not met; no incident (correct for fast SSDs)
```

### DiagnosticOnlyRuleTests (VRAM, GPU, Thermal, Process)

```csharp
[Fact] void VramPressureRule_Confirmed_NotificationLevel0()
// VRAM at 93% for 180 seconds
// → Confirmed, but NotificationLevel = 0 (silent)

[Fact] void VramPressureRule_NoIncident_Below180Seconds()
// VRAM at 95% for 120 seconds (typical level load)
// → Below gate; no incident

[Fact] void GpuComputeRule_AlwaysSilent()
// GPU at 99% for 300 seconds
// → Confirmed, but NotificationLevel = 0 always

[Fact] void ThermalRule_Disabled_WhenSensorUnavailable()
// ThermalRule with TemperatureCelsius = null
// → Rule is skipped gracefully; no INSUFFICIENT_EVIDENCE logged per cycle

[Fact] void ProcessAnomalyRule_RequiresAndCondition()
// Process CPU high (40%) but no growth pattern
// → No incident (AND logic; single condition not sufficient)

[Fact] void ProcessAnomalyRule_Confirmed_WhenBothConditionsMet()
// Process CPU high + memory growing over 120s
// → Confirmed incident
```

### DiagnosisEngineTests

```csharp
[Fact] void PriorityOrder_RamBeforeCpu_WhenBothFire()
// RAM and CPU both confirmed simultaneously
// → Primary result = RamPressure; CPU is secondary

[Fact] void ProcessAnomaly_IsAlwaysSecondary()
// ProcessAnomaly + RamPressure both firing
// → ProcessAnomaly recorded as secondary; not primary headline

[Fact] void SingleRule_ProducesCorrectResult()
// Only RamPressureRule fires
// → DiagnosisResult.Primary = RamPressure

[Fact] void NoRulesFiring_ReturnsCleanState()
// All metrics below thresholds
// → DiagnosisResult.Primary = null (no active bottleneck)
```

### UpgradeRecommendationEngineTests

```csharp
[Fact] void NoRecommendation_WhenObservationBelow7Days()
// 5 days of incident history
// → No recommendation (minimum 7 days required)

[Fact] void NoRecommendation_WhenScoreBelow20()
// 14 days of observation, 1 incident
// → Score too low; no recommendation

[Fact] void MediumConfidence_AtScoreThreshold()
// Score 37, observation 15 days
// → Medium confidence recommendation

[Fact] void HighConfidence_AtScoreThreshold()
// Score 52, observation 20 days
// → High confidence recommendation

[Fact] void CpuRecommendation_Suppressed_WhenRamCoOccurs()
// CPU incidents co-occur with RAM incidents on same days
// → CPU recommendation downgraded one level

[Fact] void NoGpuRecommendation_Ever()
// GPU incidents fill history
// → No GPU recommendation generated (not in V1)

[Fact] void CooldownRespected()
// Recommendation sent 15 days ago
// → Engine does not generate a second recommendation within 30-day cooldown
```

---

## Unit Tests — Storage (`MainPCDoctor.Storage.Tests`)

### MetricsStoreTests

```csharp
[Fact] void Write_And_Read_MinuteAggregate()
// Write a metrics_samples_1min row; read it back; values match

[Fact] void RetentionManager_Purges_OldRows()
// Insert row with sampled_at 40 days ago; run retention; row is gone

[Fact] void RetentionManager_Keeps_RecentRows()
// Insert row 20 days ago; run retention (30-day window); row remains
```

### IncidentStoreTests

```csharp
[Fact] void Save_And_Retrieve_Incident()
[Fact] void Update_Incident_Status_To_Resolved()
[Fact] void RecoverOrphans_ClosesActiveIncidents()
// Incident with Active status and LastSeenAt > 10 min ago
// → RecoverOrphans marks it Resolved with OrphanClosed reason
[Fact] void IncidentProcesses_CascadeDelete()
// Deleting an incident also deletes its incident_processes rows
```

### RetentionManagerTests

```csharp
[Fact] void Purge_Respects_ConfiguredWindow()
// 60-day configured window; rows 45 days old are deleted; rows 30 days old remain
```

---

## Integration Tests (`MainPCDoctor.Integration.Tests`)

Require Windows, real PDH counters.

```csharp
[Fact] void BackgroundLoop_Collects_ValidSnapshot()
// Start MonitoringWorker, collect one snapshot
// → CpuMetrics.TotalUtilizationPercent is between 0 and 100
// → MemoryMetrics.TotalPhysicalGb > 0
// → MemoryMetrics.AvailableGb < TotalPhysicalGb

[Fact] void RamPressure_Detected_OnSyntheticPressure()
// Allocate enough memory to trigger threshold (synthetic test)
// → Incident confirmed within 3 minutes
// (Note: this test may be hardware-dependent; skip if system has > 64 GB RAM)

[Fact] void ProcessCollector_Returns_ProcessNames()
// Collect processes
// → At least 5 processes; all have non-empty names; no paths or arguments

[Fact] void MigrationRunner_IsIdempotent()
// Run migrations twice on same database
// → No error; schema_migrations has exactly one row per version
```

---

## Manual Test Checklist

Executed during release validation:

| Test | Pass Criteria |
|---|---|
| Install on clean Windows 10 21H2 | Installs without error; tray icon appears; no UAC prompt |
| Install on clean Windows 11 23H2 | Same as above |
| Start with Windows | Enable setting; restart PC; app is in tray after login |
| RAM pressure notification | Simulate by opening many apps; Level 2 toast appears |
| CPU notification | Simulate sustained CPU load; **no toast** (CPU is silent in V1) |
| Why Was My PC Slow? | Query last 15 min with a known incident; correct bottleneck type returned |
| System Capacity — CPU rec | After sufficient history; recommendation badge appears in CPU section |
| System Capacity — GPU | GPU section shows monitoring status only; no recommendation badge |
| System Capacity — Disk | Disk section shows monitoring status only; no recommendation badge |
| Settings — clear history | Confirmation dialog shown; data cleared; incident list empty |
| Uninstall | Uninstaller removes all files and HKCU registry entries |
| 24-hour stability run | No crash; RAM < 150 MB; log has no ERROR entries |
| No elevated process | Verify app runs without UAC elevation |
| Dashboard close — process stays alive | Close Dashboard window; verify tray icon still present; verify monitoring continues (new log entries) |
| Dashboard reopen after close | Close Dashboard; reopen from tray; window appears correctly |
| Exit from tray — clean shutdown | Select Exit; verify process gone within 10s; verify DB intact |
| Windows logoff — graceful shutdown | Initiate logoff; verify app shuts down within 10s; DB intact |

---

## Not Tested in V1

- GPU notification tests (GPU incidents are always silent; no notification path to test)
- Disk upgrade recommendation tests (no disk upgrade rec in V1)
- Process disk I/O tests (no disk I/O property on Process; feature not implemented)
- 1-hour aggregate tests (table removed in V1)
