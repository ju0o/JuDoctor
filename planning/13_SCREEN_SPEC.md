# 13 — Screen Spec
*Revised: 2026-09-17 — Gate Review 01*

---

## Screen Inventory

| Screen | Route | Access |
|---|---|---|
| Dashboard | `/` | Default when window opens |
| Incident List | `/incidents` | Sidebar + tray menu |
| Incident Detail | `/incidents/:id` | From list or notification |
| Why Was My PC Slow? | `/why-slow` | Dashboard button + sidebar |
| System Capacity | `/capacity` | Sidebar |
| Settings | `/settings` | Tray menu + sidebar |

**Why Slow?** and **Diagnosis Result** are the same screen — the result appears inline after the user selects a time window.

---

## Shell Layout

```
┌─────────────────────────────────────────────────────────────┐
│  [≡] MainPC Doctor           [last scan: 14 seconds ago]   │
│  ─────────────────────────────────────────────────────────  │
│  │              │                                           │
│  │  [■] Dash    │                                           │
│  │  [≡] Incidents│         MAIN CONTENT AREA               │
│  │  [?] Why Slow│                                           │
│  │  [▦] Capacity│                                           │
│  │  [⚙] Settings│                                           │
│  │              │                                           │
│  ─────────────────────────────────────────────────────────  │
│  [●] Monitoring active             [Pause]     [v1.0.0]    │
└─────────────────────────────────────────────────────────────┘
Window: 900 × 620 px minimum. Resizable. Not full-screen in V1.
```

---

## Screen 1 — Dashboard

```
┌─────────────────────────────────────────────────────────────┐
│  SYSTEM STATUS                                              │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  ● All systems nominal                              │   │
│  │  No active incidents — last analyzed 14s ago        │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  RESOURCE TILES                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐      │
│  │  CPU     │ │  RAM     │ │  DISK    │ │  GPU     │      │
│  │  ██░░░░  │ │  ████░░  │ │  █░░░░░  │ │  ██░░░░  │      │
│  │  34%     │ │  68%     │ │  12%     │ │  41%     │      │
│  │  2.4 GHz │ │ 10.9/16GB│ │  4ms lat │ │  8/8GB   │      │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘      │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [?] WHY WAS MY PC SLOW?          [Run Analysis →]  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  RECENT INCIDENTS                                           │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [AMBER] RAM Pressure — 14 min — Today 2:34 PM  [›] │   │
│  │  [GREY]  CPU Bottleneck — 8 min — Yesterday     [›] │   │
│  │  [GREY]  RAM Pressure — 22 min — 3 days ago     [›] │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

## Screen 2 — Incident List

```
┌─────────────────────────────────────────────────────────────┐
│  INCIDENT HISTORY                                           │
│                                                             │
│  Filter: [Today ▼]  [All Types ▼]                          │
│  ─────────────────────────────────────────────────────────  │
│  │ TYPE            │ WHEN         │ DURATION │ STATUS │    │
│  ├─────────────────┼──────────────┼──────────┼────────┤    │
│  │ RAM Pressure    │ Today 2:34PM │ 14m      │ Resolved│ › │
│  │ CPU Bottleneck  │ Yest. 11:02AM│ 8m       │ Resolved│ › │
│  │ RAM Pressure    │ Aug 12 9:15AM│ 22m      │ Resolved│ › │
│  └─────────────────┴──────────────┴──────────┴────────┘    │
│                                                             │
│  Showing 3 of 12 incidents                                  │
└─────────────────────────────────────────────────────────────┘
```

---

## Screen 3 — Incident Detail

```
┌─────────────────────────────────────────────────────────────┐
│  [← Back]  RAM PRESSURE                    [Today 2:34 PM] │
│  ─────────────────────────────────────────────────────────  │
│  Duration: 14 minutes                                       │
│  Status: RESOLVED                                           │
│                                                             │
│  PEAK VALUES                                                │
│  RAM Used: 14.8 / 16.0 GB  (92.5%)                        │
│  Available at peak: 1.2 GB  (threshold: 1.6 GB)            │
│  Commit charge: 88% of limit                               │
│  Pagefile pressure: Active                                  │
│                                                             │
│  TOP PROCESSES (at confirmation)                            │
│  ┌───────────────────────────────┐                         │
│  │ chrome.exe        CPU 12%  RAM 4,200 MB                 │
│  │ slack.exe         CPU 4%   RAM 1,800 MB                 │
│  │ devenv.exe        CPU 3%   RAM 1,200 MB                 │
│  └───────────────────────────────┘                         │
│                                                             │
│  EVIDENCE                                                   │
│  ✓ Available RAM below threshold for 120 seconds           │
│  ✓ Commit charge > 80% simultaneously                      │
│  ✓ Pagefile pressure active                                 │
└─────────────────────────────────────────────────────────────┘
```

**Process list shows:** Name, CPU%, RAM (MB). No disk I/O column.

---

## Screen 4 — Why Was My PC Slow?

```
┌─────────────────────────────────────────────────────────────┐
│  WHY WAS MY PC SLOW?                                        │
│  ─────────────────────────────────────────────────────────  │
│  Analyze a time window:                                     │
│  [Last 5 min ▼]                        [Run Analysis →]    │
│                                                             │
│  ─ RESULT ────────────────────────────────────────────────  │
│  Analyzed: Today 2:30 PM – 2:44 PM (14 min window)         │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  PRIMARY: RAM PRESSURE          ████████░░  HIGH    │   │
│  │  Available RAM dropped below threshold for 14 min.  │   │
│  │  Commit charge reached 88%.                         │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  SECONDARY SIGNALS                                          │
│  CPU reached 61% average (not a bottleneck — below gate)   │
│  Disk latency: 3ms average (normal)                        │
│                                                             │
│  CONTRIBUTING PROCESSES                                     │
│  chrome.exe consumed 4.2 GB RAM during this window         │
│  slack.exe consumed 1.8 GB RAM                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Screen 5 — System Capacity

```
┌─────────────────────────────────────────────────────────────┐
│  SYSTEM CAPACITY                                            │
│  ─────────────────────────────────────────────────────────  │
│  Based on 18 days of monitoring data                        │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  CPU                                                │   │
│  │  Intel Core i5-10400 · 6 cores · 2.9 GHz base      │   │
│  │  ● RECOMMENDATION AVAILABLE     [Medium confidence] │   │
│  │  Confirmed saturation on 6 of last 18 monitored     │   │
│  │  days. Upgrade may improve responsiveness.          │   │
│  │  [View CPU Analysis →]                              │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  RAM                                                │   │
│  │  16 GB DDR4-3200                                    │   │
│  │  ● MONITORING — sufficient data, no pressure found  │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  GPU                                                │   │
│  │  NVIDIA RTX 3060 Ti · 8 GB VRAM                     │   │
│  │  ○ MONITORING STATUS                                │   │
│  │  7-day avg utilization: 54% · Peak: 99%             │   │
│  │  VRAM avg: 4.2 / 8.0 GB · Peak: 7.8 GB             │   │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  DISK (C: NVMe)                                     │   │
│  │  ○ MONITORING STATUS                                │   │
│  │  7-day avg latency: 1.2ms · Peak: 18ms              │   │
│  │  Utilization avg: 8% · Peak: 72%                    │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

**Design intent:**
- CPU and RAM: show recommendation badge if evidence threshold crossed; otherwise "MONITORING — no issues found."
- GPU and Disk: show monitoring status **only** — utilization stats and health summary. No recommendation badge. No upgrade prompt. No "Upgrade Recommended" copy for these components.
- The System Capacity screen makes it unambiguous to users that V1 provides CPU and RAM upgrade guidance, and monitoring status for everything else.

---

## Screen 6 — Settings

```
┌─────────────────────────────────────────────────────────────┐
│  SETTINGS                                                   │
│  ─────────────────────────────────────────────────────────  │
│                                                             │
│  STARTUP                                                    │
│  [●] Start with Windows                                     │
│                                                             │
│  NOTIFICATIONS                                              │
│  Notification level:                                        │
│  [○] Off  [○] Upgrade Only  [●] All (RAM + Upgrades)        │
│                                                             │
│  MONITORING                                                 │
│  Intensity:                                                 │
│  [○] Low (saves power)  [●] Balanced  [○] Detailed         │
│                                                             │
│  HISTORY                                                    │
│  Retain history for: [30 days ▼]                           │
│                                                             │
│  [Clear History...]          [← applies on next restart]   │
│  ─────────────────────────────────────────────────────────  │
│  [Pause Monitoring]                          [Version info] │
└─────────────────────────────────────────────────────────────┘
```

Note: Settings changes (intensity, retention) apply on the next app restart. The label "(← applies on next restart)" is shown inline for affected settings. Startup toggle and notification level apply immediately.
