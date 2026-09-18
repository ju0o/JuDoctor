# 06 — Monitoring Model
*Revised: 2026-09-17 — Gate Review 01*

---

## Overview

The monitoring model defines what is measured, at what rate, via which Windows APIs, and how data is structured before it reaches the diagnosis engine.

**Top-level principle:** Measure only what the diagnosis rules consume. No speculative metrics.

---

## Metrics Collected

### CPU

| Metric | Windows API | Notes |
|---|---|---|
| `TotalUtilizationPercent` | PDH `\Processor(_Total)\% Processor Time` | System-wide CPU % |
| `PerCoreUtilizationPercent[]` | PDH `\Processor(N)\% Processor Time` | One entry per logical core |
| `BaseClockMhz` | WMI `Win32_Processor.MaxClockSpeed` | Rated base; read once at startup |
| `CurrentClockMhz` | WMI `Win32_Processor.CurrentClockSpeed` | Actual running frequency |
| `TemperatureCelsius` | LibreHardwareMonitor `SensorType.Temperature` | Best-effort; `null` if unavailable |
| `LogicalCoreCount` | `Environment.ProcessorCount` | Read once at startup |

**PDH query lifecycle:** CPU counters are opened once in `WindowsCpuCollector.Initialize()` and reused per sample. `PdhOpenQuery` / `PdhAddEnglishCounter` on startup; `PdhCollectQueryData` per cycle. Counters are not re-opened each cycle.

### Memory

| Metric | Windows API | Notes |
|---|---|---|
| `TotalPhysicalGb` | `GlobalMemoryStatusEx.ullTotalPhys` | Read once at startup |
| `AvailableGb` | `GlobalMemoryStatusEx.ullAvailPhys` | Per-sample |
| `UsedGb` | Total − Available | Computed |
| `CommitChargeGb` | PDH `\Memory\Committed Bytes` | Current commit |
| `CommitLimitGb` | PDH `\Memory\Commit Limit` | System commit limit |
| `CommitChargeRatio` | CommitCharge / CommitLimit | Computed; used by RamPressureRule |
| `PagefilePressureProxy` | CommitCharge > TotalPhysical | Boolean; true = paging likely |

### Disk

| Metric | Windows API | Notes |
|---|---|---|
| `UtilizationPercent` (per drive) | PDH `\PhysicalDisk(*)\% Disk Time` | 0–100 per physical drive |
| `AverageLatencyMs` (per drive) | PDH `\PhysicalDisk(*)\Avg. Disk sec/Transfer` × 1000 | Required for DiskBottleneckRule |
| `QueueDepth` (per drive) | PDH `\PhysicalDisk(*)\Current Disk Queue Length` | Integer queue depth |

**PDH query lifecycle:** All disk counters opened once at startup, sampled per cycle. Wildcard counter `PhysicalDisk(*)` returns one instance per physical drive. If a drive disappears (USB eject), the counter is gracefully dropped from results.

**If `AverageLatencyMs` is unavailable:** The DiskBottleneckRule returns `INSUFFICIENT_EVIDENCE`. No false positive. This is the expected behavior when the PDH counter is not registered.

### GPU

| Metric | Windows API | Notes |
|---|---|---|
| `UtilizationPercent` | PDH `\GPU Engine(*engtype_3D)\Utilization Percentage` | Aggregated across adapters |
| `VramUsedGb` | PDH `\GPU Process Memory(*)\Dedicated Usage` | Aggregated VRAM usage |
| `VramTotalGb` | DXGI `IDXGIAdapter.GetDesc()` → `DedicatedVideoMemory` | **Use this, not WMI** |
| `TemperatureCelsius` | LibreHardwareMonitor GPU temp sensor | Best-effort; `null` if unavailable |

**VRAM total — critical note:** `Win32_VideoController.AdapterRAM` is UINT32 and overflows on GPUs with > 4 GB VRAM (e.g. RTX 3050 6 GB returns wrong value). `DXGI_ADAPTER_DESC.DedicatedVideoMemory` is UINT64 and is correct. V1 always uses DXGI.

**PDH GPU counter availability:** GPU PDH counters are available on Windows 10 1809+ and Windows 11 with WDDM 2.5+ drivers. If absent, GPU utilization is `null` and the GpuComputeRule is disabled gracefully.

### Processes

| Metric | Source | Notes |
|---|---|---|
| `Name` | `System.Diagnostics.Process.ProcessName` | No path, no arguments |
| `CpuPercent` | Computed from `Process.TotalProcessorTime` delta | Per-process CPU % |
| `MemoryMb` | `Process.WorkingSet64 / 1_048_576` | Working set, not private bytes |

**Process collection cadence: every 30 seconds**, not every sample. Process enumeration is expensive — opening 300+ process handles on each 10-second cycle would consume measurable CPU and RAM. The 30-second cadence produces sufficient resolution for incident attribution.

**Top N:** Top 5 by CPU, Top 5 by RAM. These two lists may overlap (same process appears in both). No "top by disk" — `System.Diagnostics.Process` does not expose disk I/O properties.

**ProcessSummary model:**
```csharp
public record ProcessMetric(
    string Name,
    float  CpuPercent,
    float  MemoryMb
);

public record ProcessSummary(
    IReadOnlyList<ProcessMetric> TopByCpu,
    IReadOnlyList<ProcessMetric> TopByRam,
    DateTimeOffset CollectedAt
);
```

---

## SystemSnapshot Model

Each sample cycle produces one `SystemSnapshot`:

```csharp
public record SystemSnapshot(
    DateTimeOffset  CollectedAt,
    CpuMetrics      Cpu,
    MemoryMetrics   Memory,
    IReadOnlyList<DiskMetrics> Disks,
    GpuMetrics?     Gpu,          // null if no GPU detected
    ProcessSummary? Processes     // null if not this cycle's 30s interval
);
```

---

## Sampling Rates (3 States)

The `SamplingScheduler` manages three states. Watch Mode has been removed.

| State | Interval | Trigger |
|---|---|---|
| **Eco** | 30 seconds | User-configured "Low" intensity, or system is definitely idle (CPU < 5%, RAM < 40%) |
| **Normal (Balanced)** | 10 seconds | Default operating state |
| **Incident** | 3 seconds | An incident candidate has been detected; high-resolution data needed |

**State transitions:**
```
Eco → Normal:     On first sample where CPU > 20% OR RAM used > 60%
Normal → Incident: On first sample triggering a rule candidate
Incident → Normal: 60 seconds after all incidents resolve
Normal → Eco:     After 10 minutes of idle conditions (CPU < 5%, RAM < 40%)
```

---

## RollingMetricsBuffer

In-memory circular buffer of recent `SystemSnapshot` objects. Used by the DiagnosisEngine for time-window evaluation.

| Parameter | Value |
|---|---|
| Capacity | 600 snapshots (~5 minutes at 3s Incident, ~100 min at 10s Normal) |
| Eviction | Oldest entry dropped when full |
| Thread-safety | Lock-free (write-once, rolling pointer) |

---

## Data Lifecycle

```
SystemSnapshot
  ├── DiagnosisEngine (in-memory, every cycle)
  ├── RollingMetricsBuffer (in-memory, last 600 samples)
  └── MetricsAggregator
        └── [every 60s] flush 1-minute aggregate → metrics_samples_1min (SQLite)
              └── [30-day retention] RetentionManager purges old rows
```

**No 1-hour aggregate table in V1.** There is no consumer of hourly aggregates in V1, so the table and aggregation logic are omitted. This simplifies the schema and removes a flush codepath.

---

## Sensor Availability Model

All hardware sensors are best-effort. The app degrades gracefully when sensors are unavailable.

| Sensor | Unavailable Behavior |
|---|---|
| CPU temperature (LHM) | Field is `null`; ThermalThrottlingRule disabled; UI shows "Unavailable" |
| GPU temperature (LHM) | Field is `null`; UI shows "Unavailable" |
| GPU utilization (PDH) | Field is `null`; GpuComputeRule and VramPressureRule disabled |
| VRAM total (DXGI) | Returns 0 if no discrete GPU; VramPressureRule disabled |
| Disk latency (PDH) | Counter returns 0 or error; DiskBottleneckRule returns INSUFFICIENT_EVIDENCE |

No error dialogs, no log spam, no degraded performance from repeated failed sensor calls. Unavailable sensors are checked once at startup and skipped for all subsequent cycles.
