# Real Windows monitoring smoke test

Runs the production MonitoringWorker, ResourceGovernor, Windows collectors, diagnosis,
incident tracking and SQLite aggregation for 80 seconds by default. Uses a new isolated database;
does not acquire the desktop app mutex or change the installed app/user history.
Notifications are suppressed in this QA harness. This is not a WPF/tray/reboot test.

From the project root on Windows:

```powershell
dotnet run --project qa/tools/MonitoringSmoke --configuration Release -- qa/evidence/my-new-smoke.db
```

The DB path must not already exist. Success requires at least two persisted aggregates
and a clean worker shutdown. Snapshot counts and the final sample are printed to stdout.
This short check does not establish 30-minute stability or installed UI behavior.

For a real-time controlled governor check on Windows:

```powershell
dotnet run --project qa/tools/MonitoringSmoke --configuration Release -- qa/evidence/my-new-controlled.db 350 governor-scenario
```

This QA host alone injects private-memory readings: 200 MiB initially, 450 MiB from
75 through 260 seconds, then 200 MiB. It does not allocate memory to induce pressure.
CPU/RAM/detail measurements come from the real Windows collectors. The injected
self-memory readings are explicitly marked as controlled evidence in stdout;
actual harness private memory is separately recorded. No test switch is added to
the production application. Full/minimum collector call counts and clean shutdown
are recorded. Deterministic tests additionally advance time through 12 cycles,
including sustained pressure, governor read failure, and recovery.

`../ObserveProduction.ps1 -TargetPid <pid>` records process resource use and the
real user DB every 30 seconds until the target process has run at least 635 seconds.
Run it from the project root. Its evidence folder is `qa/evidence/v1-governor-final/`.
