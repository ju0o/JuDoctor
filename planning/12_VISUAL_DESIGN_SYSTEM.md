# 12 — Visual Design System

---

## Design Direction

**Glitch / Cyber Diagnostic — Polished, Not Garish**

The UI feels like an advanced system diagnostic terminal that has been refined into a modern Windows application. It is dark, precise, and readable.

Glitch styling is used sparingly for:
- Logo animation
- Section transitions
- Incident markers
- Warning moments
- Diagnostic scanning animations

Glitch styling is NOT used for:
- Body text
- Metric readouts
- Navigation
- Normal dashboard state

---

## Color Palette

### Background Layers

| Token | Hex | Usage |
|---|---|---|
| `bg-base` | `#0A0C0F` | Main window background |
| `bg-surface` | `#111418` | Card / tile background |
| `bg-elevated` | `#181C22` | Hover states, selected rows |
| `bg-overlay` | `#1E232B` | Modals, dropdowns |
| `border-subtle` | `#252B35` | Card borders, dividers |
| `border-default` | `#303844` | Active borders |

### Text

| Token | Hex | Usage |
|---|---|---|
| `text-primary` | `#E8EDF5` | Primary labels, headlines |
| `text-secondary` | `#8A95A8` | Secondary labels, metadata |
| `text-muted` | `#505A6B` | Placeholder, disabled |
| `text-mono` | `#C0C8D8` | Metric numbers (monospaced) |

### Semantic Colors

| Token | Hex | Usage |
|---|---|---|
| `status-healthy` | `#2ECC88` | Normal/healthy state indicators |
| `status-healthy-dim` | `#1A4D36` | Healthy background fill |
| `status-warning` | `#F0A500` | Warning level (L2) |
| `status-warning-dim` | `#3D2A00` | Warning background fill |
| `status-critical` | `#E84040` | Critical / high severity |
| `status-critical-dim` | `#3D1010` | Critical background fill |
| `status-upgrade` | `#7B6FE8` | Upgrade recommendation |
| `status-upgrade-dim` | `#1E1B40` | Upgrade background fill |
| `status-neutral` | `#4A7FCC` | CPU color (blue tint) |
| `status-neutral-dim` | `#0F1E35` | CPU background |

### Glitch Accent

| Token | Hex | Usage |
|---|---|---|
| `glitch-cyan` | `#00F5E0` | Primary glitch color |
| `glitch-magenta` | `#FF2090` | Secondary glitch offset |
| `glitch-yellow` | `#FFE000` | Tertiary accent |

Glitch colors appear only in animations, logo, incident markers, and diagnostic scan states. They do not appear in static body UI.

---

## Typography

### Fonts

| Role | Font | Fallback |
|---|---|---|
| UI Sans-serif | Segoe UI Variable | Segoe UI, system-ui |
| Monospaced metrics | Cascadia Code | Consolas, monospace |
| Logo / Glitch title | Orbitron (optional, load only for logo) | monospace |

### Scale

| Token | Size | Weight | Line Height | Usage |
|---|---|---|---|---|
| `type-display` | 28px | 600 | 1.2 | Dashboard status headline |
| `type-headline` | 20px | 600 | 1.3 | Section headers |
| `type-title` | 16px | 600 | 1.4 | Card titles, nav items |
| `type-body` | 14px | 400 | 1.6 | Explanatory text |
| `type-body-sm` | 13px | 400 | 1.5 | Secondary body |
| `type-label` | 12px | 500 | 1.4 | Labels, badges |
| `type-caption` | 11px | 400 | 1.4 | Timestamps, metadata |
| `type-metric-lg` | 32px | 400 | 1.1 | Large metric numbers (mono) |
| `type-metric-md` | 22px | 400 | 1.1 | Medium metric numbers (mono) |
| `type-metric-sm` | 16px | 400 | 1.2 | Small metric numbers (mono) |

---

## Layout & Spacing

### Grid

- Base unit: 8px
- Common values: 4, 8, 12, 16, 24, 32, 48px
- Sidebar width: 200px fixed
- Content area: flexible
- Card padding: 16px
- Dashboard tile: minimum 160px wide, 120px tall

### Window

- Minimum size: 920 × 600px
- Default launch size: 1080 × 700px
- Resizable: Yes
- Remembered between sessions: Yes

---

## Component Library

### Resource Tile (Dashboard)

```
┌────────────────────────────┐
│  CPU                       │
│                            │
│  18  %                     │  ← type-metric-lg, text-mono
│                            │
│  ████████░░░░░░░░░░  18%  │  ← thin progress bar
│                            │
│  Normal                    │  ← status label, status-healthy
└────────────────────────────┘
```

States:
- Normal: `status-healthy` label
- Elevated: `status-warning` label + bar tint
- High: `status-warning` label + bar orange
- Critical: `status-critical` label + bar red + border glow

### Status Badge

```
[ HEALTHY ]    bg: status-healthy-dim    text: status-healthy
[ WARNING ]    bg: status-warning-dim    text: status-warning
[ CRITICAL ]   bg: status-critical-dim   text: status-critical
[ CONSIDER ]   bg: status-upgrade-dim    text: status-upgrade
```

Monospace caps, 11px, tight padding (4px × 8px).

### Progress Bar

- Track: `border-subtle` or `bg-elevated`
- Fill: color-coded by value (green → yellow → orange → red)
- Height: 4px (thin), 8px (medium for focus states)
- No border radius by default — flat terminals aesthetic

### Incident Row

```
┌──────────────────────────────────────────────────────┐
│  14:37  ·  MEMORY PRESSURE  ·  12 min  ·  Resolved  │
└──────────────────────────────────────────────────────┘
```

- Time: `text-secondary`, `type-caption`
- Type: `text-primary`, `type-label`, uppercase
- Duration: `text-secondary`, `type-caption`
- Status badge aligned right

### Sidebar Nav Item

- Normal: `text-secondary`, `type-body`
- Active: `text-primary`, `type-body`, left border 2px `glitch-cyan`
- Hover: `bg-elevated`

---

## Glitch Effects

### Logo Animation (App Title)

- Chromatic aberration: 2px red/cyan offset on `::before`/`::after`
- Brief flicker: 80ms opacity pulse
- Triggered: on app open, on incident state change
- Duration: 200–400ms total
- Frequency: not looping — one-shot per trigger

### Incident Marker (Timeline)

- Left border with 1px `glitch-cyan`
- Critical: 1px animated dashed border with subtle pulse

### Diagnostic Scanning State (Why Slow? while analyzing)

- Scanline sweep animation across the result area (dark overlay moving top to bottom, 800ms)
- Monospaced text appears character-by-character (type effect, 20ms per char)
- One-shot, completes in < 2 seconds

### Warning State (Tile)

- Border: 1px `status-warning` with 50% opacity glow (box-shadow)
- No animation — static glow only

### Critical State (Tile)

- Border: 1px `status-critical` with pulse animation (0.5s ease-in-out, 2 cycles max)
- Then settles to static glow

---

## Icon Language

Use **Segoe Fluent Icons** (built into Windows 11) for system icons.

| Context | Icon |
|---|---|
| CPU | Chip / processor glyph |
| RAM | Memory module glyph |
| Disk | Hard drive glyph |
| GPU | Video card glyph |
| Incident | Warning triangle |
| Upgrade | Arrow up glyph |
| Settings | Gear glyph |
| Why Slow? | Magnifying glass |
| Pause | Pause symbol |

Custom glyphs are not needed for V1 — Fluent Icon set is sufficient.

---

## Texture

Subtle **scanline texture** on the main background (`bg-base`):
- 1px horizontal lines at 4px intervals
- Opacity: 3–5% — barely visible
- Adds texture without distraction
- Applied via CSS/XAML background brush, not an image file
- Does NOT appear on card surfaces

---

## Dark Mode Only

V1 is dark mode only.
Light mode is explicitly deferred.
Windows system theme is ignored in V1.
