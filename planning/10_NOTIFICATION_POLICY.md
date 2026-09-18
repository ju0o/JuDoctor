# 10 — Notification Policy
*Revised: 2026-09-17 — Gate Review 01*

---

## Governing Principle

Notifications must be exceptional. A notification that fires because the user ran a build, started a game, or opened Chrome is a notification that trains the user to ignore MainPC Doctor.

V1 errs strongly on the side of silence. Fewer, more accurate notifications are better than comprehensive but noisy ones.

---

## Notification Levels

### Level 0 — Silent
All transient or gaming/build-context events. No toast, no tray change, no sound. Incident is recorded in history.

**Examples:**
- CPU at 100% for less than 5 minutes
- CPU at 100% during any build-pattern workload (identified by confirmation gate)
- GPU at 99% during any sustained graphics workload (gaming)
- Brief RAM spike (< 60 seconds or single-signal)
- VRAM saturated during level load
- Any thermal throttling event
- Any disk bottleneck event
- Any process anomaly event

### Level 2 — Warning
A confirmed, sustained incident that meaningfully impacts the user and is not explained by normal transient workload.

**V1 triggers (RAM only):**
- Confirmed RAM pressure: 2-of-3 signals sustained for ≥ 120 seconds

**V1 NOT triggered by:**
- CPU bottleneck (silent; feeds upgrade recommendation only)
- GPU compute saturation (always silent)
- Disk bottleneck (silent)
- VRAM pressure (silent)
- Thermal throttling (silent)
- Process anomaly (silent)

**Toast format:**
```
[MainPC Doctor]
⚠ Memory pressure detected
Your system is running low on available RAM. [View Incident →]
```

### Level 4 — Upgrade Recommendation
A historical pattern recommendation. Fires after sufficient evidence accumulates (≥ 14 days observation, score ≥ 35).

**V1 triggers:**
- CPU upgrade recommendation (Medium or High confidence)
- RAM upgrade recommendation (Medium or High confidence)

**V1 NOT triggered by:**
- GPU recommendations (not in V1)
- VRAM recommendations (not in V1)
- Disk recommendations (not in V1)
- Thermal recommendations (not in V1)

**Toast format:**
```
[MainPC Doctor]
📊 Capacity analysis available
Your CPU has shown sustained saturation over N days. [View Analysis →]
```

---

## Removed Levels in V1

| Level | Name | Status |
|---|---|---|
| Level 1 | Informational trend | **Removed from V1** — deferred to V2 |
| Level 3 | Pattern notice | **Removed from V1** — CPU patterns feed Level 4 directly |

Level 1 (informational tips, rising trend notices) and Level 3 (repeated-pattern notices) add complexity without meaningful user value in V1. The RAM warning (Level 2) and upgrade recommendation (Level 4) cover the two meaningful user moments: "something is wrong now" and "you should consider a hardware change."

---

## Severity Matrix (V1)

| Incident Type | Notification | Tray Change | Toast |
|---|---|---|---|
| RAM pressure (sustained, multi-signal) | Level 2 | Yes (warning) | Yes |
| CPU bottleneck (≥ 300s) | Silent | No | No |
| Disk bottleneck (latency-confirmed) | Silent | No | No |
| VRAM pressure (≥ 180s) | Silent | No | No |
| GPU compute saturation | Silent | No | No |
| Thermal throttling | Silent | No | No |
| Process anomaly | Silent | No | No |
| CPU upgrade recommendation | Level 4 | No | Yes |
| RAM upgrade recommendation | Level 4 | No | Yes |

---

## Tray Icon States

The tray icon reflects active confirmed incidents, not transient spikes.

| State | Icon | Meaning |
|---|---|---|
| Healthy | Green dot | No active confirmed incidents |
| Warning | Amber dot | Active RAM pressure incident |
| Paused | Grey dot | Monitoring manually paused |

Tray changes to Warning only for Level 2 events (RAM pressure). CPU, GPU, Disk, VRAM, Thermal incidents do not change the tray icon.

---

## Cooldowns

| Level | Per-Incident Cooldown | Per-Component Daily Max |
|---|---|---|
| Level 2 | Toast fires once per incident (not repeated) | 3 toasts/day maximum |
| Level 4 | 30 days per component | 1 per component per 30 days |

A Level 2 toast fires when the incident is confirmed (not when it starts as a candidate). It does not re-fire if the incident becomes more severe. It does not fire when a new incident of the same type occurs within the 1-hour dedup window.

---

## Notification Requirements

- Windows toast via `ToastNotificationManager` (WinRT)
- AUMID registered at install time (required for non-MSIX apps)
- Clicking toast opens the relevant screen:
  - Level 2 → Incident Detail view
  - Level 4 → System Capacity view
- Respects Windows Focus Assist / Do Not Disturb (OS-managed)
- No custom notification sounds in V1

---

## User Control

Users can set notification level in Settings:

| Setting | Behavior |
|---|---|
| All (default) | Level 2 + Level 4 |
| Upgrade Only | Level 4 only |
| Off | No toasts; incidents still recorded silently |

Changes take effect immediately (no restart required).
