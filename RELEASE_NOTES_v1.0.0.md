# JuDoctor v1.0.0

First public Windows release of JuDoctor.

## What JuDoctor does

JuDoctor runs quietly in the Windows tray, records system-health history, explains likely resource bottlenecks, and waits for enough evidence before recommending CPU or RAM upgrades.

## Highlights

- CPU / RAM / GPU / VRAM / Disk monitoring
- Dashboard
- Incident History
- **Why Was My PC Slow?**
- System Capacity
- CPU/RAM upgrade recommendation model
- Minimum 14-day observation gate
- Restart-safe Level 4 recommendation notification state
- Windows startup + tray mode
- Single-instance guard
- Local-only SQLite data storage
- Resource Governor reduced-monitoring emergency mode
- DXGI dedicated VRAM detection
- No account required
- No cloud required

## Validation

The V1 release candidate completed internal automated and manual certification, including real Windows reboot, tray, persistence, DXGI VRAM, recommendation-gate, Resource Governor, and Level 4 scheduling checks.

Final code-level audit: **93/93 automated tests passing**.

Manual acceptance: **15/15 checks passing**.

## Known limitation

Single-file publishing is not supported in V1. The official installer uses a self-contained folder publish.

Some optional lower-level sensors may be unavailable depending on hardware and drivers.

## Install

Download:

`JuDoctor-1.0.0-Setup.exe`

and verify it against:

`JuDoctor-1.0.0-Setup.exe.sha256`

If an early unsigned build triggers Windows SmartScreen, verify that the installer came from the official `ju0o/JuDoctor` GitHub Release and that its SHA256 matches before proceeding.

## Recommendation warm-up

CPU/RAM upgrade recommendations require a minimum 14-day observation window. Day 14 only makes a recommendation eligible for evaluation; it does not automatically create one.
