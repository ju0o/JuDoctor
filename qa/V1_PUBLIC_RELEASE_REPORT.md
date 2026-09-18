# JuDoctor V1 Public Release Report

**Date:** 2026-09-18  
**Release target:** `v1.0.0`  
**Repository:** `ju0o/JuDoctor`  
**Status:** `RELEASE_BLOCKED`

## Public test baseline

The public source tree was reported with:

| Project | Result |
| --- | ---: |
| MainPCDoctor.Core.Tests | 53 / 53 PASS |
| MainPCDoctor.Storage.Tests | 3 / 3 PASS |
| MainPCDoctor.Integration.Tests | 37 / 37 PASS |
| **Total** | **93 / 93 PASS** |

This baseline predates the dependency security remediation described below. The full suite must be rerun after that remediation before the release tag is created.

## Completed public-release work

- Public source migration complete.
- Raw `qa/evidence/` removed from the current public tree.
- Public product branding changed to **JuDoctor** while V1 internal `MainPCDoctor.*` identifiers remain for compatibility.
- Installer metadata changed to JuDoctor / publisher `ju0o` / `JuDoctor-1.0.0-Setup` output naming.
- README / README_KO use GitHub Releases as the end-user download path.
- Canonical GNU GPL v3.0 license text is present in root `LICENSE`.
- Third-party dependency/license audit is documented in `docs/THIRD_PARTY_LICENSES.md`.
- Public docs index and screenshot requirements are present under `docs/`.

## Release blocker — bundled SQLite dependency

The public Storage project now references `Microsoft.Data.Sqlite 9.0.20`.

The dependency audit found that this version can resolve a `SQLitePCLRaw.lib.e_sqlite3` version in the affected range for:

- GitHub advisory: `GHSA-2m69-gcr7-jv3q`
- CVE: `CVE-2025-6965`
- Severity: High

The V1 installer must not be treated as a final public release candidate until this dependency path is remediated and revalidated.

### Intended remediation

The verified restore resolves `SQLitePCLRaw.bundle_e_sqlite3 2.1.12` and `SQLitePCLRaw.lib.e_sqlite3 2.1.12` through `Microsoft.Data.Sqlite 9.0.20`.

After updating:

```powershell
dotnet restore MainPCDoctor.sln
dotnet list MainPCDoctor.sln package --include-transitive --vulnerable
dotnet build MainPCDoctor.sln --configuration Release --no-restore
dotnet test MainPCDoctor.sln --configuration Release --no-build
```

Required result:

- no vulnerable shipped package reported for this advisory;
- 0 build errors;
- 0 test failures.

### Security remediation verification

- `Microsoft.Data.Sqlite`: `9.0.20`
- `SQLitePCLRaw.bundle_e_sqlite3`: `2.1.12`
- `SQLitePCLRaw.lib.e_sqlite3`: `2.1.12`
- Vulnerability audit: PASS; all seven solution projects reported no vulnerable packages.
- Release build: PASS; 0 warnings and 0 errors.
- Regression tests: `93 / 93 PASS` (Core 53, Storage 3, Integration 37).
- Verified installer SHA256: `C110997997EC42C2AC73C7541474E30292DB3ABC6E07768DE0A7CE60C6D4C98B`.

## Installer gate

Final installer build and installed-build smoke are **not certified yet** for the remediated dependency set.

After the security blocker is cleared:

```powershell
.\installer\publish.ps1 -Installer
```

Expected artifact:

`installer/output/JuDoctor-1.0.0-Setup.exe`

Then record:

- installer SHA256;
- JuDoctor branding in installer UI;
- tray startup behavior;
- Dashboard open / close-to-tray / reopen;
- DB record growth;
- single-instance behavior;
- clean Tray → Exit;
- reboot/autostart smoke.

## Screenshot gate

Expected reviewed public screenshots:

- `docs/screenshots/dashboard.png`
- `docs/screenshots/why-was-my-pc-slow.png`
- `docs/screenshots/system-capacity.png`

No screenshot may expose usernames, absolute local paths, private logs, tokens, documents, or unrelated desktop content.

## Release verdict

`RELEASE_BLOCKED`

Do not create or push `v1.0.0` until the SQLite dependency blocker is cleared, the full suite is green again, and the resulting installer passes the final smoke checks.
