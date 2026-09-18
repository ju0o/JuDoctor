# Changelog

All notable public changes to JuDoctor will be documented here.

The project follows semantic versioning for public releases where practical.

## [Unreleased]

### Added
- Public GitHub repository bootstrap
- Release/download documentation
- Public privacy, security, support, contribution, roadmap, and known-issues docs
- GitHub issue templates and automated release workflow

## [1.0.0] - pending public release

### Added
- Windows 10/11 x64 desktop application
- Background tray monitoring
- CPU, RAM, GPU, VRAM, and disk monitoring
- Incident History
- Why Was My PC Slow?
- System Capacity view
- CPU/RAM upgrade recommendation model
- Minimum 14-day observation gate
- Persistent Level 4 recommendation notification state
- Windows startup support
- Single-instance enforcement
- Local SQLite persistence
- Resource Governor with reduced-monitoring emergency mode and automatic recovery
- DXGI dedicated VRAM detection

### Validation
- 93/93 automated tests passed at the final code-level V1 audit
- 15/15 manual V1 acceptance checks passed
- Reboot autostart, local persistence, tray lifecycle, and actual Windows runtime behavior verified

### Known limitations
- Single-file publish is not supported in V1; the official installer wraps a self-contained folder publish.
- Some hardware sensors are best-effort and may be unavailable on specific systems.
