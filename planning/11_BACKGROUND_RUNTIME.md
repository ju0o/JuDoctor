# 11 — Background Runtime
*Revised: 2026-09-17 — Gate Review 01*

---

## Architecture

Background monitoring runs as an `IHostedService` inside the single desktop process. There is no separate Windows Service, no second executable, no IPC channel.

All background components live in `MainPCDoctor.Desktop/Background/`:

```
Background/
├── MonitoringWorker.cs      ← IHostedService; the main sample loop
├── SamplingScheduler.cs     ← 3-state adaptive rate controller
├── ResourceGovernor.cs      ← Self-CPU check; skips cycle if app is too expensive
└── StartupRegistrar.cs      ← HKCU Run key management
```

---

## MonitoringWorker

Implements `IHostedService` and is registered via `services.AddHostedService<MonitoringWorker>()`.

```
MonitoringWorker.StartAsync():
  1. Initialize PDH queries (CPU, Memory, Disk, GPU counters — once per run)
  2. Verify sensor availability (LHM, DXGI, PDH GPU counters)
  3. Start sampling loop

SamplingLoop():
  while not cancelled:
    1. ResourceGovernor.CheckShouldSkip()
       → skip cycle if app's own CPU > 3% (self-throttle)
    2. Collect SystemSnapshot via ISystemMetricsCollector
    3. RollingMetricsBuffer.Add(snapshot)
    4. DiagnosisEngine.Evaluate(buffer.RecentWindow(300s))
       → IncidentTracker.Update(diagnosisResult)
          → on new confirmed incident: SQLiteIncidentStore.Save()
          → on RAM confirmed: also Notify(Level 2)
    5. MetricsAggregator.TryFlush()   [flushes once per 60s]
    6. await Task.Delay(SamplingScheduler.CurrentInterval, cancellationToken)

StopAsync():
  1. Cancel the loop
  2. Flush any pending 1-minute aggregate
  3. Mark any Active incidents as Paused (not Resolved)
  4. Dispose PDH queries
```

---

## SamplingScheduler

Controls the interval between sample cycles. Three states:

```
State Machine:
                   idle 10 min
   ┌──────────────────────────────────────────────────────────┐
   ▼                                                          │
 [Eco]                         [Normal]               [Incident]
 30s/cycle                    10s/cycle                 3s/cycle
   │                              │                        │
   └── CPU>20% OR RAM>60% ───────▶│                        │
                                  ├── candidate detected ──▶│
                                  │                        │
                                  └─── 60s after all ─────-┘
                                       incidents resolve
```

| State | Interval | Entry Condition |
|---|---|---|
| Eco | 30 seconds | Startup (Low intensity) OR 10 min of idle CPU < 5% and RAM < 40% |
| Normal | 10 seconds | Default; CPU > 20% OR RAM > 60% triggers from Eco |
| Incident | 3 seconds | Any rule candidate detected |

**Watch Mode has been removed.** The previous 4-state machine (Eco / Normal / Watch / Incident) is simplified to 3 states. Watch Mode added complexity without meaningful benefit: Normal at 10 seconds provides sufficient resolution for most rule confirmation windows.

---

## ResourceGovernor

Prevents MainPC Doctor from being its own bottleneck.

```csharp
// Pseudocode
bool CheckShouldSkip()
{
    var selfCpuPercent = GetSelfCpuPercent();  // own process CPU time delta
    if (selfCpuPercent > 3.0f)
    {
        _logger.LogDebug("Skipping cycle: self-CPU {cpu:F1}%", selfCpuPercent);
        _skippedCycleCount++;
        return true;  // caller skips this cycle
    }
    return false;
}
```

If the app skips 5+ consecutive cycles, it logs a warning and moves to Eco state.

---

## StartupRegistrar

Manages the HKCU Run key for Windows startup:

```
Key:   HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
Name:  MainPCDoctor
Value: "C:\...\MainPCDoctor.exe" --background
```

- No elevation required (HKCU).
- `Register()` called on first install and when user enables "Start with Windows" in Settings.
- `Unregister()` called when user disables the setting.
- Settings change is applied immediately (no restart required for the registry change itself).

**Settings apply on restart:** Settings changes take effect on the next app launch, not live. There is no `FileSystemWatcher` on `settings.json`. Changing monitoring intensity in Settings updates the stored value; the new interval is used the next time the app starts. This is acceptable for V1 — monitoring intensity is not a frequently changed setting.

---

## Host Setup

The Generic Host manages background services. WPF manages the UI and process lifetime. The entry point connects them.

**`UseWpfLifetime()` is not used.** It is not a standard Microsoft API. V1 uses `Microsoft.Extensions.Hosting` only — no third-party WPF hosting package.

```csharp
// Program.cs
[STAThread]
static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureLogging(logging =>
        {
            logging.AddSerilog(new LoggerConfiguration()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger());
        })
        .ConfigureServices(services =>
        {
            // ... service registrations — see 05_SYSTEM_ARCHITECTURE.md
        })
        .Build();

    host.Start();                                         // MonitoringWorker starts on background thread

    var wpfApp = new App(host.Services);
    wpfApp.Run();                                         // blocks on STA thread until Shutdown()

    host.StopAsync(TimeSpan.FromSeconds(10))              // stop monitoring, flush SQLite
        .GetAwaiter().GetResult();
    host.Dispose();
}
```

**There is no `AddWpfBlazorServices()`, Blazor, or any embedded browser in V1.** WPF is used exclusively.

## WPF Shutdown Policy

MainPC Doctor is a background-first application. **Closing the Dashboard window must not terminate the process.**

### ShutdownMode

```csharp
// App.xaml.cs — OnStartup
ShutdownMode = ShutdownMode.OnExplicitShutdown;
```

`ShutdownMode.OnExplicitShutdown` means the application only exits when `Application.Current.Shutdown()` is explicitly called. Closing all windows does not exit the process.

### Dashboard Window Close Behavior

The Dashboard window intercepts close and hides itself:

```csharp
// MainWindow.xaml.cs
protected override void OnClosing(CancelEventArgs e)
{
    e.Cancel = true;   // suppress actual close
    Hide();            // hide window; process stays alive
}
```

The window is not destroyed — it is hidden. Reopening it (from tray or notification) calls `Show()` on the existing instance.

### Application Shutdown Triggers

`Application.Current.Shutdown()` is called only from:

| Trigger | Handler |
|---|---|
| User selects "Exit" from tray menu | `TrayController.OnExitClick()` |
| Windows session ending (logoff/shutdown) | `App.OnSessionEnding()` |

```csharp
// TrayController.cs
private void OnExitClick(object sender, EventArgs e)
{
    _notifyIcon.Visible = false;
    _notifyIcon.Dispose();
    Application.Current.Shutdown();    // returns to Main(); host.StopAsync() then runs
}

// App.xaml.cs
protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
{
    // Do not cancel the session; begin graceful shutdown
    Application.Current.Shutdown();
}
```

### Shutdown Sequence

```
Application.Current.Shutdown() called
    → WPF event loop exits
    → App.Run() returns in Main()
    → host.StopAsync(10s) called
        → MonitoringWorker.StopAsync()
            → sampling loop cancelled
            → pending metrics flushed to SQLite
            → open PDH queries closed
        → all IHostedService instances stopped
    → host.Dispose()
    → Process exits
```

**10-second bounded shutdown:** `StopAsync(TimeSpan.FromSeconds(10))` is the hard limit. The SQLite WAL flush completes in < 100 ms under normal conditions. If the host does not stop within 10 seconds, the CLR forces termination — the WAL file is safe because SQLite WAL mode survives abrupt termination.

---

## Crash Recovery

On the next startup after an abrupt exit (crash, task-killed, power-off during run):

1. `RetentionManager.RecoverOrphans()` runs before the monitoring loop starts.
2. Any incident with `Status = Active` and `LastSeenAt < (now - 10 minutes)` is marked `Resolved` with `ResolveReason = OrphanClosed`.
3. The PDH query initialization proceeds normally — no state is carried from the previous session.
4. Normal monitoring starts.

Crash recovery is a startup operation only. It does not require a separate recovery worker.

---

## Process Startup Flow

```
Windows login → HKCU Run key fires MainPCDoctor.exe
                        │
                        ▼
              [STAThread] Main()
              Host.CreateDefaultBuilder().Build()
                        │
                        ▼
              host.Start()
                  └── MonitoringWorker.StartAsync() [background thread]
                          ├── PDH queries initialized
                          ├── Sensor availability probed
                          ├── RetentionManager.RecoverOrphans()
                          ├── StartupRegistrar.EnsureRegistered()
                          └── Sampling loop started (Normal state)
                        │
                        ▼
              new App(host.Services).Run()  [STA thread — blocks]
                  ├── ShutdownMode = OnExplicitShutdown
                  ├── TrayController.Initialize()
                  │       └── NotifyIcon appears in system tray
                  └── Main window hidden (not created until first open)
                        │
              [continues running until Shutdown() called]
```

---

## Main Window Lifecycle

The main WPF window is created lazily — not at startup. It is created on first open (tray double-click, toast click, or menu item).

Once created:
```
ShowInTaskbar = true  (when visible)
```

**Closing the window (X button) hides it — does not close it:**
```csharp
protected override void OnClosing(CancelEventArgs e)
{
    e.Cancel = true;
    Hide();
}
```

The window instance is reused. `Show()` is called to make it visible again.

It becomes visible when:
- User double-clicks the tray icon
- User selects any screen from the tray context menu
- User clicks a Level 2 or Level 4 toast notification

**Application exits only when:**
- User selects "Exit" from the tray context menu
- Windows session ends (logoff, shutdown)

Neither of these is triggered by closing the Dashboard window.
