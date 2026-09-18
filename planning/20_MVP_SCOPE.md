# 20 — MVP Scope
*Revised: 2026-09-17 — Gate Review 01*

---

## MVP Statement

**V1 proves the core monitoring loop:**

> Monitor → Detect → Diagnose → Notify → Explain

V1 is considered complete when a user can:
1. Install and forget about the app
2. Be notified when a RAM pressure incident is confirmed
3. Ask "why was my PC slow?" and get an evidence-based answer
4. See whether CPU or RAM upgrade is warranted based on multi-week history

---

## Included in V1

| Feature | Description |
|---|---|
| Background monitoring service | Runs silently from Windows startup |
| CPU monitoring | Total utilization, per-core, clock speed |
| RAM monitoring | Used, available, commit charge, pagefile pressure |
| Disk monitoring | Utilization, latency, queue depth per drive |
| GPU monitoring | Utilization, VRAM used/total (DXGI) |
| CPU temperature | If sensor available via LibreHardwareMonitor (graceful null) |
| GPU temperature | If sensor available via LibreHardwareMonitor (graceful null) |
| Process monitoring | Top 5 processes by CPU, top 5 by RAM |
| Incident detection | Candidate → confirmed lifecycle with duration gating |
| Incident history | 90-day rolling storage |
| "Why Was My PC Slow?" | Retrospective analysis for 5/15/30-min windows |
| Bottleneck classification | CPU / RAM / Disk / GPU / VRAM / Thermal / Process |
| CPU upgrade recommendation | Evidence-based; ≥ 14 days + score ≥ 35 for High confidence |
| RAM upgrade recommendation | Evidence-based; ≥ 14 days + score ≥ 35 for High confidence |
| System tray icon | 3 states: healthy (green) / RAM warning (amber) / paused (grey) |
| Tray context menu | Open dashboard, latest incident, pause, settings, exit |
| Windows toast notifications | Level 2 (RAM pressure warning), Level 4 (CPU or RAM upgrade rec) |
| Dashboard screen | Live resource tiles, status headline, recent incidents list |
| Incident list screen | Timeline, filter by type and date |
| Incident detail screen | Full metrics, top processes (CPU + RAM), evidence, confidence |
| System Capacity screen | CPU + RAM with upgrade rec status; GPU + Disk with monitoring status only |
| Settings screen | Startup, notifications, monitoring intensity, history retention |
| Adaptive sampling | 3-state: Eco (30s) / Normal (10s) / Incident (3s) |
| Resource governor | App skips cycles if own CPU > 3% |
| SQLite storage | 1-minute metrics aggregates, incidents, recommendations |
| Data retention management | Auto-purge old data by configured window |
| Startup registration | HKCU Run key, toggleable in Settings |
| Crash recovery | Orphan incidents closed on next startup |
| Glitch / cyber diagnostic visual style | Dark theme, scanline texture, glitch effects |

---

## Explicitly NOT in V1

| Feature | Note |
|---|---|
| GPU upgrade recommendation | Deferred to V2 — requires external market data to be actionable |
| VRAM upgrade recommendation | Deferred to V2 |
| Disk upgrade recommendation | Deferred to V2 — storage upgrade type (speed vs. space) adds diagnostic complexity |
| Thermal upgrade recommendation | Not planned — thermal issues have non-hardware solutions (cleaning, repaste) |
| Top processes by Disk I/O | `System.Diagnostics.Process` has no disk I/O properties; feature does not exist in V1 |
| GPU / VRAM / Disk / Thermal notifications | All silent in V1 — these incidents record to history only |
| Level 1 (informational trend) notifications | Deferred to V2 |
| Level 3 (pattern notice) notifications | Deferred to V2 |
| 1-hour metrics aggregates | No V1 consumer; table removed |
| Watch Mode (4th sampling state) | Removed; 3-state machine (Eco / Normal / Incident) is sufficient |
| FileSystemWatcher on settings.json | Settings apply on next restart; watcher is unnecessary complexity |
| Linux support | Not in V1; Core architecture supports future addition |
| macOS support | Not planned |
| Cloud sync or accounts | Local-only by design |
| LLM / AI diagnosis | Deferred; deterministic rules only in V1 |
| Remote PC monitoring | Not in scope |
| Hardware shopping links | Not in scope |
| Automatic actions (process kill, overclocking) | Explicitly excluded |
| Historical charts/graphs | Text-first in V1; charts are V2 |
| Light mode | V1 dark-only |
| Localization / non-English | V1 English-only |
| MSIX / Windows Store packaging | V1 uses Inno Setup |
| In-app update mechanism | V1 requires manual reinstall for updates |
| Multiple GPU support (detailed) | V1: first GPU only |

---

## MVP Acceptance Criteria

### AC-01: Background Monitoring
- [ ] App starts automatically with Windows after install (HKCU Run key present)
- [ ] Tray icon appears within 3 seconds of Windows login
- [ ] App collects CPU, RAM, Disk, and GPU metrics continuously
- [ ] Idle CPU usage < 1% averaged over 5 minutes
- [ ] Idle RAM usage < 120 MB after 10 minutes running

### AC-02: CPU Monitoring
- [ ] CPU total utilization within ±5% of Task Manager value
- [ ] Per-core utilization visible in dashboard
- [ ] CPU temperature shown if sensor available; "Unavailable" if not

### AC-03: RAM Monitoring
- [ ] RAM used and available within ±200 MB of Task Manager
- [ ] Commit charge and pagefile status captured

### AC-04: Disk Monitoring
- [ ] Disk utilization within ±10% of Performance Monitor
- [ ] Disk latency captured via PDH; shown as "Unavailable" if PDH counter absent

### AC-05: GPU Monitoring
- [ ] GPU utilization displayed if GPU detected via PDH
- [ ] VRAM used/total displayed; total from DXGI (not WMI AdapterRAM)
- [ ] "Unavailable" state shown cleanly if no GPU detected

### AC-06: Process Monitoring
- [ ] Top 5 processes by CPU shown
- [ ] Top 5 processes by RAM shown
- [ ] Process names only (no paths, no arguments)
- [ ] No "Top by Disk" list (not implemented in V1)

### AC-07: Incident Detection — CPU
- [ ] CPU at 90% for 290 seconds: **no incident** (below 300-second gate)
- [ ] CPU at 90% for 310 seconds with disk and RAM headroom: incident created
- [ ] CPU incident is silent: no Level 2 toast, tray icon does not change

### AC-08: Incident Detection — RAM
- [ ] 2-of-3 RAM signals sustained for 120 seconds: incident created
- [ ] Brief RAM spike (< 60 seconds or single-signal): no incident created
- [ ] RAM incident stored with correct timestamp range, peaks, top processes
- [ ] RAM incident triggers Level 2 toast notification
- [ ] RAM incident changes tray icon to amber

### AC-09: Notifications
- [ ] Level 2 RAM toast fires once per confirmed incident (not repeated)
- [ ] Toast click opens Incident Detail view
- [ ] Level 4 upgrade rec toast fires at Medium/High confidence threshold
- [ ] Notification respects user notification level setting
- [ ] GPU saturation: no notification (always silent)

### AC-10: Upgrade Recommendations — CPU and RAM Only
- [ ] No recommendation after 1 incident, regardless of severity
- [ ] No recommendation before 7-day minimum observation window
- [ ] High confidence recommendation requires ≥ 14 days + score ≥ 35
- [ ] Recommendation shows evidence bullets
- [ ] Recommendation does not repeat within 30 days per component
- [ ] No GPU upgrade recommendation ever generated
- [ ] No Disk upgrade recommendation ever generated

### AC-11: Why Was My PC Slow?
- [ ] Opens from dashboard button and sidebar
- [ ] Returns result within 1 second for 15-minute window
- [ ] Correctly identifies bottleneck type in a window with a recorded incident
- [ ] Returns "no issues detected" when nothing occurred
- [ ] Shows contributing processes for the identified bottleneck

### AC-12: Dashboard
- [ ] All four resource tiles display correct values
- [ ] Tiles update within 15 seconds of new data
- [ ] System status headline reflects current state
- [ ] Recent incidents list shows last 3 incidents

### AC-13: Incidents Screen
- [ ] All confirmed incidents appear in the list
- [ ] Filter works: Today / This Week / All
- [ ] Clicking an incident opens the detail view
- [ ] Incident detail shows all required fields; process table has CPU + RAM columns only

### AC-14: System Capacity Screen
- [ ] CPU section: shows spec + recommendation badge if threshold crossed, or "monitoring" status
- [ ] RAM section: same as CPU
- [ ] GPU section: shows utilization stats and monitoring status; **no recommendation badge**
- [ ] Disk section: shows latency and utilization status; **no recommendation badge**
- [ ] "Insufficient data" state shown cleanly if < 7 days of history

### AC-15: Settings
- [ ] All settings persist after app restart
- [ ] Start with Windows toggle adds/removes HKCU Run key correctly
- [ ] Notification level change takes effect immediately (no restart)
- [ ] Monitoring intensity change takes effect on next app restart
- [ ] Clear History removes database contents after confirmation

### AC-16: Window Lifecycle and Shutdown

**Dashboard window close does not terminate monitoring:**
- [ ] Start MainPC Doctor
- [ ] Monitoring begins (confirmed: tray icon present, logs show sample cycles)
- [ ] Open Dashboard
- [ ] Close Dashboard (X button)
- [ ] Process remains alive (visible in Task Manager)
- [ ] Monitoring continues (confirmed: tray icon still present, new log entries appear)
- [ ] Tray icon remains available and responsive
- [ ] Reopening Dashboard (tray double-click) succeeds

**Exit from tray terminates cleanly:**
- [ ] Select Exit from tray context menu
- [ ] Monitoring stops (MonitoringWorker receives cancellation)
- [ ] `host.StopAsync()` completes within 10 seconds
- [ ] SQLite flush completes before process exits (database is intact)
- [ ] Process terminates (no longer visible in Task Manager)

**Windows logoff/shutdown:**
- [ ] Initiate Windows logoff
- [ ] App receives `SessionEnding` event and calls `Application.Current.Shutdown()`
- [ ] Graceful shutdown completes within 10 seconds
- [ ] Database is intact after forced shutdown test (WAL mode protects it)

### AC-17: Stability
- [ ] App runs for 24 hours without crash
- [ ] App recovers cleanly after process kill (resumes on next login)
- [ ] No memory leak over 24 hours (RAM stays < 150 MB)
- [ ] SQLite database intact after abrupt exit
- [ ] Orphan incidents closed correctly on recovery restart

---

## Definition of Done (V1 Release)

- All AC criteria marked passing
- All unit tests green (`dotnet test`)
- All integration tests green on Windows
- Manual test checklist complete (from 18_TEST_STRATEGY.md)
- Developer performance regression tests pass (from 16_PERFORMANCE_BUDGET.md)
- 24-hour stability test passed
- Installer tested on clean Windows 10 21H2 and Windows 11 23H2
- Uninstaller removes all files and registry entries
- No known critical bugs outstanding
