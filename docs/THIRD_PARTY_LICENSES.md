# Third-Party Licenses

JuDoctor is licensed under the GNU General Public License v3.0. This document records the primary third-party packages used by the V1 shipping application and their upstream licenses.

> This file is a release-audit aid, not a substitute for the license files and notices distributed by each dependency. Package metadata and upstream repositories remain authoritative.

## Direct application dependencies

| Package | Version | License | Used by |
| --- | ---: | --- | --- |
| Microsoft.Extensions.Hosting.Abstractions | 9.0.0 | MIT | Core / Platform.Windows |
| Microsoft.Extensions.Logging.Abstractions | 9.0.0 | MIT | Core / Platform.Windows |
| Microsoft.Extensions.Hosting | 9.0.0 | MIT | Desktop |
| Serilog.Extensions.Hosting | 9.0.0 | Apache-2.0 | Desktop |
| Serilog.Sinks.File | 6.0.0 | Apache-2.0 | Desktop |
| CommunityToolkit.Mvvm | 8.4.0 | MIT | Desktop |
| System.Diagnostics.PerformanceCounter | 9.0.0 | MIT | Platform.Windows |
| System.Management | 9.0.0 | MIT | Platform.Windows |
| Vortice.DXGI | 2.4.2 | MIT | Platform.Windows |
| Microsoft.Data.Sqlite | 9.0.0 | MIT | Storage |
| Dapper | 2.1.35 | Apache-2.0 | Storage |

## Important transitive dependency note

The current public V1 tree references `Microsoft.Data.Sqlite 9.0.0`. Its dependency chain can resolve to `SQLitePCLRaw` 2.1.10 components, including the bundled native SQLite package.

As of the V1 public-release audit, `SQLitePCLRaw.lib.e_sqlite3` versions through 2.1.11 are affected by GitHub advisory `GHSA-2m69-gcr7-jv3q` / `CVE-2025-6965` (high severity). Because of that, **the current 9.0.0 dependency set must not be treated as release-ready**.

Release remediation target:

- update `Microsoft.Data.Sqlite` to a current compatible 9.0.x servicing release that resolves `SQLitePCLRaw.bundle_e_sqlite3` / `SQLitePCLRaw.lib.e_sqlite3` to 2.1.12 or newer;
- restore from the public repository;
- run a transitive vulnerability audit;
- rerun the full JuDoctor test suite;
- rebuild and smoke-test the installer.

At the time of this audit, `Microsoft.Data.Sqlite 9.0.20` is the intended minimal servicing target because its published dependency metadata requires `SQLitePCLRaw.bundle_e_sqlite3 >= 2.1.12`.

## Upstream references

- Microsoft.Extensions / Microsoft.Data.Sqlite / System.*: https://www.nuget.org/
- Serilog: https://github.com/serilog
- CommunityToolkit.Mvvm: https://github.com/CommunityToolkit/dotnet
- Dapper: https://github.com/DapperLib/Dapper
- Vortice.Windows: https://github.com/amerkoleci/Vortice.Windows
- SQLitePCL.raw: https://github.com/ericsink/SQLitePCL.raw
- GitHub advisory `GHSA-2m69-gcr7-jv3q`: https://github.com/advisories/GHSA-2m69-gcr7-jv3q

## Release policy

Before each public binary release:

1. restore dependencies from the tagged source tree;
2. run `dotnet list package --include-transitive --vulnerable`;
3. investigate any reported vulnerability affecting a shipped package;
4. keep this document aligned with direct shipping dependencies when versions change.

Last audited for the JuDoctor V1 public-release pass: 2026-09-18.
