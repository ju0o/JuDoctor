# JuDoctor

> **Know why your Windows PC is slowing down — before replacing hardware blindly.**

[한국어 README](README_KO.md) · [Download](https://github.com/ju0o/JuDoctor/releases/latest) · [Known issues](KNOWN_ISSUES.md) · [Privacy](PRIVACY.md)

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)](https://github.com/ju0o/JuDoctor)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/ju0o/JuDoctor?display_name=tag)](https://github.com/ju0o/JuDoctor/releases)
[![Downloads](https://img.shields.io/github/downloads/ju0o/JuDoctor/total)](https://github.com/ju0o/JuDoctor/releases)

JuDoctor is a **local-first Windows PC health monitor and upgrade advisor**. It runs quietly in the system tray, records real workload history, explains likely bottlenecks, and waits for sustained evidence before recommending a CPU or RAM upgrade.

Task Manager shows numbers. JuDoctor tries to answer the question behind them:

> **“Do I actually need to upgrade my PC — and what is the evidence?”**

---

## Download

### Windows 10 / 11 x64

**[⬇ Download the latest JuDoctor installer](https://github.com/ju0o/JuDoctor/releases/latest)**

Official release assets are published through **GitHub Releases**:

- `JuDoctor-<version>-Setup.exe`
- `JuDoctor-<version>-Setup.exe.sha256`

JuDoctor V1 uses a **self-contained folder publish wrapped by the installer**, so users do not need to install a separate .NET runtime.

> Early unsigned builds may trigger Windows SmartScreen because the publisher does not yet have established reputation. If that happens, verify the installer SHA256 against the hash published in the same GitHub Release.

See [Installation](docs/INSTALLATION.md) for details.

---

## What JuDoctor does

- Runs quietly in the Windows system tray
- Monitors **CPU, RAM, GPU, VRAM, and disk**
- Records incidents instead of reacting to every short spike
- Provides **Why Was My PC Slow?** analysis
- Keeps local incident/history data in SQLite
- Distinguishes temporary load from sustained pressure
- Recommends **CPU or RAM upgrades only after enough historical evidence**
- Starts with Windows in tray-only mode when enabled
- Prevents duplicate app instances
- Works without an account or cloud service

### What it does *not* do

JuDoctor does not treat a brief `100% CPU` reading as proof that you need a new processor. It does not automatically kill processes, overclock hardware, edit the registry, or upload your activity history to a cloud service in V1.

---

## Why not just use Task Manager?

A single utilization number has very little context.

| Situation | Task Manager | JuDoctor |
| --- | --- | --- |
| CPU reaches 100% for a short build | Shows 100% | Usually stays silent |
| RAM pressure repeats across real work sessions | Shows current RAM | Tracks repeated pressure over time |
| PC felt slow 15 minutes ago | Current state only | Reviews recent recorded evidence |
| “Should I buy more RAM?” | You decide manually | Waits for sustained multi-signal evidence |
| GPU is legitimately at 99% while gaming | Shows 99% | Does not automatically warn or recommend replacement |

JuDoctor is designed around **duration + repeated evidence + corroborating signals**, not one-off percentages.

---

## V1 diagnosis loop

```text
Background monitoring
        ↓
Candidate abnormality
        ↓
Multi-signal confirmation
        ↓
Incident
        ↓
Diagnosis / history
        ↓
Meaningful notification when required
        ↓
Long-term evidence
        ↓
CPU / RAM upgrade recommendation
```

CPU/RAM upgrade recommendations have a **minimum 14-day observation gate**. Reaching day 14 does not automatically generate a recommendation; the evidence still has to qualify.

---

## Privacy: local-first by default

JuDoctor V1 keeps monitoring history on your PC.

It does **not** need to collect:

- file contents
- browser page contents
- terminal/command text
- passwords
- clipboard contents
- personal documents

No JuDoctor account is required, and V1 does not require cloud telemetry upload.

See [PRIVACY.md](PRIVACY.md).

---

## Verified V1 status

The V1 release candidate completed internal certification with:

- **93 / 93 automated tests** passing at the final code-level audit
- **15 / 15 manual acceptance checks** passing
- real DXGI VRAM detection on an RTX 3050-class GPU (~5.9 GB dedicated VRAM)
- reboot autostart verified in tray-only mode
- database persistence verified across restart/reboot scenarios
- Resource Governor emergency/recovery monitoring verified
- Level 4 CPU/RAM recommendation scheduling and restart-safe deduplication verified

Certification verdict:

`USER_STABLE_PASS_WITH_KNOWN_ISSUES`

See [KNOWN_ISSUES.md](KNOWN_ISSUES.md) for the accepted V1 limitations.

---

## Screens

JuDoctor V1 includes:

- **Dashboard** — current system health at a glance
- **Incident History** — recorded abnormal periods
- **Why Was My PC Slow?** — recent evidence-based diagnosis
- **System Capacity** — long-term CPU/RAM capacity status
- **Settings** — startup and monitoring preferences

Screenshots will be added to this README with the first public binary release.

---

## Build from source

JuDoctor is a Windows WPF application built with .NET. The public source tree is organized around separate monitoring, diagnostics, storage, platform, desktop, and test responsibilities.

For the first public release, use the provided release tooling rather than `PublishSingleFile=true`; WPF pack-URI resources have a known single-file limitation in V1.

Public source/build instructions will live alongside the sanitized source migration. See [ROADMAP.md](ROADMAP.md).

---

## Release channels

| Channel | Purpose |
| --- | --- |
| `v1.0.x` | Stability, compatibility, installer, privacy/security fixes |
| `v1.1+` | Only features justified by repeated real-user feedback |
| Linux/macOS | Not part of V1 |

JuDoctor intentionally avoids turning every idea into the current release line. V1 exists to prove that the core monitoring and recommendation loop works reliably on real Windows PCs.

---

## Report a problem

Hardware compatibility reports are especially useful. When filing an issue, please include Windows version, CPU/GPU model, installed RAM, JuDoctor version, what you expected, and what actually happened. **Do not upload private databases/logs without reviewing them first.**

- [Report a bug](https://github.com/ju0o/JuDoctor/issues/new?template=bug_report.yml)
- [Request a feature](https://github.com/ju0o/JuDoctor/issues/new?template=feature_request.yml)
- [Support](SUPPORT.md)

---

## License

Source code is released under the **GNU General Public License v3.0**. See [LICENSE](LICENSE).

The license does not grant third parties the right to present modified distributions as the official **JuDoctor** product. See [TRADEMARKS.md](TRADEMARKS.md).

---

## Project status

**JuDoctor V1 — Windows / Local-first / Public release track**

Current focus: ship a trustworthy Windows release, collect compatibility feedback from real PCs, and keep the `v1.0.x` line stability-only.

> Task Manager tells you what the number is. **JuDoctor tries to tell you whether that number actually matters.**
