# 05 — System Architecture
*Revised: 2026-09-17 — Gate Review 01*

---

## Architecture Principles

- **Vertical separation:** Platform → Core → UI. No layer skips.
- **Collector isolation:** Windows APIs live only in `Platform.Windows`. The diagnosis engine never imports Windows namespaces.
- **Single process:** Background monitoring, tray, and UI run in one user-session process. No IPC, no Windows Service, no second executable.
- **Background-first:** The background worker is independent of the UI. The main window is created on demand.
- **Storage abstraction:** No component outside `Storage` holds a direct SQLite reference.
- **Testability:** Collectors implement interfaces; Core rules run on `net8.0` with mock collectors in unit tests.

---

## ASCII Architecture Diagram

```
╔══════════════════════════════════════════════════════════════════════╗
║                         MAINPC DOCTOR                               ║
╠══════════════════════════════════════════════════════════════════════╣
║                                                                      ║
║   ┌─────────────────────────────────────────────────────────────┐   ║
║   │  MainPCDoctor.Desktop  (WPF / .NET 8 / net8.0-windows)      │   ║
║   │                                                             │   ║
║   │  App.xaml             TrayController                        │   ║
║   │  Views/               ViewModels/                           │   ║
║   │  Background/                                                │   ║
║   │    MonitoringWorker   SamplingScheduler                     │   ║
║   │    IncidentTracker    ResourceGovernor                      │   ║
║   │    StartupRegistrar                                         │   ║
║   └──────────────────────────┬──────────────────────────────────┘   ║
║                              │ uses (via DI)                         ║
║          ┌───────────────────┼───────────────────┐                  ║
║          │                   │                   │                  ║
║   ┌──────▼───────────┐   ┌───▼────────────┐   ┌─▼──────────────┐  ║
║   │ Platform.Windows │   │  Core          │   │  Storage        │  ║
║   │ net8.0-windows   │   │  net8.0        │   │  net8.0         │  ║
║   │                  │   │                │   │                 │  ║
║   │ CpuCollector     │   │ Abstractions/  │   │ SQLiteMetrics   │  ║
║   │ MemoryCollector  │──▶│ Models/        │◀──│ SQLiteIncident  │  ║
║   │ DiskCollector    │   │ History/       │   │ UpgradeStore    │  ║
║   │ GpuCollector     │   │ Diagnosis/     │   │ Aggregator      │  ║
║   │ ProcessCollector │   │   Rules/       │   │ RetentionMgr    │  ║
║   │ NotifyService    │   │ Incidents/     │   │ MigrationRunner │  ║
║   └──────────────────┘   │ Recommendations│   └─────────────────┘  ║
║                           └────────────────┘                        ║
╚══════════════════════════════════════════════════════════════════════╝
```

---

## Project Structure

**4 source projects. Background components are in Desktop.**

```
MainPCDoctor.sln
├── src/
│   ├── MainPCDoctor.Core/                  (net8.0 — no Windows dependencies)
│   │   ├── Abstractions/
│   │   │   ├── ISystemMetricsCollector.cs
│   │   │   ├── IIncidentStore.cs
│   │   │   ├── IDiagnosisEngine.cs
│   │   │   └── INotificationService.cs
│   │   ├── Models/
│   │   │   ├── SystemSnapshot.cs
│   │   │   ├── CpuMetrics.cs
│   │   │   ├── MemoryMetrics.cs
│   │   │   ├── DiskMetrics.cs
│   │   │   ├── GpuMetrics.cs
│   │   │   ├── ProcessSummary.cs
│   │   │   ├── ProcessMetric.cs
│   │   │   ├── Incident.cs
│   │   │   ├── DiagnosisResult.cs
│   │   │   ├── BottleneckType.cs
│   │   │   ├── ConfidenceLevel.cs
│   │   │   └── UpgradeRecommendation.cs
│   │   ├── History/
│   │   │   └── RollingMetricsBuffer.cs
│   │   ├── Diagnosis/
│   │   │   ├── DiagnosisEngine.cs
│   │   │   ├── DiagnosisContext.cs
│   │   │   ├── DiagnosisThresholds.cs
│   │   │   └── Rules/
│   │   │       ├── IDiagnosisRule.cs
│   │   │       ├── RamPressureRule.cs
│   │   │       ├── CpuBottleneckRule.cs
│   │   │       ├── DiskBottleneckRule.cs
│   │   │       ├── VramPressureRule.cs
│   │   │       ├── GpuComputeRule.cs
│   │   │       ├── ThermalThrottlingRule.cs
│   │   │       └── ProcessAnomalyRule.cs
│   │   ├── Incidents/
│   │   │   └── IncidentTracker.cs
│   │   └── Recommendations/
│   │       └── UpgradeRecommendationEngine.cs
│   │
│   ├── MainPCDoctor.Storage/               (net8.0)
│   │   ├── SQLiteMetricsStore.cs
│   │   ├── SQLiteIncidentStore.cs
│   │   ├── UpgradeRecommendationStore.cs
│   │   ├── MetricsAggregator.cs
│   │   ├── RetentionManager.cs
│   │   └── Migrations/
│   │       ├── 001_InitialSchema.sql
│   │       └── MigrationRunner.cs
│   │
│   ├── MainPCDoctor.Platform.Windows/      (net8.0-windows)
│   │   ├── Collectors/
│   │   │   ├── WindowsCpuCollector.cs
│   │   │   ├── WindowsMemoryCollector.cs
│   │   │   ├── WindowsDiskCollector.cs
│   │   │   ├── WindowsGpuCollector.cs
│   │   │   └── WindowsProcessCollector.cs
│   │   ├── WindowsSystemMetricsCollector.cs
│   │   └── WindowsNotificationService.cs
│   │
│   └── MainPCDoctor.Desktop/               (net8.0-windows, WPF)
│       ├── App.xaml / App.xaml.cs
│       ├── Program.cs                      ← Generic Host entry point
│       ├── TrayController.cs
│       ├── Background/                     ← Background components live here
│       │   ├── MonitoringWorker.cs
│       │   ├── SamplingScheduler.cs
│       │   ├── ResourceGovernor.cs
│       │   └── StartupRegistrar.cs
│       ├── Views/
│       │   ├── DashboardView.xaml
│       │   ├── IncidentListView.xaml
│       │   ├── IncidentDetailView.xaml
│       │   ├── WhySlowView.xaml
│       │   ├── DiagnosisResultView.xaml
│       │   ├── SystemCapacityView.xaml
│       │   └── SettingsView.xaml
│       └── ViewModels/
│           ├── DashboardViewModel.cs
│           ├── IncidentListViewModel.cs
│           ├── IncidentDetailViewModel.cs
│           ├── WhySlowViewModel.cs
│           ├── SystemCapacityViewModel.cs
│           └── SettingsViewModel.cs
│
└── tests/
    ├── MainPCDoctor.Core.Tests/            (net8.0 — no Windows required)
    │   ├── Rules/
    │   │   ├── RamPressureRuleTests.cs
    │   │   ├── CpuBottleneckRuleTests.cs
    │   │   ├── DiskBottleneckRuleTests.cs
    │   │   └── DiagnosticOnlyRuleTests.cs
    │   ├── DiagnosisEngineTests.cs
    │   ├── IncidentTrackerTests.cs
    │   └── UpgradeRecommendationEngineTests.cs
    ├── MainPCDoctor.Storage.Tests/         (net8.0)
    │   ├── MetricsStoreTests.cs
    │   ├── IncidentStoreTests.cs
    │   └── RetentionManagerTests.cs
    └── MainPCDoctor.Integration.Tests/    (net8.0-windows)
        └── BackgroundWorkerSmokeTests.cs
```

**Why `MainPCDoctor.Core` is `net8.0` and not `net8.0-windows`:**
- Diagnosis rules operate on abstract model types, not Windows API types.
- Rule unit tests run on any platform without a Windows environment.
- This has real testability value; it is not hypothetical Linux support.

**Why background components live in `MainPCDoctor.Desktop`:**
- They are a hosted service inside the desktop process.
- There is no independent consumer of this assembly.
- A separate DLL adds build overhead with no architectural benefit in a single-process V1 app.

---

## Data Flow: Normal Sampling Cycle

```
[SamplingScheduler fires (10s, Balanced)]
        │
        ▼
WindowsSystemMetricsCollector.CollectAsync()
  ├── CpuCollector     → CPU %, per-core %, clock (WMI), temp (LHM if available)
  ├── MemoryCollector  → available, total, commit charge, pagefile proxy
  ├── DiskCollector    → per-drive utilization, latency, queue
  ├── GpuCollector     → GPU %, VRAM used/total (DXGI), GPU temp (LHM if available)
  └── ProcessCollector → top 5 by CPU + top 5 by RAM  [called every 30s only]
        │
        ▼
SystemSnapshot assembled
        │
        ├── RollingMetricsBuffer.Add(snapshot)        [in-memory]
        │
        ├── DiagnosisEngine.Evaluate(recentWindow)    [in-memory rules]
        │      └── IncidentTracker.Update(result)
        │              ├── No change → nothing
        │              ├── RAM incident confirmed → SQLiteIncidentStore.Save()
        │              │                           → Notify (Level 2)
        │              ├── CPU/Disk/GPU/VRAM/Thermal incident → SQLiteIncidentStore.Save()
        │              │                                        → SILENT (no toast)
        │              └── [every 30 days] UpgradeRecommendationEngine.Evaluate()
        │                       └── CPU or RAM score ≥ 35, ≥ 14 days → Notify (Level 4)
        │
        └── [every 60s] MetricsAggregator.Flush()
               └── SQLiteMetricsStore.WriteMinuteAggregate()
```

---

## Process Model

One user-session Windows process containing:
- WPF Application (hidden main window, shown on demand)
- WinForms interop for `NotifyIcon` (system tray)
- `IHostedService` (MonitoringWorker) via Generic Host

**No Windows Service. No second process. No IPC.**

Startup: HKCU Run key → `MainPCDoctor.exe --background`

---

## Deployment Layout

```
%LOCALAPPDATA%\MainPCDoctor\
├── MainPCDoctor.exe
├── MainPCDoctor.Core.dll
├── MainPCDoctor.Storage.dll
├── MainPCDoctor.Platform.Windows.dll
├── data\
│   └── metrics.db
├── logs\
│   └── mainpcdoctor.log
└── config\
    └── settings.json
```

---

## Application Lifetime Model

WPF owns the process lifetime. The Generic Host runs background services. Neither controls the other — they are connected by the entry point.

```
[STAThread] Main()
  │
  ├── host = Host.CreateDefaultBuilder().Build()
  ├── host.Start()                    ← starts MonitoringWorker (background thread)
  ├── new App(host.Services).Run()    ← WPF event loop on this STA thread (blocks)
  ├── host.StopAsync(10s timeout)     ← stops MonitoringWorker, flushes data
  └── host.Dispose()
```

**No third-party WPF hosting package is required.** `Microsoft.Extensions.Hosting` is sufficient. `UseWpfLifetime()` is **not** a standard Microsoft API — it belongs to third-party packages (`Dapplo.Microsoft.Extensions.Hosting.Wpf`, etc.) and must not be added without a concrete V1 requirement.

## DI Registration

```csharp
// Program.cs — STA thread; no async Main
[STAThread]
static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(services =>
        {
            services.AddSingleton<ISystemMetricsCollector, WindowsSystemMetricsCollector>();
            services.AddSingleton<IIncidentStore, SQLiteIncidentStore>();
            services.AddSingleton<IDiagnosisEngine, DiagnosisEngine>();
            services.AddSingleton<INotificationService, WindowsNotificationService>();
            services.AddSingleton<RollingMetricsBuffer>();
            services.AddSingleton<MetricsAggregator>();
            services.AddSingleton<IncidentTracker>();
            services.AddSingleton<UpgradeRecommendationEngine>();
            services.AddSingleton<SamplingScheduler>();
            services.AddSingleton<ResourceGovernor>();
            services.AddSingleton<StartupRegistrar>();
            services.AddSingleton<TrayController>();
            services.AddHostedService<MonitoringWorker>();
        })
        .Build();

    host.Start();                                          // start MonitoringWorker

    var wpfApp = new App(host.Services);
    wpfApp.Run();                                          // blocks; WPF event loop on STA thread

    host.StopAsync(TimeSpan.FromSeconds(10))               // flush + stop gracefully
        .GetAwaiter().GetResult();
    host.Dispose();
}
```

**`host.Start()` is synchronous** (vs `StartAsync`). It starts all `IHostedService` instances including `MonitoringWorker`. The monitoring loop is running on a background thread before the WPF app starts.

**`Application.Run()` blocks on the STA thread** until `Application.Current.Shutdown()` is called. The process does not exit until that call is made.

**`host.StopAsync(10s)` is time-bounded.** If background services do not stop within 10 seconds, the host forces termination. The SQLite WAL flush must complete within this window.
