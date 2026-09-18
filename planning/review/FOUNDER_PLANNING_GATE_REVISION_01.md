# Founder Planning Gate — Revision 01
**Date:** 2026-09-17
**Authorized by:** Founder (REVISION GO authorization)
**Scope:** Planning documents only. No implementation. No code. No dependencies.

---

## A. Revision Status

**REVISION COMPLETE.**

All 14 documents specified in the REVISION GO authorization have been updated. One revision record created. The planning pack is consistent and ready for re-review.

---

## B. Files Modified

| File | Change Summary |
|---|---|
| `05_SYSTEM_ARCHITECTURE.md` | Merged Background project into Desktop; 4-project structure (was 5); removed IPC diagram; updated DI registration to correct WPF host setup; updated data flow diagram |
| `06_MONITORING_MODEL.md` | Removed `DiskIoKbps` from ProcessMetric; VRAM total now from DXGI not WMI; process cadence 30s documented; PDH query lifecycle section added; `INSUFFICIENT_EVIDENCE` concept introduced for Disk latency |
| `07_DIAGNOSIS_RULES.md` | CPU gate 60s→300s; RAM threshold `max(2.0GB, totalRam×0.05)`; VRAM gate 60s→180s; GPU rule silent-only; Disk rule requires latency evidence (INSUFFICIENT_EVIDENCE); ProcessAnomaly AND logic; upgrade categories: CPU+RAM only |
| `08_INCIDENT_MODEL.md` | Removed disk_io_kbps from IncidentProcess model; GPU incidents documented as silent; no notification level > 0 for GPU |
| `09_UPGRADE_RECOMMENDATION_MODEL.md` | V1 scope: CPU + RAM only; GPU/VRAM/Disk/Thermal upgrade recommendation sections removed; System Capacity non-upgrade component display documented |
| `10_NOTIFICATION_POLICY.md` | Removed Level 1 and Level 3; CPU Level 2 removed (CPU always silent); Level 2 = RAM pressure only; Level 4 = CPU or RAM upgrade rec only; tray icon: amber only for RAM |
| `11_BACKGROUND_RUNTIME.md` | Watch Mode removed; 3-state machine (Eco/Normal/Incident); FileSystemWatcher removed; `AddWpfBlazorServices()` removed; correct `UseWpfLifetime()` pattern documented; Background components in Desktop |
| `13_SCREEN_SPEC.md` | System Capacity: GPU/Disk show monitoring status only (no rec badge); CPU/RAM show recommendation or monitoring status; process table has CPU+RAM columns only (no disk I/O column) |
| `14_DATA_MODEL.md` | Removed `metrics_samples_1hour` table; removed `disk_io_kbps` from `incident_processes`; upgrade_recommendations documented as CPU+RAM only; schema aligned with revised rules |
| `16_PERFORMANCE_BUDGET.md` | "Benchmark Targets" section renamed to "Developer Performance Regression Tests"; process enumeration rate-limiting documented; PDH warm-up behavior documented |
| `17_WINDOWS_TECHNICAL_PLAN.md` | VRAM total fixed: DXGI `IDXGIAdapter.GetDesc().DedicatedVideoMemory` (not WMI); WPF host setup corrected (removed invalid `AddWpfBlazorServices()`); 4-project TFM table; PDH lifecycle section added; no-disk-IO note for Process API |
| `18_TEST_STRATEGY.md` | Test structure aligned with 4-project solution; removed GPU notification tests; removed disk I/O process tests; removed 1-hour aggregate tests; added INSUFFICIENT_EVIDENCE test; ProcessAnomaly AND logic test added |
| `19_WBS.md` | **Major revision:** 13 phases / 125 tasks → 7 phases / 47 tasks; all tasks independently completable with explicit done conditions |
| `20_MVP_SCOPE.md` | Removed Top by Disk from Included; removed GPU/VRAM/Disk/Thermal upgrade recs from Included; added to Not in V1; AC-06 updated (no disk column); AC-07 corrected (300s gate); AC-08 split (CPU silent, RAM notifies); AC-10 corrected (CPU+RAM only); AC-14 updated (GPU/Disk: no rec badge) |
| `README.md` | Updated status, document summaries, key decisions table |

---

## C. WBS Size: Old → New

| Metric | Before | After | Reduction |
|---|---|---|---|
| Phases | 13 | 7 | −6 |
| Tasks | 125 | 47 | −78 (−62%) |
| Calendar estimates | None (was absent) | None (still absent) | — |

**Target from REVISION GO:** 6–8 phases, 40–55 tasks. **Result: 7 phases, 47 tasks. Within target.**

The reduction was achieved by:
1. Removing over-specified subtasks (e.g. separate tasks per WMI query, per PDH counter, per test file)
2. Bundling naturally cohesive work (e.g. all diagnostic-only rules into one task)
3. Removing tasks for features not in V1 (Watch Mode, FileSystemWatcher, 1-hour aggregates, process disk I/O, GPU/Disk upgrade recommendation)
4. Removing calendar scaffolding tasks (no sprint planning or timeline tasks in the WBS)

---

## D. Removed V1 Scope

The following items were removed from V1 scope as part of this revision:

| Item Removed | Category | Reason |
|---|---|---|
| GPU upgrade recommendation | Scope | Requires external market data; actionability unclear without per-title VRAM requirements |
| VRAM upgrade recommendation | Scope | Same as GPU |
| Disk upgrade recommendation | Scope | Upgrade type (speed vs. space vs. controller) requires deeper diagnosis |
| Thermal upgrade recommendation | Scope | Thermal issues primarily have non-hardware solutions |
| "Top by Disk" process ranking | API reality | `System.Diagnostics.Process` has no disk I/O properties |
| Level 1 (informational trend) notifications | Scope | Low-value complexity; deferred to V2 |
| Level 3 (pattern notice) notifications | Scope | CPU pattern feeds Level 4 directly; separate level is unnecessary in V1 |
| CPU Level 2 (immediate warning toast) | Rule change | 300s gate + no immediate warning; builds would be noisy |
| Watch Mode (4th sampling state) | Architecture | 3-state machine is sufficient; Watch Mode added complexity with no measurable benefit |
| FileSystemWatcher for settings | Architecture | Settings apply on restart; no live reload needed in V1 |
| `metrics_samples_1hour` table | Data model | No V1 consumer; unnecessary complexity |
| Background as separate project | Architecture | Hosted service in Desktop; no independent consumer |

---

## E. Critical Corrections Made

These were bugs in the planning documents, not scope decisions:

| Bug | Fix |
|---|---|
| `Win32_VideoController.AdapterRAM` UINT32 overflow on GPUs > 4 GB | Replaced with DXGI `IDXGIAdapter.GetDesc().DedicatedVideoMemory` (UINT64) in docs 06, 17 |
| `AddWpfBlazorServices()` — method does not exist | Removed from docs 11, 17; replaced with correct `UseWpfLifetime()` |
| Process disk I/O (`DiskIoKbps`) — `System.Diagnostics.Process` has no such property | Removed from docs 06, 08, 14, 18, 20 |
| CPU confirmation gate 60s too short for developer builds | Raised to 300s in docs 07, 08, 18, 20 |
| WBS claimed "~80 tasks" but had 125 | Recounted; redesigned to 47 tasks |
| `IncidentTracker` in separate Background project | Moved to Core (rules) and Desktop (background worker) appropriately |

---

## F. Consistency Audit Result

Cross-document consistency verified for all revised files:

| Check | Result |
|---|---|
| CPU gate (300s) | Consistent: docs 07, 08, 18, 20 |
| RAM threshold formula | Consistent: docs 07, 06 |
| VRAM gate (180s) | Consistent: docs 07, 06 |
| GPU always silent | Consistent: docs 07, 08, 10, 13, 20 |
| Upgrade recs = CPU + RAM only | Consistent: docs 07, 09, 10, 13, 14, 20 |
| DXGI for VRAM total | Consistent: docs 06, 07, 14, 17 |
| 3-state sampling | Consistent: docs 06, 11, 16, 20 |
| No disk I/O on Process | Consistent: docs 06, 08, 13, 14, 18, 20 |
| No 1-hour table | Consistent: docs 06, 11, 14 |
| 4-project structure | Consistent: docs 05, 11, 17, 18, 19 |
| WPF host setup (UseWpfLifetime) | Consistent: docs 05, 11, 17 |
| Process cadence 30s | Consistent: docs 06, 11, 16, 17 |

---

## G. Remaining Risks

These risks are known but accepted for V1:

| Risk | Likelihood | Mitigation |
|---|---|---|
| LHM temperature sensors unavailable on user hardware | Medium | Graceful null; ThermalRule disabled; no error shown |
| PDH GPU counters absent (WDDM < 2.5 or older GPU drivers) | Low | Graceful null; GpuComputeRule and VramPressureRule disabled |
| AUMID registration fails silently (notification API mismatch) | Low | StartupRegistrar verifies AUMID on startup; logs warning |
| ProcessCollector > 50ms on machines with 600+ processes | Low | 30s cadence and ResourceGovernor limit impact; monitored by regression test |
| DXGI returns 0 for integrated-GPU-only systems | Medium | VRAM rule disabled when total = 0; UI shows "No discrete GPU detected" |
| RAM pressure threshold too high on 4 GB systems (cap = 2.0 GB) | Low | 2.0 GB threshold on a 4 GB machine = 50%; rule fires when RAM is genuinely scarce |

---

## H. Final V1 Scope Loop

**V1 does exactly this:**

```
Windows startup
    → HKCU Run key launches MainPCDoctor.exe
    → PDH queries initialized (CPU, Memory, Disk, GPU)
    → Sensor probed (LHM, DXGI)
    → Monitoring loop starts (Normal: 10s interval)

Per cycle:
    → Collect SystemSnapshot
    → Buffer in RollingMetricsBuffer
    → DiagnosisEngine evaluates last 300 seconds

On RAM pressure confirmed (2-of-3 signals, 120s):
    → Incident saved to SQLite
    → Level 2 toast: "Memory pressure detected"
    → Tray icon → amber

On CPU bottleneck confirmed (all 4 signals, 300s):
    → Incident saved to SQLite (SILENT)

On any other incident (GPU, VRAM, Disk, Thermal, Process):
    → Incident saved to SQLite (SILENT)

Every 60 seconds:
    → 1-minute aggregate flushed to metrics_samples_1min

User opens app (dashboard):
    → Live resource tiles
    → Recent incidents
    → "Why Was My PC Slow?" button

User asks "Why Was My PC Slow?":
    → Selects time window
    → DiagnosisEngine runs against stored history
    → Returns primary bottleneck + contributing signals + processes

After ≥ 14 days, if CPU evidence score ≥ 35:
    → Level 4 toast: "Capacity analysis available — CPU"
    → System Capacity screen shows CPU recommendation

After ≥ 14 days, if RAM evidence score ≥ 35:
    → Level 4 toast: "Capacity analysis available — RAM"
    → System Capacity screen shows RAM recommendation

GPU / VRAM / Disk / Thermal:
    → Always in history and "Why Slow?"
    → Never a notification
    → Never a recommendation
    → System Capacity shows monitoring status only
```

---

## I. What Was NOT Changed

Per REVISION GO instructions, documents 01, 02, 03, 04, 12, and 15 were not revised:

| Document | Reason Unchanged |
|---|---|
| `01_PRODUCT_DEFINITION.md` | No issues identified in Gate Review |
| `02_PRODUCT_PRINCIPLES.md` | Principles still hold; no revision needed |
| `03_USER_FLOW.md` | User flows are correct; minor details propagate from revised screens |
| `04_INFORMATION_ARCHITECTURE.md` | Navigation structure unchanged |
| `12_VISUAL_DESIGN_SYSTEM.md` | No design changes in this revision |
| `15_PRIVACY_AND_SECURITY.md` | No changes to privacy model |

---

---

## J. Final Correction — WPF Application Lifetime (Post-Revision-01)

**Date:** 2026-09-17 (same session)

### Issue Found

`UseWpfLifetime()` was used as the WPF + Generic Host integration method in docs 05, 11, 17. This is **not a standard Microsoft API**. It is provided by third-party packages (`Dapplo.Microsoft.Extensions.Hosting.Wpf`, `ReactiveMarbles.Extensions.Hosting.Wpf`, etc.). V1 must not add such a dependency.

### Correct Pattern (no third-party package required)

```
[STAThread] Main()
  host.Start()                    ← MonitoringWorker starts on background thread
  new App(host.Services).Run()    ← WPF event loop blocks on STA thread
  host.StopAsync(10s)             ← stops monitoring, flushes SQLite
  host.Dispose()
```

WPF shutdown policy:
- `ShutdownMode = OnExplicitShutdown` — process does not exit when windows close
- `MainWindow.OnClosing` cancels close, calls `Hide()` — window hides, monitoring continues
- `TrayController.OnExitClick` calls `Application.Current.Shutdown()` — only normal exit
- `App.OnSessionEnding` calls `Application.Current.Shutdown()` — handles logoff/shutdown
- `host.StopAsync(TimeSpan.FromSeconds(10))` — time-bounded; SQLite WAL is safe on forced exit

### Files Corrected

| File | Change |
|---|---|
| `05_SYSTEM_ARCHITECTURE.md` | Replaced `UseWpfLifetime()` with `[STAThread] Main()` + `host.Start()` + `App.Run()` + `host.StopAsync()` pattern; shutdown sequence documented |
| `11_BACKGROUND_RUNTIME.md` | Replaced Host Setup section; added WPF Shutdown Policy section (ShutdownMode, OnClosing hide behavior, exit triggers, shutdown sequence); updated Process Startup Flow diagram; corrected Main Window Lifecycle |
| `17_WINDOWS_TECHNICAL_PLAN.md` | Replaced WPF Application Host section; explicit prohibition of `UseWpfLifetime()` and third-party packages |
| `18_TEST_STRATEGY.md` | Added 4 manual test cases: Dashboard close (process alive), Dashboard reopen, Exit from tray, Windows logoff |
| `19_WBS.md` | Updated task 4.5 to reference correct lifetime pattern |
| `20_MVP_SCOPE.md` | Added AC-16 (Window Lifecycle and Shutdown) with explicit step-by-step test cases for all three shutdown scenarios |

### Consistency Status After Correction

- No `UseWpfLifetime()` reference remains anywhere in the planning pack
- Dashboard close does not terminate monitoring (OnClosing hides, ShutdownMode guards)
- Tray Exit performs actual app shutdown (`Application.Current.Shutdown()`)
- Windows logoff triggers graceful shutdown (`SessionEnding` handler)
- One user-session process — architecture unchanged
- No new workers, services, or IPC introduced
- 10-second bounded `StopAsync` prevents hung shutdown

---

## K. Next Step

This planning pack is ready for founder re-review and implementation authorization.

**To begin implementation**, the founder must explicitly issue an implementation authorization. The recommended starting point per `19_WBS.md` is Phase 0 (Foundation: solution setup, Core models, interfaces, RollingMetricsBuffer, DI/host scaffolding).
