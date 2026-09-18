## Resource Governor final reliability check — 2026-09-18

**Scope:** the V1 ResourceGovernor invariant only. No scheduling redesign, new product feature, or broad event subscriptions. Earlier sections below are historical and do not override this check. This is not a new full USER-STABLE/reboot/UI certification.

| Required result | Evidence / outcome |
|---|---|
| RESOURCE_GOVERNOR_NORMAL | Existing 3/10/30-second scheduler unchanged and regression-tested. Production runtime evidence summarized below. |
| RESOURCE_GOVERNOR_THROTTLED | >400 MiB enters a 60-second minimum-monitoring cadence. Worker still collects snapshots, updates the buffer, and calls the existing minute aggregator. |
| MINIMUM_MONITORING | CPU summary, RAM total/available/commit summary, snapshot heartbeat, self private bytes, and `normal` / `throttled` / `governor_read_failed` state retained. Disk/GPU/VRAM/process/temperature collection omitted in the minimum collector. |
| DB_CONTINUITY | Existing `metrics_samples_1min` extended transactionally by migration 002. Persisted `heartbeat_at`, `self_private_bytes`, `monitoring_state`, `mem_total_gb`. V1 row preservation and repeated migration tested. |
| RECOVERY | Automatic recovery below 350 MiB, on the same worker loop. Deterministic and real-time controlled runs return to normal collection; no restart or user action. Existing single-instance guard also verified with the installed executable. |
| THRESHOLD_STABILITY | Stateful hysteresis: enter strictly >400 MiB, leave strictly <350 MiB, keep state in between. Boundary oscillations at 399/400/401 and recovery edge covered. |
| TEST_RESULTS | 76 passed, 0 failed: Core 53, Integration 20, Storage 3. Final complete run: `release-check.txt` and `release-check_*.trx`. |
| WINDOWS_RUNTIME_EVIDENCE | See production and controlled-run results below; no unsafe memory allocation used. |

### Reliability details and limits

- Governor reads refresh the process metrics every time. Read exceptions return reduced monitoring with unknown (`NULL`) private bytes and an explicit failure state, never skip the whole loop. Recovery after a read exception is covered by tests.
- Transition logging avoids repeated per-cycle warning spam. A fresh governor reading after the wait handles recovery/entry during the wait without restarting the scheduler.
- Full diagnosis is deferred while detailed sensors are omitted: missing disk/process/GPU data must not falsely resolve or confirm incidents. CPU/RAM values and minimum health remain observed and persisted. On recovery, duration evidence uses the consecutive normal-sample portion after the reduced interval, avoiding treating missing detailed evidence as a continuous full observation.
- Normal aggregate writes retain the existing 60-second gate. A normal-to-throttled transition can produce a gap of about 90 seconds because a fresh emergency snapshot can arrive before the last minute flush is due; steady throttled writes then occur every ~60 seconds. This is bounded reduced monitoring, not permanent blindness.
- This guarantee covers governor decisions and sensor-read failures. It is not a guarantee against OS termination, disk failure, or a collector that never returns. No unsafe system memory exhaustion was attempted.

### Controlled Windows run

`dotnet run --project qa/tools/MonitoringSmoke --configuration Release -- qa/evidence/v1-governor-final/controlled-windows.db 350 governor-scenario`

- Real Windows collectors and production worker/aggregator; isolated DB, notifications suppressed by the QA host.
- Only the private-memory input is substituted: 200 MiB initially, 450 MiB from 75–260 seconds, then 200 MiB. Actual host private memory is separately recorded. No control flag was added to the product executable.
- 353 seconds elapsed, 9 snapshots: **6 full + 3 minimum**, **6 persisted aggregates**, clean shutdown, exit 0.
- Persisted states: normal, normal, throttled, throttled, normal, normal. Consecutive throttled rows include CPU/RAM, current heartbeat and 471859200 simulated private bytes; disk/GPU columns are NULL. Later normal rows restore details.
- Deterministic tests additionally simulate 12 collection cycles (12 minutes for sustained throttle), require at least five persisted records, current heartbeat, no duplicate aggregate timestamps, automatic recovery, and safe governor read failure.
- Evidence: `controlled-windows.txt`, `controlled-db.json`, `controlled-windows.db`.

### Production Windows run and local installation

PRODUCTION_RESULT_PENDING

- Fixed self-contained folder build ran on the actual MainPC, using the real user database and normal governor readings. Installed and observed Desktop DLL SHA256 match: `786307C4E373E67FD73EA9866CD79F6665B916A58147AADE97296ABF69949CAB`.
- A real startup blocker was found: custom `Program.Main` never called `App.InitializeComponent()`, so application resources were absent (`BgDeepBrush` XamlParseException). Added that one initialization call. Evidence: `startup-failure.txt`; corrected runtime exception check: `runtime-errors.json`. The earlier report's path-space explanation is not established; the current fix addresses a directly observed missing-initialization cause. Folder publish remains the approved V1 packaging policy; single-file behavior was not retested.
- Original in-place install failed because the old folder's `clrjit.dll` was locked (installer exit 5). The old PID 17804 remains inaccessible with zero reported handles and no window; no claim is made that the user can exit it from a tray.
- The same installer succeeded (exit 0) into `%LOCALAPPDATA%\Programs\MainPCDoctorV1`. HKCU startup and shortcuts now point to that installation. Old locked files remain in `%LOCALAPPDATA%\Programs\MainPCDoctor`; they were not forcibly deleted. No reboot was performed.
- Second installed-app launch exited 0 while the first worker remained alive, confirming no second active worker from that launch. Evidence: `single-instance.json`.
- Installer: `qa/evidence/v1-governor-final/MainPCDoctor-1.0.0-Setup-v4.exe`; SHA256 `BFE9526E4AEAFE7957E9381E7BE096DBF9D5FC99753D18CF0A09BC7171A69791`.
- Main evidence directory: `qa/evidence/v1-governor-final/`. GUI navigation/tray clicks and reboot were not re-certified by this resource-governor check.

---

