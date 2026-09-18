# 17 — Windows Technical Plan
*Revised: 2026-09-17 — Gate Review 01*

---

## Platform Target

| Item | Choice |
|---|---|
| Runtime | .NET 8 (LTS) |
| UI framework | WPF (`net8.0-windows`) |
| Minimum Windows version | Windows 10 21H2 (build 19044) |
| Tested on | Windows 10 21H2, Windows 11 23H2 |
| Architecture | x64 only |
| Installer | Inno Setup (no MSIX / no Store) |

---

## Project Target Frameworks

| Project | TFM |
|---|---|
| `MainPCDoctor.Core` | `net8.0` |
| `MainPCDoctor.Storage` | `net8.0` |
| `MainPCDoctor.Platform.Windows` | `net8.0-windows` |
| `MainPCDoctor.Desktop` | `net8.0-windows` |
| `Core.Tests` | `net8.0` |
| `Storage.Tests` | `net8.0` |
| `Integration.Tests` | `net8.0-windows` |

---

## Windows APIs

### CPU Metrics

| API | Use |
|---|---|
| PDH `\Processor(_Total)\% Processor Time` | Total CPU utilization |
| PDH `\Processor(N)\% Processor Time` | Per-core utilization |
| WMI `Win32_Processor.MaxClockSpeed` | Base clock (read once) |
| WMI `Win32_Processor.CurrentClockSpeed` | Current clock (per cycle or on-demand) |
| LibreHardwareMonitor `SensorType.Temperature` | CPU temperature (best-effort, graceful null) |

### Memory Metrics

| API | Use |
|---|---|
| `GlobalMemoryStatusEx` (kernel32.dll) | Available/total physical RAM per sample |
| PDH `\Memory\Committed Bytes` | Commit charge |
| PDH `\Memory\Commit Limit` | Commit limit |

### Disk Metrics

| API | Use |
|---|---|
| PDH `\PhysicalDisk(*)\% Disk Time` | Per-drive utilization |
| PDH `\PhysicalDisk(*)\Avg. Disk sec/Transfer` | Per-drive latency (required for DiskBottleneckRule) |
| PDH `\PhysicalDisk(*)\Current Disk Queue Length` | Queue depth |

**PDH query lifecycle:** All disk counters are opened once at startup (`PdhOpenQuery` / `PdhAddEnglishCounter`). `PdhCollectQueryData` is called once per sample cycle. The same query handle is reused. Counters are never re-opened per cycle.

### GPU Metrics

| API | Use |
|---|---|
| PDH `\GPU Engine(*engtype_3D)\Utilization Percentage` | GPU compute utilization |
| PDH `\GPU Process Memory(*)\Dedicated Usage` | VRAM in use |
| **DXGI `IDXGIAdapter.GetDesc()`** → `DedicatedVideoMemory` | **VRAM total capacity** |
| LibreHardwareMonitor GPU temp sensor | GPU temperature (best-effort, graceful null) |

**VRAM total — implementation note:**
```csharp
// Correct: DXGI DedicatedVideoMemory (UINT64)
using var factory = new SharpDX.DXGI.Factory1();
var adapter = factory.GetAdapter(0);
var desc = adapter.Description;
double vramTotalGb = desc.DedicatedVideoMemory / (1024.0 * 1024.0 * 1024.0);
```

```csharp
// WRONG — do not use this:
// Win32_VideoController.AdapterRAM is UINT32 and overflows for GPUs > 4 GB.
// RTX 3050 6 GB, RTX 3060 8 GB, etc. will return incorrect values.
```

### Process Metrics

| API | Use |
|---|---|
| `System.Diagnostics.Process` | Enumerate running processes |
| `Process.TotalProcessorTime` delta | Compute per-process CPU% |
| `Process.WorkingSet64` | Per-process RAM usage |

**Note:** `System.Diagnostics.Process` does not expose disk I/O properties. "Top by Disk" process ranking is not implemented in V1.

### Notifications

| API | Use |
|---|---|
| WinRT `ToastNotificationManager` | Send Windows toast notifications |
| AUMID registration | Required at install time for non-MSIX apps |
| `Windows.UI.Notifications` namespace | Toast XML template construction |

AUMID is registered in the Inno Setup installer via a COM server registration in HKCU. The notification service verifies AUMID registration at startup and logs a warning if absent.

### System Tray

| API | Use |
|---|---|
| `System.Windows.Forms.NotifyIcon` | Tray icon + context menu |
| `ContextMenuStrip` | Tray right-click menu items |

WinForms interop is used only for `NotifyIcon`. The WPF project references `System.Windows.Forms` for this purpose. The tray controller does not use WinForms for any UI rendering.

### Startup Registration

| API | Use |
|---|---|
| `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` | Auto-start at login |
| `Registry.CurrentUser.OpenSubKey(...)` | Read/write via `Microsoft.Win32.Registry` |

No elevation required. HKCU is always writable by the current user.

---

## NuGet Dependencies

| Package | Version | Use |
|---|---|---|
| `Microsoft.Extensions.Hosting` | 8.x | Generic Host, DI, IHostedService |
| `Microsoft.Extensions.Logging.Abstractions` | 8.x | Logging interfaces |
| `Serilog.Extensions.Hosting` | latest stable | Serilog integration |
| `Serilog.Sinks.File` | latest stable | Log file sink |
| `CommunityToolkit.Mvvm` | latest stable | MVVM, ObservableObject, RelayCommand |
| `Microsoft.Data.Sqlite` | 8.x | SQLite via EF-free ADO.NET |
| `Dapper` | latest stable | Lightweight SQL mapping |
| `LibreHardwareMonitorLib` | latest stable | CPU/GPU temperature sensors (best-effort) |
| `SharpDX.DXGI` | 4.x | DXGI `IDXGIAdapter.GetDesc()` for VRAM total |
| `xunit` | 2.x | Unit test framework |
| `xunit.runner.visualstudio` | 2.x | Test discovery in VS / CLI |
| `NSubstitute` | latest stable | Mocking for unit tests |

**Not included:**
- No NVAPI (LibreHardwareMonitor covers GPU temps)
- No Blazor, WebView2, or any browser embedding
- No Entity Framework Core (direct Dapper + ADO.NET)
- No SignalR, gRPC, or any network stack

---

## PDH Query Lifecycle

This is a common source of bugs and performance issues. The correct pattern:

```
App startup:
  PdhOpenQuery(null, 0, out queryHandle)
  PdhAddEnglishCounter(queryHandle, "\\Processor(_Total)\\% Processor Time", ...)
  PdhAddEnglishCounter(queryHandle, "\\PhysicalDisk(*)\\% Disk Time", ...)
  // ... all other counters
  PdhCollectQueryData(queryHandle)   // first call; establishes baseline
  Thread.Sleep(1000)                 // required delay before first meaningful sample
  // First usable sample will be available on the second PdhCollectQueryData call.

Per sample cycle:
  PdhCollectQueryData(queryHandle)   // delta since last call
  PdhGetFormattedCounterValue(counterHandle, ...)  // read each counter

App shutdown:
  PdhCloseQuery(queryHandle)
```

**Never call `PdhOpenQuery` per cycle.** Opening and closing a PDH query on every sample is expensive and causes the first sample to always return 0 (no baseline for delta counters).

---

## LibreHardwareMonitor Integration

```csharp
// Initialize once at startup
var computer = new Computer
{
    IsCpuEnabled = true,
    IsGpuEnabled = true
};
computer.Open();

// Per sample (or on-demand — LHM polls hardware on Update())
computer.Accept(new UpdateVisitor());

// Read sensor
var cpuTemp = computer.Hardware
    .FirstOrDefault(h => h.HardwareType == HardwareType.Cpu)
    ?.Sensors
    .FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Name == "Core Average")
    ?.Value;  // null if unavailable
```

LHM may require elevated privileges for some sensors on some hardware. V1 does not run elevated. If `computer.Open()` returns no temperature sensors, `TemperatureCelsius` remains `null` and the ThermalThrottlingRule is disabled for the session. No error, no retry.

---

## WPF Application Host

WPF owns the process lifetime. The Generic Host manages background services. The entry point starts the host, runs WPF, then stops the host.

```csharp
// Program.cs — STA thread; WPF requires STA
// Do NOT use UseWpfLifetime() — it is NOT a standard Microsoft API.
// Do NOT use AddWpfBlazorServices() — that method does not exist.
// Do NOT add Dapplo.Microsoft.Extensions.Hosting.Wpf or similar packages.
// Microsoft.Extensions.Hosting alone is sufficient.

[STAThread]
static void Main(string[] args)
{
    var host = Host.CreateDefaultBuilder(args)
        .ConfigureServices(...)
        .Build();

    host.Start();                                     // MonitoringWorker starts

    var wpfApp = new App(host.Services);
    wpfApp.Run();                                     // blocks until Shutdown()

    host.StopAsync(TimeSpan.FromSeconds(10))
        .GetAwaiter().GetResult();
    host.Dispose();
}
```

**Shutdown policy:**
- `App.xaml.cs` sets `ShutdownMode = ShutdownMode.OnExplicitShutdown` in `OnStartup`
- `MainWindow.OnClosing` cancels the close event and calls `Hide()` — the process stays alive
- `TrayController.OnExitClick` calls `Application.Current.Shutdown()` — the only normal exit path
- `App.OnSessionEnding` calls `Application.Current.Shutdown()` — handles Windows logoff/shutdown

**`host.StopAsync(TimeSpan.FromSeconds(10))` is time-bounded.** SQLite WAL survives abrupt termination; the 10-second limit prevents hang on stop.

---

## Installer Requirements

Inno Setup script must:
1. Install files to `%LOCALAPPDATA%\MainPCDoctor\`
2. Register AUMID for WinRT toast notifications (HKCU COM server registration)
3. Create Start Menu shortcut
4. Register HKCU Run key if "Start with Windows" checked during install
5. Uninstaller: remove all installed files, registry keys, Start Menu shortcut

No UAC elevation required for install (LOCALAPPDATA is user-writable).
