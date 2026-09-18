# JuDoctor

> **Know why your Windows PC is slowing down — before replacing hardware blindly.**

[한국어](README_KO.md) · [Download](https://github.com/ju0o/JuDoctor/releases/latest) · [Releases](https://github.com/ju0o/JuDoctor/releases) · [Privacy](PRIVACY.md) · [Known issues](KNOWN_ISSUES.md)

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?logo=windows)](https://github.com/ju0o/JuDoctor)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![GitHub Release](https://img.shields.io/github/v/release/ju0o/JuDoctor?display_name=tag)](https://github.com/ju0o/JuDoctor/releases)
[![Downloads](https://img.shields.io/github/downloads/ju0o/JuDoctor/total)](https://github.com/ju0o/JuDoctor/releases)

JuDoctor is a **local-first Windows PC health monitor and upgrade advisor**. It runs quietly in the system tray, keeps a history of real workload behavior, explains likely bottlenecks, and waits for sustained evidence before recommending a CPU or RAM upgrade.

Task Manager tells you what the number is. JuDoctor tries to answer the question behind it:

> **“Does this actually matter, and is it really time to upgrade?”**

---

## ⬇ Download JuDoctor

### Windows 10 / 11 · x64

**[Download the latest Windows installer →](https://github.com/ju0o/JuDoctor/releases/latest)**

For the first stable public release, the release page provides:

- `JuDoctor-1.0.0-Setup.exe`
- `JuDoctor-1.0.0-Setup.exe.sha256`

Direct v1.0.0 links after the release is published:

- [JuDoctor-1.0.0-Setup.exe](https://github.com/ju0o/JuDoctor/releases/download/v1.0.0/JuDoctor-1.0.0-Setup.exe)
- [SHA256 checksum](https://github.com/ju0o/JuDoctor/releases/download/v1.0.0/JuDoctor-1.0.0-Setup.exe.sha256)

JuDoctor is published as a **self-contained Windows installer**. You do not need to install a separate .NET runtime.

### Install

1. Download `JuDoctor-1.0.0-Setup.exe` from GitHub Releases.
2. Run the installer.
3. Enable **Start with Windows** if you want JuDoctor to collect long-term history automatically.
4. Leave it in the system tray and open the Dashboard only when you want to inspect your PC.

> Early unsigned builds may trigger Windows SmartScreen because the publisher does not yet have established reputation. Verify the SHA256 against the checksum published in the same GitHub Release before running the installer.

PowerShell checksum example:

```powershell
Get-FileHash .\JuDoctor-1.0.0-Setup.exe -Algorithm SHA256
```

See [Installation](docs/INSTALLATION.md) for more details.

> **Release status:** the public source is already available in this repository. The downloadable installer is published through the repository's release workflow. If the direct v1.0.0 link above is not live yet, use the [Releases page](https://github.com/ju0o/JuDoctor/releases) to check the current binary release status.

---

## What JuDoctor does

- Runs quietly in the Windows system tray
- Monitors **CPU, RAM, GPU, VRAM, and disk**
- Records meaningful incidents instead of reacting to every short spike
- Provides **Why Was My PC Slow?** analysis
- Keeps local history in SQLite
- Distinguishes temporary load from sustained pressure
- Recommends **CPU or RAM upgrades only after enough historical evidence**
- Starts with Windows in tray-only mode when enabled
- Prevents duplicate app instances
- Works without an account or cloud service

### V1 recommendation scope

JuDoctor V1 can produce historical **CPU/RAM upgrade recommendations**. GPU, VRAM, disk, process and temperature data are used as diagnostic evidence/status where available; V1 does not make GPU/disk replacement recommendations.

### What it does not do

JuDoctor does not treat a brief `100% CPU` reading as proof that you need a new processor. It does not automatically kill processes, overclock hardware, or upload your activity history to a JuDoctor cloud service in V1.

---

## Why not just use Task Manager?

| Situation | Task Manager | JuDoctor |
| --- | --- | --- |
| CPU hits 100% during a short build | Shows 100% | Usually stays silent |
| RAM pressure repeats during real work | Shows current usage | Tracks repeated pressure over time |
| The PC was slow 15 minutes ago | Current state only | Uses recorded history |
| “Should I buy more RAM?” | You decide manually | Waits for sustained evidence |
| GPU sits at 99% while gaming | Shows 99% | Does not automatically recommend replacement |

JuDoctor is designed around **duration + repeated evidence + corroborating signals**, not one-off percentages.

---

## How the V1 diagnosis loop works

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

JuDoctor V1 keeps monitoring history on your PC. No JuDoctor account is required, and V1 does not require cloud telemetry upload.

JuDoctor is not designed to collect the contents of your:

- files or documents
- browser pages
- terminal commands
- passwords
- clipboard

See [PRIVACY.md](PRIVACY.md).

---

## Verified V1 baseline

The certified V1 baseline completed:

- **93 / 93 automated tests** at the final code-level audit
- **15 / 15 manual acceptance checks**
- real DXGI VRAM detection on an RTX 3050-class GPU (~5.9 GB dedicated VRAM)
- reboot autostart verification in tray-only mode
- database persistence across restart/reboot scenarios
- Resource Governor emergency/recovery verification
- restart-safe Level 4 CPU/RAM recommendation notification state

Certification verdict:

`USER_STABLE_PASS_WITH_KNOWN_ISSUES`

See [KNOWN_ISSUES.md](KNOWN_ISSUES.md).

---

## Screens

JuDoctor V1 includes:

- **Dashboard** — current system health at a glance
- **Incident History** — recorded abnormal periods
- **Why Was My PC Slow?** — evidence-based recent diagnosis
- **System Capacity** — long-term CPU/RAM capacity status
- **Settings** — startup and monitoring preferences

Public screenshots will be added here as part of the first binary release presentation.

---

## Build from source

Requirements:

- Windows 10/11
- .NET 9 SDK
- Inno Setup 6 only if you want to build the installer

```powershell
git clone https://github.com/ju0o/JuDoctor.git
cd JuDoctor
dotnet restore MainPCDoctor.sln
dotnet test MainPCDoctor.sln --configuration Release
.\installer\publish.ps1 -Installer
```

V1 intentionally uses a self-contained **folder publish** inside the installer. `PublishSingleFile=true` is not the supported V1 packaging path because of the accepted WPF pack-URI limitation.

---

## Release process

The repository includes `.github/workflows/release.yml`.

A `v*` tag such as `v1.0.0` runs the Windows release pipeline:

```text
checkout
  ↓
restore
  ↓
test
  ↓
self-contained win-x64 publish
  ↓
Inno Setup installer
  ↓
SHA256
  ↓
GitHub Release
```

The resulting assets are downloadable by normal users directly from [GitHub Releases](https://github.com/ju0o/JuDoctor/releases).

See [Releasing JuDoctor](docs/RELEASING.md).

---

## Report a problem

Hardware compatibility reports are especially useful. Please include your Windows version, CPU/GPU model, RAM amount, JuDoctor version, expected behavior and actual behavior.

**Review logs or databases yourself before attaching them to a public issue.**

- [Report a bug](https://github.com/ju0o/JuDoctor/issues/new?template=bug_report.yml)
- [Request a feature](https://github.com/ju0o/JuDoctor/issues/new?template=feature_request.yml)
- [Support](SUPPORT.md)

---

## License

JuDoctor source is released under **GNU GPL v3.0**. See [LICENSE](LICENSE).

The software license does not grant third parties the right to present modified builds as the official **JuDoctor** product. See [TRADEMARKS.md](TRADEMARKS.md).

---

## Project status

**JuDoctor V1 — Windows / Local-first / Open Source**

The V1 line is focused on stability, compatibility, privacy and reliable PC diagnosis. New features should be justified by real-user feedback rather than added to the stable line by default.

> Task Manager tells you what the number is. **JuDoctor tries to tell you whether that number actually matters.**
