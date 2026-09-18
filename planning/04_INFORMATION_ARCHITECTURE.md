# 04 — Information Architecture

---

## Navigation Model

MainPC Doctor has two entry points:
1. **System tray** — always-available, minimal menu
2. **Main window** — opened on demand

The main window uses a **left-sidebar + content area** layout.
There is no persistent top nav. Navigation is sidebar-driven.

---

## Tray Menu Structure

```
┌────────────────────────────┐
│  ● MainPC Doctor           │
│  ─────────────────────     │
│  System: Healthy           │
│                            │
│  > Open Dashboard          │
│  > Recent Incident         │
│  > Run Diagnosis Now       │
│  ─────────────────────     │
│  > Pause Monitoring        │
│  > Settings                │
│  ─────────────────────     │
│  > Exit                    │
└────────────────────────────┘
```

"System" status reflects the current aggregate health:
- **Healthy** — no active incidents
- **Monitoring** — collecting evidence, not yet at threshold
- **Warning** — active Level 2 incident
- **Critical** — severe Level 2+ incident (rare)
- **Paused** — monitoring is paused by user

---

## Main Window Structure

```
┌──────────────────────────────────────────────────────────┐
│  [LOGO] MainPC Doctor          [minimize] [close]        │
├──────────┬───────────────────────────────────────────────┤
│          │                                               │
│ SIDEBAR  │  CONTENT AREA                                 │
│          │                                               │
│ ─ ─ ─ ─  │                                               │
│          │                                               │
│ Dashboard│                                               │
│          │                                               │
│ Incidents│                                               │
│          │                                               │
│ Why Slow?│                                               │
│          │                                               │
│ Capacity │                                               │
│          │                                               │
│ ─ ─ ─ ─  │                                               │
│          │                                               │
│ Settings │                                               │
│          │                                               │
└──────────┴───────────────────────────────────────────────┘
```

---

## Screen Inventory

| ID | Screen | Entry Points |
|---|---|---|
| SCR-01 | Dashboard | Tray → Open Dashboard; Tray click |
| SCR-02 | Incident List | Sidebar → Incidents |
| SCR-03 | Incident Detail | Incident List → select; Notification click |
| SCR-04 | Why Was My PC Slow? | Sidebar → Why Slow?; Dashboard button |
| SCR-05 | System Capacity (Upgrades) | Sidebar → Capacity; Level 4 notification |
| SCR-06 | Settings | Sidebar → Settings; Tray → Settings |
| SCR-07 | Diagnosis Result | Why Slow? after analysis |

---

## Screen Hierarchy

```
Main Window
├── SCR-01  Dashboard
│     ├── CPU Tile
│     ├── RAM Tile
│     ├── GPU Tile
│     ├── Disk Tile
│     ├── Diagnosis Summary
│     └── Recent Incidents (list, 3 items)
│
├── SCR-02  Incident List
│     ├── Filter: [Today | This Week | All]
│     ├── Timeline list
│     └── → SCR-03  Incident Detail (per item)
│
├── SCR-04  Why Was My PC Slow?
│     ├── Time window selector
│     ├── [Analyze] trigger
│     └── → SCR-07  Diagnosis Result
│
├── SCR-05  System Capacity
│     ├── RAM Assessment
│     ├── CPU Assessment
│     ├── GPU Assessment
│     └── Disk Assessment
│
└── SCR-06  Settings
      ├── Startup section
      ├── Notifications section
      ├── Monitoring Intensity section
      ├── History section
      └── Danger Zone (Clear History, Pause)
```

---

## Content Hierarchy by Screen

### SCR-01 — Dashboard

**Priority order (top to bottom):**
1. System status headline ("HEALTHY" / "WARNING" / "CRITICAL")
2. Resource tiles — 2×2 or horizontal row: CPU, RAM, GPU, Disk
3. Diagnosis summary — one-sentence status or active incident banner
4. Recent incidents (last 3, linked)
5. Last analyzed timestamp

**Not on dashboard:** raw graphs (secondary), detailed metrics, history charts

---

### SCR-02 — Incident List

**Elements:**
- Date group headers (TODAY, YESTERDAY, [date])
- Per incident row:
  - Time
  - Type (Memory Pressure, CPU Saturation, etc.)
  - Duration
  - Status (Resolved / Active / Transient)
- Filter strip at top
- Empty state for no incidents

---

### SCR-03 — Incident Detail

**Elements:**
- Incident ID + timestamp range
- Type badge
- Key metrics at peak (contextual to type):
  - RAM incident: peak used, min available, commit, pagefile state
  - CPU incident: peak %, duration above threshold, core breakdown
  - GPU incident: peak utilization, VRAM peak
  - Disk incident: peak utilization, peak latency
- Top 5 processes at peak (name, metric value)
- Diagnosis statement
- Confidence level
- Related incidents (if part of pattern)

---

### SCR-04 — Why Was My PC Slow?

**Elements:**
- Instruction text
- Time window radio buttons (5 min / 15 min / 30 min)
- Analyze button
- → transitions to SCR-07

---

### SCR-07 — Diagnosis Result

**Elements:**
- Likely Cause (prominent, labeled type)
- Evidence block (metrics, timestamps)
- Top processes at the time
- Alternative explanations (if confidence is MEDIUM)
- "No bottleneck detected" state

---

### SCR-05 — System Capacity

**Elements (per component):**
- Component name
- Current installed spec
- 30-day pressure status
- Recommendation status (NOT YET / CONSIDER / RECOMMENDED)
- Observation window
- Key evidence (bullet list)

**No shopping links in V1.**

---

### SCR-06 — Settings

| Setting | Type | Default |
|---|---|---|
| Start with Windows | Toggle | On |
| Notifications | Radio: Critical / Important / All / Off | Important |
| Monitoring Intensity | Radio: Eco / Balanced / Detailed | Balanced |
| History Retention | Select: 7d / 14d / 30d / 90d | 30d |
| Pause Monitoring | Button | — |
| Clear Incident History | Button (confirm) | — |
| Clear All History | Button (confirm) | — |
