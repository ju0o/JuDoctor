# Founder Planning Gate Review
## MainPC Doctor V1 — Pre-Implementation Critical Review

**Date:** 2026-09-17
**Status:** ⚠️ CHANGES_REQUIRED
**Reviewed:** 05, 06, 07, 09, 10, 11, 16, 17, 19, 20

---

## Overall Status

**CHANGES_REQUIRED**

The Planning Pack is directionally correct. The architecture, product principles, and core diagnosis model are well-designed. However, there are **5 blocking issues** and **9 non-blocking issues** that must be resolved before implementation.

**Blocking issues (must fix before coding):**
1. VRAM total measurement via `Win32_VideoController.AdapterRAM` overflows on GPUs > 4 GB — will return wrong values on the RTX 3050 6 GB mentioned in your own product spec
2. Process disk I/O is not accessible via `System.Diagnostics.Process` — the plan assumes an API that does not exist
3. CPU bottleneck 60-second threshold will generate incident notifications from normal developer builds
4. GPU compute rule will produce noise notifications during normal gaming sessions
5. WBS task count is wrong: document claims "~80 tasks" but contains 125

---

## Section A — Scope Audit

### Feature Classification

| Feature / Decision | Classification | Notes |
|---|---|---|
| `MainPCDoctor.Core` targeting `net8.0` (not windows) | REQUIRED_V1 | Real value: diagnosis rules testable without Windows; forces clean abstraction |
| `MainPCDoctor.Platform.Windows` as separate project | REQUIRED_V1 | Correct isolation of Windows API code |
| `MainPCDoctor.Storage` as separate project | REQUIRED_V1 | Clean boundary, testable |
| `MainPCDoctor.Desktop` (WPF) | REQUIRED_V1 | |
| `MainPCDoctor.Background` as separate project | DEFER_TO_POST_V1 | 4 files — MonitoringWorker, SamplingScheduler, ResourceGovernor, StartupRegistrar — add no architectural value as a separate DLL in V1. Merge into Desktop. |
| All 7 diagnosis rules | REQUIRED_V1 | Core product intelligence |
| UpgradeRecommendationEngine | REQUIRED_V1 | Core product promise |
| `RollingMetricsBuffer` | REQUIRED_V1 | |
| SQLite with WAL mode | REQUIRED_V1 | |
| 1-minute aggregate table | REQUIRED_V1 | Needed for Why Was My PC Slow? history |
| 1-hour aggregate table | DEFER_TO_POST_V1 | Upgrade engine reads incidents, not hourly aggregates. The only use case is the System Capacity view's "30-day pressure" which can be derived from incident records instead. Remove the table and the aggregation job in V1. |
| NVAPI supplemental GPU temperature | DEFER_TO_POST_V1 | PDH GPU counters are sufficient for V1. NVAPI adds dependency complexity for marginal gain. |
| LibreHardwareMonitor (CPU temp) | REQUIRED_V1 (best-effort) | Must be graceful if unavailable; thermal rule already handles null |
| Adaptive sampling (Normal/Incident/Recovery) | REQUIRED_V1 | Performance-critical |
| Watch Mode (4th sampling state, 5s interval) | DEFER_TO_POST_V1 | Adds state machine complexity. Two states (Normal + Incident) are sufficient. "Watch mode" is a premature optimization — go directly from Normal to Incident when threshold is first crossed. Simplify to 3 states. |
| Level 0 notifications (silent) | REQUIRED_V1 | The policy itself is required; no code needed |
| Level 1 notifications (weekly trend toasts) | DEFER_TO_POST_V1 | Requires computing weekly averages and session counts — non-trivial logic. Not core to the MVP loop. Not required to prove the product. |
| Level 2 notifications (active incident) | REQUIRED_V1 | |
| Level 3 notifications (repeat pattern) | SUPPORTING_IMPLEMENTATION_DETAIL | Requires multi-session pattern detection. Defer if timeline is tight. Core loop works without it. |
| Level 4 notifications (upgrade rec) | REQUIRED_V1 | |
| FileSystemWatcher for settings hot-reload | DEFER_TO_POST_V1 | Threading complexity, event debouncing required. Settings can apply on next app restart in V1. |
| History retention user-configurable (4 options) | DEFER_TO_POST_V1 | Hard-code 30-day retention in V1. One less setting, one less UI element, one less persistence concern. |
| 5 project abstractions (interfaces) | SUPPORTING_IMPLEMENTATION_DETAIL | Keep — critical for testability |
| ResourceGovernor (self-CPU skip) | REQUIRED_V1 | Performance contract enforcement |
| ProcessAnomalyRule | SUPPORTING_IMPLEMENTATION_DETAIL | Useful but the lowest-priority of the 7 rules. Can be deferred if timeline is under pressure. |
| HKCU Run key startup | REQUIRED_V1 | |
| `AddWpfBlazorServices()` in Program.cs | REMOVE | This method does not exist in WPF. WPF and Blazor are separate frameworks. The host setup for a WPF app hosting a BackgroundService does not use this method. Must be corrected before implementation. |

### Scope Violations Found

**SV-01 — Separate Background project is premature**
`MainPCDoctor.Background` is a 4-file project. In V1, these components can live in `MainPCDoctor.Desktop`. Keeping it separate adds build configuration, project reference overhead, and a deployed DLL with no architectural benefit in a single-process app. Merge into Desktop.

**SV-02 — 1-hour aggregate table is premature**
The only consumer of 1-hour aggregates would be "30-day pressure" in the System Capacity view. This data is already captured in incident records (each incident has a duration and timestamps). The upgrade recommendation engine works entirely from incident records. Delete `metrics_samples_1hour` and task 3.4, 3.8 from WBS.

**SV-03 — Level 1 notifications add implementation cost without MVP value**
Computing "memory usage has been unusually high during several sessions this week" requires session-boundary detection, historical comparison, and statistical analysis. This is not a trivial feature. It is not core to the MVP loop. Defer.

**SV-04 — WBS task count is wrong**
Document states "~80 tasks." Actual count: **125 tasks** across 13 phases. The discrepancy is not minor. See Section B.

---

## Section B — MVP Size Audit

### Task Count Discrepancy

| | Current | Actual | Δ |
|---|---|---|---|
| Tasks claimed in document | ~80 | 125 | **+45** |
| Phases | 12 | 13 (including Phase 0) | +1 |

**Actual task count by phase:**

| Phase | Current Tasks | After Removals | Notes |
|---|---|---|---|
| 0 — Foundation | 7 | 7 | Keep |
| 1 — Collectors | 11 | 10 | Remove 1.8 (NVAPI supplemental) |
| 2 — Diagnosis Engine | 13 | 11 | Remove 2.9 (ProcessAnomalyRule) if deferred |
| 3 — Storage | 11 | 9 | Remove 3.4+3.8 (1-hour aggregates) |
| 4 — Background Service | 10 | 9 | Remove 4.8 (FileSystemWatcher hot-reload) |
| 5 — Tray & Notifications | 13 | 11 | Remove 5.10 (Level 3 toast) + merge Background project removes Phase 5 scope boundary shift |
| 6 — Dashboard UI | 13 | 13 | Keep |
| 7 — Incidents & Capacity | 10 | 10 | Keep |
| 8 — Why Slow? | 8 | 7 | Remove 8.8 (previous analyses list) |
| 9 — Settings | 8 | 7 | Remove 9.6 (history retention selector — hard-code 30d) |
| 10 — Integration/Polish | 8 | 8 | Keep |
| 11 — Testing & QA | 7 | 7 | Keep |
| 12 — Installer | 6 | 6 | Keep |
| **Total** | **125** | **115** | |

**CURRENT_TASK_COUNT: 125**
**REDUCED_TASK_COUNT: 115**
**CURRENT_PHASE_COUNT: 13 (Phase 0–12)**
**REDUCED_PHASE_COUNT: 12 (merge Background project into Desktop, eliminating one project boundary)**

The reduction from 125 → 115 is modest (10 tasks). The more important reduction is in *complexity* — removing the 1-hour aggregate table, FileSystemWatcher, Watch mode, and Level 1/3 notifications simplifies 5-6 components that are each small but add non-trivial surface area.

At 115 tasks, with the described phase dependencies, a solo founder doing 5-6 tasks per week faces a **20–23 week timeline**, not 12–16. The 12–16 week estimate assumes a pace that was plausible only if the task count were actually ~80. The timeline in 19_WBS.md must be revised.

---

## Section C — Windows Telemetry Reality Check

### Metric Feasibility Classification

| Metric | Classification | Notes |
|---|---|---|
| CPU total utilization % | **RELIABLE** | PDH `\Processor(_Total)\% Processor Time` — exact match to Task Manager |
| CPU per-core utilization % | **RELIABLE** | PDH `\Processor(N)\%` — works reliably |
| CPU clock speed (current) | **BEST_EFFORT** | `Win32_Processor.CurrentClockSpeed` updates infrequently (every few seconds, polling granularity varies). Sufficient for "is it throttled?" detection over a window. |
| CPU base clock | **RELIABLE** | `Win32_Processor.MaxClockSpeed` — static value |
| CPU temperature | **HARDWARE_DEPENDENT** | LibreHardwareMonitor: works without elevation on some systems, requires WinRing0 kernel driver on others. Plan's graceful-null approach is correct. |
| RAM available | **RELIABLE** | `GlobalMemoryStatusEx.ullAvailPhys` — exact, kernel call |
| Commit charge | **RELIABLE** | PDH `\Memory\Committed Bytes` — accurate |
| Pagefile activity | **BEST_EFFORT** | `\Memory\Pages/sec` is a proxy. Not a clean boolean. Plan's flag approach (threshold > N samples) is the right implementation. |
| Disk utilization % | **BEST_EFFORT** | `% Disk Time` on SSDs is unreliable — can show 100% even when SSD is not saturated (time-based counter, not request-queue-based). Reliable on HDDs. Plan should prefer latency-based detection on SSDs. |
| Disk latency | **RELIABLE** | `\PhysicalDisk\Avg. Disk sec/Transfer` — accurate and more meaningful than utilization on SSDs |
| Disk per-drive | **RELIABLE** | Use `\PhysicalDisk(0 C:)\` counters. Note: plan says "per-drive" in monitoring model but `\PhysicalDisk(_Total)` in technical plan — inconsistency to resolve |
| GPU utilization % | **BEST_EFFORT** | PDH `\GPU Engine(*engtype_3D)\%` available on Windows 10 1803+. Works for NVIDIA/AMD/Intel. Only covers 3D engine (appropriate for the use case). Counter can be noisy. |
| GPU temperature | **HARDWARE_DEPENDENT** | NVAPI (NVIDIA only). AMD GPU temp requires LibreHardwareMonitor with potential driver requirement. Plan correctly marks as optional. |
| VRAM used | **BEST_EFFORT** | PDH `\GPU Process Memory(*)\Dedicated Usage` — summing across processes works on Windows 10 1803+ |
| VRAM total | **UNAVAILABLE_OR_UNRELIABLE** — **⚠️ BUG** | `Win32_VideoController.AdapterRAM` is a UINT32. On GPUs with > 4 GB VRAM it **overflows and returns a wrong value**. An RTX 3050 6 GB would return an incorrect number. Must use DXGI `DXGI_ADAPTER_DESC.DedicatedVideoMemory` (via DirectX) or LibreHardwareMonitor to get the correct value. This is a confirmed bug in the current technical plan. |
| Process CPU % | **BEST_EFFORT** | `Process.TotalProcessorTime` delta works but requires exception handling — AccessDeniedException for privileged processes, InvalidOperationException for exited processes. These must be caught. |
| Process RAM | **RELIABLE** | `Process.WorkingSet64` — available for user processes; returns 0 for inaccessible processes |
| Process disk I/O | **UNAVAILABLE_OR_UNRELIABLE** — **⚠️ GAP** | `System.Diagnostics.Process` does NOT expose disk I/O counters. The process API has no `.DiskReadBytes` or `.DiskWriteBytes` properties. To get per-process disk I/O you must use Win32 P/Invoke `GetProcessIoCounters(HANDLE, LPPROCESS_IO_COUNTERS)` or PDH per-process I/O counters. This must be explicitly planned. Omitting it means the "Top by Disk" process list cannot be built. |
| Thermal throttling (clock drop) | **HARDWARE_DEPENDENT** | Requires both CPU temp sensor AND per-core clock data from LibreHardwareMonitor. Both are HARDWARE_DEPENDENT. Rule correctly doesn't fire when sensors are null. |

### Critical Telemetry Bugs

**BUG-01 — VRAM total overflows for GPUs > 4 GB (17_WINDOWS_TECHNICAL_PLAN.md)**
`Win32_VideoController.AdapterRAM` is a UINT32. 6 GB = 6,442,450,944 bytes, which overflows UINT32 (max 4,294,967,295). The field returns garbage for your target GPU (RTX 3050 6 GB). Fix: use DXGI `IDXGIAdapter.GetDesc()` to read `DedicatedVideoMemory` as UINT64, or use LibreHardwareMonitor's GPU memory total.

**BUG-02 — Process disk I/O is a non-existent API (06_MONITORING_MODEL.md, 17_WINDOWS_TECHNICAL_PLAN.md)**
The plan shows `ProcessMetric.DiskIoKbps` and lists `System.Diagnostics.Process` as the source. This API does not provide disk I/O. Options: (a) use Win32 `GetProcessIoCounters` via P/Invoke, (b) use PDH per-process I/O counters, (c) remove the "Top by Disk" process list from V1. Option (c) is the safest for a tight MVP — show only Top by CPU and Top by RAM, defer Top by Disk.

---

## Section D — Diagnosis Safety Audit

### Per-Rule False Positive Analysis

---

#### RAM_PRESSURE_RULE

**Required signals:** AvailableGb < 2.0, CommitPressure > 85%, duration ≥ 120s, pagefile flag
**Duration gate:** 120 seconds — ADEQUATE
**Repeat requirement for upgrade:** score ≥ 35 over 14 days — ADEQUATE

**Risk: Fixed 2.0 GB threshold does not scale with installed RAM**

On a 16 GB system, 2.0 GB available = 12.5% free — genuinely low.
On a 64 GB system, 2.0 GB available = 3.1% free — catastrophically low.
On a 128 GB workstation, this threshold is essentially never useful.

A 64 GB system with 6 GB available is under no RAM pressure, but the current rule would not fire. That's fine. But a 64 GB system with 2.5 GB available might actually be under significant pressure — and the 2.0 GB hard threshold would miss it.

**Required fix:** The threshold should scale with installed RAM. Proposed: `max(2.0 GB, totalRam * 0.05)`. Examples:
- 16 GB system: max(2.0, 0.8) = 2.0 GB threshold
- 32 GB system: max(2.0, 1.6) = 2.0 GB threshold
- 64 GB system: max(2.0, 3.2) = 3.2 GB threshold
- 128 GB system: max(2.0, 6.4) = 6.4 GB threshold

This rule change also affects the upgrade recommendation "minutes below threshold" calculation.

---

#### CPU_BOTTLENECK_RULE

**Required signals:** CPU > 85% in 70% of samples, duration ≥ 60s, RAM OK, Disk OK
**Duration gate:** 60 seconds — **TOO SHORT**
**Repeat requirement for upgrade:** score ≥ 35 over 14 days — ADEQUATE for recommendation

**Risk: Developer workloads will generate notification spam**

A `dotnet build` of a medium-sized solution regularly takes 90–180 seconds and can saturate all CPU cores at > 90%. Under the current rules, every significant build triggers a confirmed CPU bottleneck incident and a Level 2 notification.

A developer doing 20 builds per day would receive 20 CPU incident notifications.
A developer doing `npm run build` (typically 60–180 seconds) would also trigger this.
Video encoding, compilation, and data processing are all expected workloads that legitimately peg the CPU.

**The fundamental issue:** CPU bottleneck for upgrade purposes requires ruling out that the user is simply running an expected CPU-intensive task. The app cannot know whether the user started a build intentionally. However, it can require a longer duration, which filters out most builds:

- 60 seconds: catches every serious build → too noisy
- 5 minutes: catches sustained bottleneck during "normal workflow," passes most builds
- 10 minutes: very conservative, but almost never false-positive

**Required fix:** Increase `CpuSustainedSeconds` default from 60 to 300 (5 minutes). This is the single most important threshold change. It eliminates notification spam for developer workloads. Builds that take > 5 minutes on a particular CPU are genuinely indicating a sustained bottleneck.

Also: the notification policy should not fire Level 2 for a CPU incident that resolves in under 5 minutes. The incident can be recorded for history, but the notification threshold for CPU should be higher than the incident confirmation threshold.

---

#### GPU_COMPUTE_RULE

**Required signals:** GPU > 95% in 70% of samples, duration ≥ 60 seconds, VRAM OK
**Duration gate:** 60 seconds — **TOO SHORT AND WRONG THRESHOLD**
**Repeat requirement for upgrade:** score ≥ 35 over 14 days

**Risk: Gaming produces continuous GPU incidents — this is a fundamental design error**

A user playing a demanding game for 3 hours would experience:
- GPU at 97% for the entire session
- 60-second threshold crossed immediately
- One confirmed GPU compute incident created
- Level 2 notification: "GPU Compute Bottleneck — 3 hours — Resolved"
- Over 14 days of gaming: score rapidly accumulates toward 35
- Upgrade recommendation: "GPU compute capacity appears to be the primary limiting factor"

This is wrong. A GPU running at 99% during gaming is doing exactly what it is supposed to do. That is the product of the GPU working correctly. It is not evidence the GPU needs upgrading.

The app has no way to know whether the user's GPU is the limiting factor causing degradation, or whether the GPU is simply doing expected heavy work. A GPU at 99% with the game running smoothly at 60 FPS does not need upgrading.

**This is a serious product risk.** Incorrect GPU upgrade recommendations will destroy user trust.

**Required fix (choose one or combine):**
1. **Remove GPU compute rule from V1 entirely.** GPU upgrade recommendations are particularly unreliable without frame-timing or application context. VRAM pressure is more defensible (empty VRAM causes obvious stutter). Defer GPU compute bottleneck to V2.
2. **Raise the threshold significantly:** GPU > 98% for > 30 minutes continuously (not 60 seconds). This only catches truly extreme sustained saturation. Still doesn't distinguish gaming.
3. **Add an exclusion condition:** If `VramUsedGb / VramTotalGb > 0.5` (user is running a VRAM-consuming workload typical of games/3D apps), suppress GPU compute notification. This is an imperfect proxy.
4. **Separate the incident from the notification:** GPU compute incidents are recorded silently (no toast), only visible in history and contributing to the upgrade score. The upgrade recommendation is then the only user-facing output, and it requires the multi-signal/multi-day evidence bar.

**Recommended approach: Option 4 + raise duration threshold to 10 minutes.** Record GPU pressure in history with no immediate notification. Only surface it through the upgrade engine after sustained evidence.

---

#### VRAM_PRESSURE_RULE

**Required signals:** VRAM > 90% in 60% of samples, duration ≥ 60 seconds
**Duration gate:** 60 seconds — BORDERLINE

**Risk: Level loading in games triggers VRAM pressure incidents**

Many games temporarily fill VRAM during level loads or scene transitions. A level load might fill VRAM to 95% for 30–90 seconds. Under the current rule this creates an incident.

However, VRAM pressure is more defensible than GPU compute pressure for upgrade recommendations — actual VRAM overflow causes observable stuttering (texture streaming, asset eviction). The symptom is directly observable.

**Lower risk than GPU compute rule, but still needs a longer duration gate.**

**Required fix:** Increase VRAM duration threshold from 60 seconds to 3 minutes. This eliminates level-load false positives while retaining detection of genuinely sustained VRAM pressure.

Also: the VRAM total bug (BUG-01) must be fixed first — without an accurate `VramTotalGb`, the `VramUsedGb / VramTotalGb` ratio is meaningless.

---

#### DISK_BOTTLENECK_RULE

**Risk: `% Disk Time` on SSDs is unreliable**

Set A uses `\PhysicalDisk\% Disk Time`. On NVMe SSDs, this counter saturates easily (it measures the percentage of time the disk was busy, which hits 100% at even moderate IOPS on SSDs). This means Set A could trigger on a healthy, fast SSD with normal I/O.

**Required fix:** For Set A, add a guard: only evaluate `% Disk Time` threshold when `AvgLatencyMs > 5` (i.e., the disk also shows elevated latency, which indicates actual congestion, not just SSD being efficient). On a healthy NVMe drive, latency stays < 1ms regardless of utilization. This makes Set A effectively latency-gated on SSDs.

Alternatively: rely primarily on Set B (latency-based) for all drives, and only use Set A as a secondary signal on drives with known HDD characteristics (detectable from drive type: `DriveInfo.DriveType` doesn't expose this, but WMI `Win32_DiskDrive.MediaType` can distinguish HDD vs SSD).

---

#### THERMAL_THROTTLING_RULE

**Risk: Laptop CPUs throttling is by design**

Thin-and-light laptops are explicitly designed to thermal throttle under sustained load. For these devices, a "Thermal Throttling" incident is expected behavior, not a problem requiring action.

**Concern:** The incident description says "Thermal Throttling Detected" which implies something abnormal. For a laptop user, this is alarming but normal.

**Required fix:** The diagnosis output for thermal throttling should be worded differently based on context. If detected: "Thermal management is limiting CPU performance. This is normal behavior in many laptops under sustained load. Ensuring adequate airflow or reducing sustained load may help." Not: "Thermal Throttling — Critical — Detected."

No rule change needed; only the output text and recommendation category wording needs to distinguish "laptop thermal management" from "desktop CPU running too hot."

---

#### PROCESS_ANOMALY_RULE

**Risk: CPU-bound legitimate processes trigger false anomalies**

One process > 40% CPU is plausible for: a game, a video encoder, an AI inference task, a build process. This isn't anomalous — it's expected.

The RAM growth condition (> 500 MB in 10 minutes) is the stronger and more interesting signal. A process growing RAM rapidly is more reliably anomalous.

**Required fix:** The ProcessAnomalyRule should require BOTH conditions (high resource usage AND growth pattern), not either/or. "40% CPU OR 30% RAM" should be "40% CPU with RAM growth > 200 MB/10min OR 30% RAM with RAM growth > 500 MB/10min." This prevents false positives from legitimate CPU-bound work.

---

### Summary: Rules Safe for V1 as-is (with threshold fixes)

| Rule | V1 Safe? | Blocker |
|---|---|---|
| RamPressureRule | Fix threshold scaling | Fix 2.0 GB → scaled threshold |
| CpuBottleneckRule | Fix duration threshold | Raise 60s → 300s |
| DiskBottleneckRule | Fix Set A on SSDs | Add latency guard |
| ThermalThrottlingRule | Fix output text | Wording only |
| VramPressureRule | Fix duration + BUG-01 first | 60s → 3 minutes + VRAM total fix |
| GpuComputeRule | Major redesign required | See Section D above |
| ProcessAnomalyRule | Fix condition logic | AND not OR |

---

## Section E — Notification Audit

### Scenario Testing

| Scenario | Current Behavior | Expected Behavior | Verdict |
|---|---|---|---|
| npm build, CPU 100%, 40 seconds | No incident (< 60s duration gate) | Silent | ✅ Correct |
| `dotnet build`, CPU 95%, 90 seconds | **CPU incident created, Level 2 toast fires** | Silent (expected workload) | ❌ SPAM |
| Game running GPU at 99%, 3 hours | **GPU incident created, Level 2 toast fires** | Silent (expected workload) | ❌ SPAM |
| Chrome opens tabs, RAM 80%, 6 GB available | No incident (available > 2 GB) | Silent | ✅ Correct |
| RAM 97%, 620 MB available, 15 minutes | Incident + Level 2 notification | Notify — this is impactful | ✅ Correct |
| Virus scan, disk 95%, 20 minutes | **Disk incident created, Level 2 toast fires** | Debatable — user probably knows why | ⚠️ Arguable |
| VS Code + 30 browser tabs, RAM slowly climbs to 97% over 2 hours | Incident + Level 2 notification | Notify — user impact | ✅ Correct |
| Windows Update writing to disk, 10 minutes | **Disk incident may fire (Set A)** | Silent (expected OS activity) | ⚠️ Arguable |

**Key finding: Two scenarios generate spam notifications.**

**Notification Risk 1: CPU builds**
A `dotnet build` lasting 90 seconds with CPU > 85% crosses the 60-second confirmation threshold. This fires a Level 2 toast: "CPU Saturation — 90 seconds — Resolved." For a developer, this is useless and annoying. Fix: raise CPU incident confirmation to 300 seconds.

**Notification Risk 2: Gaming GPU utilization**
A game at GPU 99% for any session exceeds the 60-second GPU threshold. Notification: "GPU Compute Bottleneck — 2 hours." For a gamer, this is wrong. Fix: see GPU rule changes in Section D.

**Acceptable uncertainty: disk during virus scan / Windows Update**
These are borderline. Windows Update writing to disk for 10-20 minutes could trigger a disk incident. This is less harmful than a CPU/GPU false positive because disk pressure during a large write is real (even if expected). The notification provides accurate information, even if the user already knows why.

No fix required for disk scenario — it's an acceptable edge case.

---

## Section F — Performance Audit

### "Benchmark Suite" Naming Issue

**16_PERFORMANCE_BUDGET.md** contains a section titled "Benchmark Targets (to verify before shipping)."
**20_MVP_SCOPE.md** says "Benchmark suite — Not in scope."

These refer to **different things** and must be clarified:

- `20_MVP_SCOPE.md` "Benchmark suite" = a user-facing PC performance benchmark feature (like Cinebench/3DMark). **Correctly excluded from V1.**
- `16_PERFORMANCE_BUDGET.md` "Benchmark Targets" = developer performance regression tests using BenchmarkDotNet. **Required for V1 quality assurance.**

**Required fix:** Rename the section in `16_PERFORMANCE_BUDGET.md` from "Benchmark Targets" to "Developer Performance Regression Tests" to eliminate the ambiguity.

### Realistic Performance Assessment

| Budget Item | Target | Realistic? | Risk |
|---|---|---|---|
| Idle CPU < 0.5% (Balanced) | < 0.5% avg | ACHIEVABLE | PDH queries are lightweight at 10s intervals |
| Idle RAM < 80 MB (background only) | < 80 MB | TIGHT | .NET 8 runtime + minimal WPF host may be 50–70 MB baseline. LibreHardwareMonitor adds ~10–20 MB. 80 MB is tight but achievable. |
| Dashboard RAM < 120 MB | < 120 MB | ACHIEVABLE | WPF UI with modest data binding |
| Why Slow? analysis < 300ms | < 300ms | ACHIEVABLE | In-memory rule evaluation on 180-sample buffer |

**Performance Risk 1: `GetProcesses()` at every sample cycle**

`System.Diagnostics.Process.GetProcesses()` enumerates all running processes. On a typical system with 150–250 processes, this call takes **30–100ms** and involves kernel transitions for each process. At 10-second intervals, this could add 0.3–1% CPU usage just for process enumeration — a significant fraction of the total budget.

**Required fix:** Decouple process collection from the main sampling cycle. Collect process metrics every 30 seconds (or every 3rd sample at Balanced), not every sample. CPU and RAM metrics (the fast-changing ones) still sample at 10s. Process rankings change slowly enough that 30s is fine.

**Performance Risk 2: PDH query initialization**

PDH counters must be initialized before use. The plan implies collecting metrics per-sample but doesn't explicitly state that PDH queries are initialized once and kept open. If PDH queries are opened and closed per sample, this adds substantial overhead.

**Required fix:** Explicitly note in the architecture that all PDH queries are initialized once at startup (`PdhOpenQuery`, `PdhAddCounter`) and held open. Only `PdhCollectQueryData` + `PdhGetFormattedCounterValue` run per sample cycle. This must be called out in `06_MONITORING_MODEL.md` and `17_WINDOWS_TECHNICAL_PLAN.md`.

---

## Section G — Background Architecture Audit

### Does V1 Need a Separate Background Project?

**No.**

`MainPCDoctor.Background` contains 4 files: `MonitoringWorker`, `SamplingScheduler`, `ResourceGovernor`, `StartupRegistrar`. These are tightly coupled to the Desktop application's lifetime and have no independent consumers.

A separate project:
- Adds one more DLL to deploy
- Adds project references to maintain
- Provides no testability benefit (these components are tested via the integration test, not unit tests)
- Provides no architectural benefit in a single-process app

**Recommendation:** Merge `MainPCDoctor.Background` into `MainPCDoctor.Desktop`. The 4 files become part of the Desktop project, housed in a `Background/` subfolder. This matches what the architecture actually is: one process, background work hosted inside the UI process.

### Does V1 Need IPC?
No. Single process. No IPC needed. ✅

### Does V1 Need a Windows Service?
No. HKCU Run key startup is correct and requires no elevation. ✅

### Does V1 Need Elevation?
No. All required APIs work at user level. LibreHardwareMonitor degrades gracefully. ✅

### FileSystemWatcher for Settings Hot-Reload

The plan proposes using `FileSystemWatcher` to reload settings when `settings.json` changes. This requires:
- Event debouncing (multiple events fire per file write)
- Thread-safe settings update (watcher fires on thread pool)
- Error handling for locked files

**This is more complex than the MVP requires.** Simpler alternatives:
1. Settings changes take effect on the next full app restart
2. Settings changes are applied immediately when the user saves (the UI directly updates the in-memory settings object, no file watching)

Option 2 is better: the UI writes settings to both the in-memory object AND the JSON file simultaneously. No file watcher needed. The file is only read on startup. On next startup, settings are loaded from file.

**Remove `FileSystemWatcher` from V1.**

### Is the 5-Project Structure Worth It?

```
Core (net8.0)            ← Keep. Real testability value.
Storage (net8.0)         ← Keep. Clean boundary.
Platform.Windows         ← Keep. Correct isolation.
Background               ← Merge into Desktop.
Desktop                  ← Keep.
```

After merge: **4 source projects + test projects.** This is a clean, justified structure.

---

## Section H — Collected Issues

### Blocking Issues (must fix before coding)

| ID | Severity | Location | Issue |
|---|---|---|---|
| BLK-01 | BLOCKING | `17_WINDOWS_TECHNICAL_PLAN.md` | `Win32_VideoController.AdapterRAM` overflows on GPUs > 4 GB. RTX 3050 6 GB will return wrong VRAM total. Must use DXGI or LibreHardwareMonitor for VRAM total. |
| BLK-02 | BLOCKING | `06_MONITORING_MODEL.md`, `17_WINDOWS_TECHNICAL_PLAN.md` | Process disk I/O is not available via `System.Diagnostics.Process`. Must use Win32 P/Invoke `GetProcessIoCounters` or remove "Top by Disk" from V1. Recommended: remove "Top by Disk" from V1 scope. |
| BLK-03 | BLOCKING | `07_DIAGNOSIS_RULES.md` | CPU bottleneck 60-second confirmation threshold will generate notification spam from developer builds. Raise to 300 seconds. |
| BLK-04 | BLOCKING | `07_DIAGNOSIS_RULES.md` | GPU compute rule fires continuously during normal gaming. Rule must be silent-only (no L2 notification) or removed from V1 entirely. |
| BLK-05 | BLOCKING | `19_WBS.md` | WBS claims "~80 tasks" but contains 125. Timeline of 12–16 weeks is significantly underestimated. Document must be corrected. |

### Non-Blocking Issues (fix before or during implementation)

| ID | Severity | Location | Issue |
|---|---|---|---|
| NB-01 | HIGH | `07_DIAGNOSIS_RULES.md` | Fixed 2.0 GB available RAM threshold doesn't scale with installed RAM. Proposed formula: `max(2.0, totalRam * 0.05)`. |
| NB-02 | HIGH | `07_DIAGNOSIS_RULES.md` | VRAM pressure duration threshold too short (60s). Level loads in games will create incidents. Raise to 180 seconds. |
| NB-03 | HIGH | `07_DIAGNOSIS_RULES.md` | ProcessAnomalyRule condition "40% CPU OR 30% RAM" is too loose. Legitimate CPU-bound processes trigger it. Change to require both resource consumption AND growth pattern. |
| NB-04 | MEDIUM | `07_DIAGNOSIS_RULES.md` | DiskBottleneck Set A (`% Disk Time` > 90%) fires false positives on SSDs. Add guard: only evaluate Set A when `AvgLatencyMs > 5ms`. |
| NB-05 | MEDIUM | `11_BACKGROUND_RUNTIME.md` | `AddWpfBlazorServices()` is a non-existent method. WPF and Blazor are separate frameworks. Must be corrected. |
| NB-06 | MEDIUM | `05_SYSTEM_ARCHITECTURE.md` + `19_WBS.md` | `MainPCDoctor.Background` separate project is unnecessary for a single-process V1 app. Merge into Desktop. |
| NB-07 | MEDIUM | `16_PERFORMANCE_BUDGET.md` | "Benchmark Targets" section name collides with "Benchmark suite — Not in V1" in MVP scope. Rename to "Developer Performance Regression Tests." |
| NB-08 | MEDIUM | `06_MONITORING_MODEL.md` | `GetProcesses()` called at every sample cycle. Must be rate-limited to every 30 seconds minimum. Otherwise process collection alone may consume 0.5–1% CPU. |
| NB-09 | LOW | `17_WINDOWS_TECHNICAL_PLAN.md` | PDH query initialization must be done once at startup (not per sample). This must be explicitly documented in the implementation guidance. |

---

## Section I — Exact Files Requiring Modification

| File | Change Required |
|---|---|
| `05_SYSTEM_ARCHITECTURE.md` | Remove `MainPCDoctor.Background` as a separate project. Merge into `MainPCDoctor.Desktop`. Update project structure diagram. |
| `06_MONITORING_MODEL.md` | Mark process disk I/O as requiring P/Invoke (`GetProcessIoCounters`) or removed from V1. Add PDH query initialization lifecycle note. Add process collection rate-limiting note (every 30s, not every sample). |
| `07_DIAGNOSIS_RULES.md` | CPU: raise `CpuSustainedSeconds` from 60 → 300. RAM: scale threshold with installed RAM. VRAM: raise duration from 60 → 180 seconds. GPU: redesign to silent-only incidents. Disk: add latency guard for Set A on SSDs. Process anomaly: require both conditions (AND not OR). |
| `09_UPGRADE_RECOMMENDATION_MODEL.md` | GPU compute score contribution should be deferred or marked explicitly as unreliable for gaming workloads. Consider removing GPU compute upgrade rec from V1. |
| `10_NOTIFICATION_POLICY.md` | CPU Level 2 notification should not fire for incidents under 5 minutes. GPU Level 2 notification should be removed (GPU incidents are silent). |
| `11_BACKGROUND_RUNTIME.md` | Remove `FileSystemWatcher` — settings apply on restart or via direct in-memory update. Fix `AddWpfBlazorServices()` to correct WPF host initialization code. Describe 3-state sampling machine (Normal/Incident/Recovery), not 4-state (remove Watch mode). |
| `14_DATA_MODEL.md` | Remove `metrics_samples_1hour` table and associated index. |
| `16_PERFORMANCE_BUDGET.md` | Rename "Benchmark Targets" to "Developer Performance Regression Tests." |
| `17_WINDOWS_TECHNICAL_PLAN.md` | Fix VRAM total: replace `Win32_VideoController.AdapterRAM` with DXGI `DXGI_ADAPTER_DESC.DedicatedVideoMemory`. Add PDH query lifecycle note (open once, hold open). |
| `19_WBS.md` | Correct task count to 125 (not ~80). Remove deferred tasks. Revise timeline estimate. |
| `20_MVP_SCOPE.md` | Remove "Top by Disk" process list from V1 included features (unless P/Invoke approach is confirmed). Remove "GPU temperature" from included features (defer NVAPI to V2). |

---

## Section J — Proposed Final V1 Core Loop

```
WINDOWS STARTS
     │
     ▼
MainPCDoctor.exe starts (HKCU Run key)
Single process: WPF + MonitoringWorker + Tray
     │
     ▼
PDH queries initialized (once, held open)
SQLite opened (WAL mode)
Open incidents loaded (orphans closed)
     │
     ▼
Tray icon appears (< 2 seconds)
     │
     ▼
MonitoringWorker loop begins (10s interval, Balanced)

  Every 10 seconds:
  ├── Collect CPU, RAM, Disk, GPU via PDH (fast)
  ├── Collect top processes by CPU + RAM (GetProcesses, rate-limited to 30s)
  ├── Assemble SystemSnapshot
  ├── Add to RollingBuffer (30 min window)
  ├── Run DiagnosisEngine against recent window
  │     ├── RamPressureRule   (120s gate)
  │     ├── CpuBottleneckRule  (300s gate — NOT 60s)
  │     ├── DiskBottleneckRule (60s gate, latency-guarded on SSD)
  │     ├── VramPressureRule  (180s gate — NOT 60s)
  │     ├── GpuComputeRule    (silent-only, 300s gate)
  │     ├── ThermalRule       (sensor-dependent)
  │     └── ProcessAnomalyRule (AND condition)
  ├── IncidentTracker.Update(result)
  │     ├── Candidate → Confirmed at gate → Notify (L2, CPU/RAM/Disk/Thermal only)
  │     └── Resolved when cleared
  └── Every 60s: flush 1-minute aggregate to SQLite

  Every session:
  ├── UpgradeRecommendationEngine re-evaluates from incident history
  └── If score ≥ 35, observation ≥ 14 days → Level 4 notification (once per 30 days)

USER ACTION (optional):
  Tray click → Dashboard → live tiles
  "Why Was My PC Slow?" → retrospective analysis against buffer + DB
  Incidents → browse history
  System Capacity → component assessment
  Settings → configure
```

**Notable changes from original plan:**
- CPU gate: 300s not 60s
- GPU: silent incidents only, no L2 toast
- VRAM: 180s gate
- Watch mode: removed (3 states not 4)
- Level 1 notifications: removed
- Process disk I/O: removed (Top by CPU + RAM only)
- 1-hour aggregate table: removed
- FileSystemWatcher: removed

---

## Section K — Implementation Gate Acceptance Criteria

These criteria must be met before implementation begins.

### Gate 1 — Technical Clarifications (resolve before writing code)

- [ ] **BLK-01 resolved:** VRAM total measurement approach confirmed (DXGI or LibreHardwareMonitor). Document updated.
- [ ] **BLK-02 resolved:** Process disk I/O decision made (P/Invoke approach documented, OR Top-by-Disk removed from V1 scope). Document updated.
- [ ] **BLK-03 resolved:** CPU confirmation threshold changed to 300 seconds in `07_DIAGNOSIS_RULES.md`.
- [ ] **BLK-04 resolved:** GPU compute rule redesigned to silent-only or removed. `07_DIAGNOSIS_RULES.md` and `10_NOTIFICATION_POLICY.md` updated.
- [ ] **NB-05 resolved:** `AddWpfBlazorServices()` corrected to valid WPF host initialization code.

### Gate 2 — Architecture Decisions Confirmed

- [ ] Decision recorded: `MainPCDoctor.Background` merged into `MainPCDoctor.Desktop` OR kept separate with written justification.
- [ ] Decision recorded: FileSystemWatcher removed in favor of restart-to-apply or direct in-memory update.
- [ ] Decision recorded: Watch mode removed (3-state sampling machine confirmed).
- [ ] Decision recorded: 1-hour aggregate table removed.

### Gate 3 — WBS Corrected

- [ ] Actual task count documented (125 → reduced target after removals).
- [ ] Revised timeline estimate produced (realistic for solo founder).
- [ ] Phase gate criteria reviewed and confirmed achievable.

### Gate 4 — Diagnosis Rules Reviewed

- [ ] RAM threshold scaling formula accepted.
- [ ] All rule duration thresholds reviewed and confirmed.
- [ ] GPU rule decision finalized (silent / deferred / redesigned).
- [ ] ProcessAnomalyRule condition updated (AND logic).
- [ ] Disk Set A SSD guard added.

---

## Final Verdict

The Planning Pack is **CHANGES_REQUIRED**.

The architecture is sound. The product principles are correct. The data model is well-designed. The core diagnosis model is the right approach.

The blocking issues are specific and fixable — they are not fundamental design errors, they are implementation details that were underspecified or technically incorrect. Once the 5 blocking issues are resolved and the diagnosis rules are tightened, this plan is ready for implementation.

The recommended order of changes:
1. Fix BLK-01 through BLK-05 in the relevant documents
2. Apply diagnosis rule threshold changes (Section D)
3. Apply architecture simplifications (Section G)
4. Correct WBS task count and timeline
5. Return for final approval

**Do not begin implementation until Founder reviews these findings and approves the revised documents.**
