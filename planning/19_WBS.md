# 19 — Work Breakdown Structure
*Revised: 2026-09-17 — Gate Review 01*

---

## WBS Summary

| Phase | Name | Tasks |
|---|---|---|
| Phase 0 | Foundation | 5 |
| Phase 1 | Windows Collectors | 6 |
| Phase 2 | Diagnosis Engine | 9 |
| Phase 3 | Storage | 5 |
| Phase 4 | Background, Tray & Notifications | 8 |
| Phase 5 | Desktop UI | 8 |
| Phase 6 | Integration, QA & Release | 6 |
| **Total** | | **47** |

**Previous WBS:** 13 phases, 125 tasks.
**Reduction:** −6 phases, −78 tasks.

Each task is independently completable with a clear done condition. No calendar estimates are included — sequencing is explicit through dependencies listed per phase.

---

## Phase 0 — Foundation

*Prerequisite for all other phases.*

| ID | Task | Done Condition |
|---|---|---|
| 0.1 | Create solution with 4-project structure: Core (net8.0), Storage (net8.0), Platform.Windows (net8.0-windows), Desktop (net8.0-windows) | `dotnet build` succeeds with 0 warnings |
| 0.2 | Define Core models: `SystemSnapshot`, `CpuMetrics`, `MemoryMetrics`, `DiskMetrics`, `GpuMetrics`, `ProcessMetric`, `ProcessSummary`, `Incident`, `DiagnosisResult`, `BottleneckType`, `UpgradeRecommendation` | All types compile; XML docs on public members |
| 0.3 | Define Core interfaces: `ISystemMetricsCollector`, `IIncidentStore`, `IDiagnosisEngine`, `INotificationService` | Interfaces defined; all referenced by at least one test stub |
| 0.4 | Implement `RollingMetricsBuffer` (circular buffer, thread-safe read/write, configurable capacity) | Unit tests: add → read → evict when full; all green |
| 0.5 | Configure Generic Host in Desktop; wire Serilog to rolling file sink; create xUnit test projects for Core, Storage, Integration; verify `dotnet test` finds all 3 test projects | All 3 test runners report 0 tests / 0 failures (no tests yet; wired correctly) |

---

## Phase 1 — Windows Collectors

*Requires Phase 0. All collectors implement interfaces from Phase 0.3.*

| ID | Task | Done Condition |
|---|---|---|
| 1.1 | Implement `WindowsCpuCollector`: PDH total + per-core utilization; WMI base clock (read once at startup) | Unit-level: constructor does not throw on Windows; values are plausible (0–100%) |
| 1.2 | Implement `WindowsMemoryCollector`: `GlobalMemoryStatusEx` for available/total; PDH for commit bytes and commit limit; compute `CommitChargeRatio` and `PagefilePressureProxy` | Values plausible; `TotalPhysicalGb` matches Task Manager at ±0.2 GB |
| 1.3 | Implement `WindowsDiskCollector`: PDH wildcard `PhysicalDisk(*)` for utilization, latency, and queue; graceful null for all fields when PDH counter unavailable | Values plausible; no exception when disk is removed mid-run |
| 1.4 | Implement `WindowsGpuCollector`: PDH GPU Engine utilization; PDH Dedicated Usage for VRAM in use; DXGI `IDXGIAdapter.GetDesc().DedicatedVideoMemory` for VRAM total; LHM GPU temp (best-effort, null if unavailable) | VRAM total from DXGI > 0 on systems with discrete GPU; gracefully returns null struct when no GPU present |
| 1.5 | Implement `WindowsProcessCollector`: top 5 by CPU + top 5 by RAM via `System.Diagnostics.Process`; enforce 30-second rate limit in `WindowsSystemMetricsCollector` | Returns within 50ms for machines with 400+ processes; no disk IO field present |
| 1.6 | Implement `WindowsSystemMetricsCollector`: orchestrate all 5 collectors into `SystemSnapshot`; add LHM CPU temp (best-effort, null if unavailable); integration test: collect 1 snapshot, assert all numeric fields are plausible | Integration test green on Windows; all fields populated or explicitly null on unavailable sensor |

---

## Phase 2 — Diagnosis Engine

*Requires Phase 0. Runs entirely on `net8.0` with mock data — no Windows dependency.*

| ID | Task | Done Condition |
|---|---|---|
| 2.1 | Implement `DiagnosisThresholds` record with all configurable defaults (CPU, RAM, Disk, VRAM, GPU, Thermal, Process thresholds) | Record is immutable; all values have documented defaults; no magic numbers in rules |
| 2.2 | Implement `RamPressureRule`: 2-of-3 signal logic, capacity-aware threshold `max(2.0, totalRam * 0.05)`, 120s confirmation gate | Unit tests: no incident on single signal; confirmed on 2-of-3 sustained 120s; threshold correct for 4GB and 64GB systems |
| 2.3 | Implement `CpuBottleneckRule`: 300s gate, 85% total + 50% core count + RAM headroom guard + disk headroom guard; `NotificationLevel = 0` | Unit tests: no incident at 240s; confirmed at 300s; suppressed when RAM also constrained; always silent |
| 2.4 | Implement `DiskBottleneckRule`: latency-gated; return `INSUFFICIENT_EVIDENCE` when latency data is null; 90s gate | Unit tests: INSUFFICIENT_EVIDENCE on null latency; confirmed on all 3 signals present; no incident on high util + low latency (NVMe) |
| 2.5 | Implement `VramPressureRule`, `GpuComputeRule`, `ThermalThrottlingRule`: all diagnostic-only (`NotificationLevel = 0`); VRAM 180s gate; GPU always silent; Thermal disabled gracefully when temp sensor null | Unit tests: all confirm correctly; all return Level 0; Thermal gracefully disabled |
| 2.6 | Implement `ProcessAnomalyRule`: AND condition (resource abnormal AND growth pattern sustained 120s); name captured; no notification | Unit tests: no incident on resource-only (build process); incident on sustained resource + growth |
| 2.7 | Implement `DiagnosisEngine`: run all rules, apply priority ordering, return primary + secondary findings | Unit tests: RAM beats CPU when both fire; ProcessAnomaly is always secondary; null primary when nothing fires |
| 2.8 | Implement `UpgradeRecommendationEngine`: CPU + RAM only; evidence score model; confidence thresholds; 14-day minimum; 30-day cooldown; corroborating evidence suppression for CPU | Unit tests: no rec at < 7 days; Medium/High at correct score/day thresholds; GPU history produces no rec |
| 2.9 | Complete all Core unit tests; all green, all boundary cases covered | `dotnet test MainPCDoctor.Core.Tests` → 100% pass |

---

## Phase 3 — Storage

*Requires Phase 0.*

| ID | Task | Done Condition |
|---|---|---|
| 3.1 | SQLite database setup: `DatabaseFactory` creates WAL-mode database at `%LOCALAPPDATA%\MainPCDoctor\data\metrics.db`; `MigrationRunner` applies `001_InitialSchema.sql` on startup; idempotent | `MigrationRunner` applies migration once; second run is a no-op; all tables exist with correct schema |
| 3.2 | Write `001_InitialSchema.sql`: all 6 tables (`metrics_samples_1min`, `incidents`, `incident_processes`, `upgrade_recommendations`, `settings`, `schema_migrations`) with indexes | Schema matches `14_DATA_MODEL.md` exactly; no `metrics_samples_1hour` table |
| 3.3 | Implement `SQLiteMetricsStore` + `MetricsAggregator`: buffer raw snapshots; flush 1-minute aggregate every 60s | Unit test: 10 snapshots → 1 flush → 1 row in DB with correct avg/min/max |
| 3.4 | Implement `SQLiteIncidentStore`: save, update, query by type, query by date range, `RecoverOrphans()` | Unit tests: orphan recovery marks stale Active incidents as Resolved; cascade delete on incident_processes |
| 3.5 | Implement `UpgradeRecommendationStore` + `RetentionManager` | Unit tests: retention purges rows outside window; recommendation cooldown check works |

---

## Phase 4 — Background, Tray & Notifications

*Requires Phases 1, 2, 3.*

| ID | Task | Done Condition |
|---|---|---|
| 4.1 | Implement `SamplingScheduler` (3 states: Eco/Normal/Incident) + `ResourceGovernor` (self-CPU check, skip cycle if > 3%) | Unit tests: state transitions correct; governor skips cycle and logs when threshold exceeded |
| 4.2 | Implement `MonitoringWorker` (`IHostedService`): full sample loop with scheduler, governor, collector, buffer, diagnosis, aggregator flush | Integration smoke test: worker starts, collects 3 snapshots, stops cleanly |
| 4.3 | Implement `IncidentTracker`: candidate → confirmed lifecycle, 10-minute dedup merge, multi-type simultaneous incidents, orphan recovery delegation | Unit tests: no duplicate incidents within merge window; separate incidents for different types; orphan recovery called on startup |
| 4.4 | Implement `StartupRegistrar`: register/unregister HKCU Run key | Manual test: key present after `Register()`; key absent after `Unregister()` |
| 4.5 | WPF app host setup: `[STAThread] Main()` with `host.Start()` → `new App(host.Services).Run()` → `host.StopAsync(10s)`; `ShutdownMode = OnExplicitShutdown`; `MainWindow.OnClosing` hides window; `TrayController.Exit` calls `Application.Current.Shutdown()`; `App.OnSessionEnding` calls `Shutdown()` | App starts; tray icon appears; X hides window (process alive, monitoring continues); Exit from tray terminates process; logoff triggers graceful shutdown |
| 4.6 | Implement `TrayController`: `NotifyIcon` with 3 states (healthy green / RAM warning amber / paused grey); `ContextMenuStrip` (Open Dashboard, Latest Incident, Pause, Settings, Exit) | Tray icon appears; context menu items route to correct screens; Pause toggles monitoring |
| 4.7 | Implement `WindowsNotificationService`: AUMID-based `ToastNotificationManager`; Level 2 RAM warning toast; Level 4 upgrade rec toast; toast click routes to correct screen | Manual test: Level 2 toast fires on synthetic RAM pressure; clicking opens Incident Detail |
| 4.8 | Crash recovery integration: on startup, `RecoverOrphans()` closes Active incidents older than 10 min; PDH queries re-initialize fresh | Integration test: kill app while incident Active; restart; incident is Resolved with OrphanClosed |

---

## Phase 5 — Desktop UI

*Requires Phase 4 for data; can begin Views/ViewModels with mock data from Phase 2.*

| ID | Task | Done Condition |
|---|---|---|
| 5.1 | XAML design system resources: color tokens (`SurfaceBase`, `AccentCyan`, `WarningAmber`, `DangerRed`, `TextPrimary`, etc.); typography; status badge styles; scanline texture overlay | All tokens defined in `App.xaml`; app renders correct dark theme at 1080p |
| 5.2 | Dashboard: `ResourceTile` control with live data binding (CPU, RAM, Disk, GPU); system status headline; recent incidents list (last 3) | Tiles update each sample; correct values; status headline reflects active incident state |
| 5.3 | Why Was My PC Slow? screen: time window selector; analysis trigger; result display (primary bottleneck + confidence + contributing signals + top processes) | Returns correct bottleneck type for a window containing a known incident; "No issues found" for clean window |
| 5.4 | Incident List screen: all incidents sorted newest-first; filter by Today/This Week/All; filter by type | Filters work; clicking row navigates to detail |
| 5.5 | Incident Detail screen: all fields per `08_INCIDENT_MODEL.md`; top processes table (CPU + RAM columns, no disk I/O column) | All fields render; no empty fields without "Unavailable" labels |
| 5.6 | System Capacity screen: CPU section (spec + recommendation badge or "monitoring" status); RAM section (same); GPU/VRAM section (monitoring status only — utilization and health summary, no recommendation badge); Disk section (monitoring status only) | GPU and Disk sections show no upgrade recommendation badge; CPU/RAM sections correctly show badge when threshold met |
| 5.7 | Settings screen: all 4 settings with correct persistence; Clear History with confirmation dialog; Pause toggle wired to MonitoringWorker | Settings survive app restart; Clear History clears DB; Pause stops sample loop |
| 5.8 | Glitch effects: logo animation (CRT flicker on load); incident transition effect (brief scanline glitch when severity changes); diagnostic scan bar on Why Slow? screen | Effects render at 60fps; no frame drops on idle; no effect on accessibility (not blocking) |

---

## Phase 6 — Integration, QA & Release

*Requires all prior phases.*

| ID | Task | Done Condition |
|---|---|---|
| 6.1 | End-to-end test: install → background starts → simulate RAM pressure → Level 2 toast → click toast → Incident Detail shows correct data | All steps pass on clean Windows 10 21H2 VM |
| 6.2 | End-to-end test: Why Was My PC Slow? against real 15-minute history window containing a recorded incident | Correct bottleneck type and contributing processes returned |
| 6.3 | Developer performance regression tests (from `16_PERFORMANCE_BUDGET.md`): sample overhead, diagnosis latency, flush latency, memory leak, process collector overhead | All assertions pass |
| 6.4 | 24-hour stability run: app running continuously with simulated normal workload | No crash; RAM < 150 MB; log has no ERROR lines; DB intact |
| 6.5 | Inno Setup installer: AUMID registration, Start Menu shortcut, HKCU Run key, all files to LOCALAPPDATA; uninstaller removes all traces | Clean install and uninstall on Windows 10 21H2 and Windows 11 23H2 |
| 6.6 | Manual test checklist execution (from `18_TEST_STRATEGY.md`) | All items marked Pass |
