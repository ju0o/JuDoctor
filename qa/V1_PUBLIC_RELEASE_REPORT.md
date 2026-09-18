# JuDoctor V1 Public Release Report

PUBLIC_HEAD_SHA=b91a7c0
SQLITE_VERSION=9.0.20
SQLITEPCLRAW_VERSION=2.1.12
VULNERABILITY_AUDIT=PASS
BUILD=PASS
TESTS=93/93 PASS
INSTALLER_SHA256=C110997997EC42C2AC73C7541474E30292DB3ABC6E07768DE0A7CE60C6D4C98B
INSTALLER_SMOKE=PARTIAL
REBOOT_SMOKE=NOT_RUN
SCREENSHOTS=PASS

## Evidence

- `dotnet restore MainPCDoctor.sln` completed successfully.
- The transitive graph resolved `SQLitePCLRaw.bundle_e_sqlite3 2.1.12` and `SQLitePCLRaw.lib.e_sqlite3 2.1.12`.
- `dotnet list MainPCDoctor.sln package --include-transitive --vulnerable` reported no vulnerable packages for all seven solution projects.
- `dotnet build MainPCDoctor.sln --configuration Release --no-restore` completed with 0 warnings and 0 errors.
- `dotnet test MainPCDoctor.sln --configuration Release --no-build` passed Core 53, Storage 3, and Integration 37 tests.
- The exact required installer was hash-verified before installation and installed with exit code 0. Installed metadata reports JuDoctor 1.0.0 by ju0o.
- The installed executable launched from `%LOCALAPPDATA%\\Programs\\MainPCDoctorV1` and showed a JuDoctor window. Exactly one `MainPCDoctor.Desktop` process was observed.
- The app-only screenshots were captured at `docs/screenshots/dashboard.png`, `docs/screenshots/why-was-my-pc-slow.png`, and `docs/screenshots/system-capacity.png` and are linked from both README files.
- X-to-hide was verified: the Dashboard window became hidden while the process remained alive. The tray item was observed as `JuDoctor`.
- The installed startup value points to the installed executable with `--tray`, and the database WAL continued receiving records after launch.

## Outstanding smoke evidence

- Tray → Open JuDoctor did not reliably reopen the hidden Dashboard through the available native automation surface.
- Tray → Exit was not verified.
- A reboot with startup enabled was not performed after the user explicitly instructed: “끄지마 컴퓨터” (“do not turn off the computer”). Therefore tray-only startup after reboot and post-reboot DB continuity are not claimed.

FINAL_VERDICT=RELEASE_BLOCKED

No `v1.0.0` tag was created.
