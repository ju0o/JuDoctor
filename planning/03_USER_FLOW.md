# 03 — User Flow

---

## Flow A — Installation & First Start

```
User downloads installer
        │
        ▼
Install wizard runs
  - Accept license
  - Choose install path
  - Confirm: "Start with Windows" (default ON)
        │
        ▼
Installation completes
        │
        ▼
Background service starts
        │
        ▼
Tray icon appears (subtle, normal state)
        │
        ▼
Optional: first-run tooltip
  "MainPC Doctor is running in the background.
   Click the tray icon to open the dashboard."
        │
        ▼
User resumes work
Background monitoring is now active
```

---

## Flow B — Normal Daily Operation (Nothing Unusual)

```
Windows boots
        │
        ▼
MainPC Doctor Background Service starts (auto-start)
        │
        ▼
MonitoringWorker begins low-frequency sampling
        │
        ▼
Metrics collected, stored, aggregated
  CPU / RAM / Disk / GPU / Processes
        │
        ▼
Diagnosis engine evaluates each sample
        │
        ▼
No thresholds triggered → SILENT
        │
        ▼
User works all day
No notifications
No interruptions
        │
        ▼
Tray icon: normal state (no color change)
        │
        ▼
End of day — user shuts down or sleeps
Monitoring pauses
History retained on disk
```

---

## Flow C — PC Feels Slow → Why Was My PC Slow?

```
User notices PC feels slow / sluggish
        │
        ▼
User clicks tray icon
        │
        ▼
Tray menu appears:
  [ Open Dashboard ]  ← user clicks this
        │
        ▼
Dashboard opens
  System Status: [state shown]
        │
        ▼
User clicks: "WHY WAS MY PC SLOW?"
        │
        ▼
Time window picker appears:
  ○ Last 5 minutes
  ○ Last 15 minutes   ← default
  ○ Last 30 minutes
        │
        ▼
Diagnosis runs against history in window
        │
        ├─── If evidence found ───▶
        │                           Result screen:
        │                           LIKELY CAUSE: [type]
        │                           [Metric evidence]
        │                           [Timeline of events]
        │                           [Top processes at time]
        │                           [Confidence]
        │
        └─── If no evidence ──────▶
                                    "No bottleneck detected in this window.
                                     The slowdown may have been brief or
                                     unrelated to hardware resources."
```

---

## Flow D — Notification Received → Incident Investigation

```
System detects sustained memory pressure
        │
        ▼
Incident created (confirmed state)
        │
        ▼
Level 2 notification fires (Windows toast):
  "Memory pressure has remained high for 14 minutes.
   Available: 1.4 GB. Pagefile: Elevated."
        │
        ▼
User sees notification (working in other app)
        │
        ├─── User ignores ──────────▶ Notification dismissed
        │                              Incident stored in history
        │                              No further notification this session
        │                              (cooldown active)
        │
        └─── User clicks toast ──────▶
                                        Dashboard opens to Incident Detail:
                                        INCIDENT #xxxx
                                        Type: Memory Pressure
                                        Duration: 14 min (ongoing)
                                        Peak RAM: 31.1 / 32 GB
                                        Min Available: 1.4 GB
                                        Top Processes: [list]
                                        Pagefile: Elevated
                                        Diagnosis: RAM Capacity Pressure
                                        Confidence: High
```

---

## Flow E — Upgrade Recommendation (Long-Term)

```
Over 14+ days:
  9+ memory pressure incidents recorded
  Available RAM repeatedly < 2 GB
  Pagefile activity elevated across multiple sessions
        │
        ▼
Upgrade recommendation engine evaluates evidence
Confidence threshold crossed
        │
        ▼
Level 4 notification fires:
  "RAM upgrade may now provide a noticeable benefit."
        │
        ▼
User opens System Capacity page:
  RAM
  Current: 32 GB
  Status: Capacity Pressure (Recurring)
  Observation: 14 days
  Evidence:
    - 9 high-memory incidents
    - 147 min below 2 GB available
    - Repeated pagefile activity
  Recommendation: Increase RAM capacity
  Confidence: HIGH

  [What this means]
  [Evidence timeline]
        │
        ▼
User decides whether to act
App does not push shopping links
App does not repeat the recommendation for [configurable cooldown]
```

---

## Flow F — Settings Configuration

```
User right-clicks tray icon
  → Settings
        │
        ▼
Settings screen:
  Startup
  Notifications
  Monitoring Intensity
  History Retention
  Pause Monitoring
  Clear History
        │
        ▼
User adjusts preferences
Changes take effect immediately or on next start
```

---

## Flow G — Manual Diagnosis Trigger

```
User right-clicks tray icon
  → Run Diagnosis Now
        │
        ▼
Diagnostic scan runs against last 15 minutes of history
        │
        ▼
Result appears (same format as Why Was My PC Slow?)
```

---

## ASCII Master User Flow

```
┌─────────────────────────────────────────────────────────────┐
│                      MAINPC DOCTOR                          │
│                   User Interaction Map                      │
└─────────────────────────────────────────────────────────────┘

BACKGROUND (always running, invisible)
─────────────────────────────────────────────────────────────
  Sampling  →  History  →  Diagnosis  →  Incident Tracker
                                              │
                               ┌─────────────┴──────────────┐
                               │                            │
                          No incident                  Incident found
                          Stay silent                       │
                                              ┌────────────┐
                                              │  Notify?   │
                                              └────────────┘
                                               Level 0 → silent
                                               Level 1 → optional info
                                               Level 2 → toast warning
                                               Level 3 → repeat pattern
                                               Level 4 → upgrade rec

USER-INITIATED (tray or notification click)
─────────────────────────────────────────────────────────────
  Tray Click
     │
     ├── Open Dashboard
     │       ├── System Status tiles (CPU/RAM/GPU/Disk)
     │       ├── Recent Incidents
     │       ├── Why Was My PC Slow? ──→ Time picker ──→ Analysis
     │       ├── System Capacity (Upgrades)
     │       └── Settings
     │
     ├── Recent Incident → Incident Detail
     ├── Run Diagnosis Now → Analysis Result
     ├── Pause Monitoring
     ├── Settings
     └── Exit
```
