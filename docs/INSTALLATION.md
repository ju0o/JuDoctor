# Installation

## Recommended installation

Download the latest official installer from:

https://github.com/ju0o/JuDoctor/releases/latest

Expected release assets:

- `JuDoctor-<version>-Setup.exe`
- `JuDoctor-<version>-Setup.exe.sha256`

## Verify the SHA256

PowerShell:

```powershell
Get-FileHash .\JuDoctor-1.0.0-Setup.exe -Algorithm SHA256
```

Compare the printed hash with the `.sha256` file attached to the same GitHub Release.

## SmartScreen on early releases

Early unsigned releases may show a Windows SmartScreen warning because the publisher/app does not yet have established reputation.

Only install files downloaded from the official `ju0o/JuDoctor` GitHub Releases page and verify the SHA256 when in doubt.

## First launch

JuDoctor runs in the current user session.

- The Dashboard can be opened manually.
- Closing the Dashboard hides the window but keeps the tray monitor alive.
- Tray → Exit performs the actual application shutdown.
- `Start with Windows` can be enabled in Settings.
- Startup mode runs tray-only and should not force the Dashboard to open.
- A single-instance guard prevents duplicate monitoring processes.

## Local data

V1 currently stores its local history/configuration under:

```text
%APPDATA%\MainPCDoctor\
```

The public brand is JuDoctor, but this internal path remains unchanged in V1 to avoid a cosmetic data migration.

## Recommendation warm-up

CPU/RAM upgrade recommendations require at least 14 days of observation. During the warm-up period, System Capacity should display a collecting-data state rather than a premature recommendation.

## Uninstall

Use Windows **Installed apps / Apps & features** and uninstall JuDoctor.

The final release notes should explicitly state whether the installer version removes or retains local history under `%APPDATA%\MainPCDoctor\`.

## Portable / single-file build

V1 does not provide an official single-file portable executable. WPF pack-URI resource behavior is a known limitation of the current single-file configuration.
