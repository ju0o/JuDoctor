# 07 — Diagnosis Rules
*Revised: 2026-09-17 — Gate Review 01*

---

## Purpose

Rules translate raw metrics into structured findings. Each rule is independent. The DiagnosisEngine runs all rules and applies priority/disambiguation when multiple fire.

**Upgrade recommendation categories in V1: CPU and RAM only.**
GPU, VRAM, Disk, and Thermal rules are diagnostic and incident-creating — they never generate upgrade recommendations.

---

## Global Definitions

```
EvidenceWindow   = last 300s of SystemSnapshots from RollingMetricsBuffer
IncidentGate     = component-specific minimum confirmed seconds (see each rule)
FreeRamThreshold = max(2.0 GB, totalPhysicalRam * 0.05)   -- capacity-aware
```

---

## Rule 01 — RAM Pressure

**Purpose:** Detect when the system is genuinely starved of physical memory.

### Condition (ALL of the following):

| Signal | Threshold | Notes |
|---|---|---|
| `MemoryMetrics.AvailableGb` | < `FreeRamThreshold` | Capacity-aware threshold |
| `MemoryMetrics.CommitChargeRatio` | > 0.80 | Commit > 80% of limit |
| Pagefile proxy (commit > physical) | True | Indicates active paging pressure |

*At least 2 of 3 signals must fire before confirmation starts.*

### Confirmation:
- Minimum 120 seconds of sustained multi-signal pressure
- Samples below threshold reset the candidate countdown

### Output:
```
BottleneckType   = RamPressure
NotificationLevel = 2 (warning)
UpgradeCategory  = RAM    ← V1 upgrade recommendation enabled
```

### Upgrade Recommendation Conditions:
Evidence score ≥ 35 over ≥ 14 days: recommend RAM upgrade (see doc 09).

### V1 False-Positive Guards:
- A single RAM sample at 92% does not confirm. Sustained multi-signal required.
- Systems with 32 GB RAM have a higher absolute threshold (1.6 GB free vs 2.0 GB cap).
- Systems with 4 GB RAM have threshold = 2.0 GB (cap prevents over-sensitivity).

---

## Rule 02 — CPU Bottleneck

**Purpose:** Detect when the CPU is the sustained limiting factor.

### Condition (ALL of the following):

| Signal | Threshold | Notes |
|---|---|---|
| `CpuMetrics.TotalUtilizationPercent` | ≥ 85% | System-wide |
| Affected cores | ≥ 50% of core count at > 90% | Multi-core signal |
| `MemoryMetrics.AvailableGb` | > `FreeRamThreshold` | RAM not simultaneously starved |
| `DiskMetrics.MaxQueueDepth` | < 5 | Disk not simultaneously bottlenecked |

*Requires all 4 signals simultaneously.*

### Confirmation:
- Minimum **300 seconds** of sustained conditions

**Rationale for 300s gate:** A 40-second `dotnet build` or `msbuild` easily saturates all cores. Legitimate CPU capacity issues persist for 5+ minutes. 60 seconds was too short and would trigger for every developer build.

### Output:
```
BottleneckType    = CpuBottleneck
NotificationLevel = 0 (silent)   ← No immediate warning toast in V1
UpgradeCategory   = CPU          ← V1 upgrade recommendation enabled
```

### Upgrade Recommendation Conditions:
Evidence score ≥ 35 over ≥ 14 days: recommend CPU upgrade (see doc 09).

### V1 False-Positive Guards:
- CPU at 100% during a build = silent. Confirm 300 seconds of ALL conditions.
- Disk I/O guard prevents mislabeling disk thrash as CPU bottleneck.
- RAM guard prevents mislabeling RAM starvation as CPU bottleneck.
- No Level 2 warning toast. CPU incidents accumulate toward upgrade recommendation only.

---

## Rule 03 — Disk Bottleneck

**Purpose:** Detect when disk I/O is the limiting factor for the workload.

### Condition (ALL of the following):

| Signal | Threshold | Notes |
|---|---|---|
| `DiskMetrics.UtilizationPercent` (max drive) | ≥ 90% | `% Disk Time` from PDH |
| `DiskMetrics.AverageLatencyMs` (max drive) | ≥ 50 ms | **Required latency evidence** |
| `DiskMetrics.QueueDepth` | ≥ 5 | Queue buildup |

**If latency data is unavailable:** Rule returns `INSUFFICIENT_EVIDENCE`. No incident created. This prevents false positives on systems where `% Disk Time` is high on a fast NVMe with sub-ms latency but reporting artifacts.

### Confirmation:
- Minimum 90 seconds

### Output:
```
BottleneckType    = DiskBottleneck
NotificationLevel = 0 (silent)    ← Diagnostic only
UpgradeCategory   = None          ← No upgrade recommendation in V1
```

### Disk in V1:
- Disk incidents record to history and appear in "Why Slow?" but do not drive notifications or upgrade recommendations.
- Latency data from `\PhysicalDisk(*)\Avg. Disk sec/Transfer` PDH counter.

---

## Rule 04 — VRAM Pressure

**Purpose:** Detect when GPU VRAM is exhausted and affecting rendering.

### Condition (ALL of the following):

| Signal | Threshold | Notes |
|---|---|---|
| `GpuMetrics.VramUsedGb / VramTotalGb` | ≥ 0.92 | 92% of VRAM capacity |
| `GpuMetrics.VramTotalGb` | Obtained from **DXGI** `DedicatedVideoMemory` | Accurate for GPUs > 4 GB |

**VRAM total source:** `IDXGIAdapter.GetDesc()` → `DedicatedVideoMemory` (UINT64). **Never `Win32_VideoController.AdapterRAM`** — that field overflows on GPUs > 4 GB (UINT32 wrapping).

### Confirmation:
- Minimum **180 seconds** of sustained VRAM pressure

**Rationale for 180s gate:** Games load levels in bursts. VRAM spikes during level transitions are transient. 180 seconds eliminates these false positives.

### Output:
```
BottleneckType    = VramPressure
NotificationLevel = 0 (silent)    ← Diagnostic only; no user-facing alert
UpgradeCategory   = None          ← No upgrade recommendation in V1
```

---

## Rule 05 — GPU Compute Saturation

**Purpose:** Detect when GPU compute capacity is the limiting factor.

### Condition:

| Signal | Threshold | Notes |
|---|---|---|
| `GpuMetrics.UtilizationPercent` | ≥ 90% | From PDH `\GPU Engine(*engtype_3D)\Utilization Percentage` |
| Duration | ≥ 120 seconds | Sustained |

### Output:
```
BottleneckType    = GpuCompute
NotificationLevel = 0 (silent)    ← ALWAYS silent; no toast, no pattern notice
UpgradeCategory   = None          ← No upgrade recommendation in V1
```

**Why always silent:** GPU at 99% during gaming is a sign of a well-optimized game, not a problem. A toast notification at that moment would be disruptive and wrong. GPU compute incidents contribute to "Why Slow?" analysis only.

---

## Rule 06 — Thermal Throttling

**Purpose:** Detect if the CPU or GPU is being thermally throttled, reducing performance.

### Condition:

| Signal | Threshold | Notes |
|---|---|---|
| `CpuMetrics.TemperatureCelsius` | > 90°C | Via LibreHardwareMonitor (best-effort) |
| `CpuMetrics.CurrentClockMhz` | < `CpuMetrics.BaseClockMhz * 0.90` | 10%+ clock drop |

Both signals required. If temperature is unavailable (sensor not supported): rule is **disabled gracefully** — no incident, no error, no log spam.

### Confirmation:
- Minimum 60 seconds

### Output:
```
BottleneckType    = ThermalThrottling
NotificationLevel = 0 (silent)    ← Diagnostic only
UpgradeCategory   = None          ← No upgrade recommendation in V1
```

---

## Rule 07 — Process Anomaly

**Purpose:** Detect when a single process is causing resource problems.

### Condition (AND — both required):

| Signal | Threshold | Notes |
|---|---|---|
| `ProcessMetric.CpuPercent` OR `ProcessMetric.MemoryMb` | CPU > 40% OR Memory > 500 MB | **Resource is abnormal** |
| Growth pattern sustained | Resource increasing or flat-high over 120 seconds | **Pattern is problematic** |

**AND logic:** Both conditions must be true. A legitimate CPU-intensive process (compiler, renderer) passes resource threshold but has no sustained pathological growth pattern — it is not flagged.

### Confirmation:
- 120 seconds of sustained AND condition

### Output:
```
BottleneckType    = ProcessAnomaly
NotificationLevel = 0 (silent)    ← Diagnostic only
UpgradeCategory   = None
ProcessName       = [name of offending process, no path/args]
```

---

## Priority and Disambiguation

When multiple rules fire simultaneously, the DiagnosisEngine applies this priority order:

```
1. ThermalThrottling     (highest — hardware safety concern)
2. RamPressure           (direct user impact, only rule with Level 2 notification)
3. CpuBottleneck
4. DiskBottleneck
5. VramPressure
6. GpuCompute
7. ProcessAnomaly        (lowest priority — contextual, not root cause)
```

**Multi-signal scenarios:**
- CPU high + RAM high → RAM wins (RAM is more likely the constraint; CPU load is a symptom)
- Disk high + CPU moderate → Disk wins if latency confirmed
- VRAM high + GPU high → VRAM wins (GPU saturation is expected when VRAM-limited)
- ProcessAnomaly always recorded as secondary finding alongside the primary

---

## Diagnosis Decision Table

| Bottleneck Type | Level | Notification | Upgrade Rec | False-Positive Guard |
|---|---|---|---|---|
| RamPressure | 2 | Warning toast | RAM (V1) | 2-of-3 signals, 120s, capacity-aware threshold |
| CpuBottleneck | 0 | Silent | CPU (V1) | 300s gate, disk + RAM headroom guards |
| DiskBottleneck | 0 | Silent | None | Latency required (INSUFFICIENT_EVIDENCE if absent) |
| VramPressure | 0 | Silent | None | 180s gate, DXGI VRAM total |
| GpuCompute | 0 | Silent | None | Always silent (gaming expected) |
| ThermalThrottling | 0 | Silent | None | Sensor-dependent, graceful disable |
| ProcessAnomaly | 0 | Silent | None | AND logic, growth pattern required |

---

## INSUFFICIENT_EVIDENCE State

Some rules can return `INSUFFICIENT_EVIDENCE` rather than a diagnosis. This is not an error — it is the engine honestly reporting that available data does not support a conclusion.

**Triggers:**
- Disk rule: no latency data available from PDH counter
- Thermal rule: no temperature sensor accessible via LibreHardwareMonitor
- Any rule: evaluation window has fewer than 30 samples (< 5 minutes of history)

**Behavior:** `INSUFFICIENT_EVIDENCE` is not stored as an incident. It appears in "Why Was My PC Slow?" as: *"Disk activity was elevated, but latency data was unavailable — analysis could not be completed."*

---

## Thresholds Record (DiagnosisThresholds)

All thresholds are configurable via `DiagnosisThresholds` record. Defaults shown.

```csharp
public record DiagnosisThresholds
{
    // RAM
    public float RamFreeThresholdMinGb          { get; init; } = 2.0f;
    public float RamFreeThresholdRatio           { get; init; } = 0.05f;
    public float RamCommitChargeRatio            { get; init; } = 0.80f;
    public int   RamPressureConfirmSeconds       { get; init; } = 120;
    public int   RamPressureMinSignals           { get; init; } = 2;

    // CPU
    public float CpuUtilizationThreshold        { get; init; } = 0.85f;
    public float CpuAffectedCoreRatio           { get; init; } = 0.50f;
    public int   CpuBottleneckConfirmSeconds    { get; init; } = 300;

    // Disk
    public float DiskUtilizationThreshold       { get; init; } = 0.90f;
    public float DiskLatencyThresholdMs         { get; init; } = 50f;
    public int   DiskQueueDepthThreshold        { get; init; } = 5;
    public int   DiskBottleneckConfirmSeconds   { get; init; } = 90;

    // VRAM
    public float VramUtilizationThreshold       { get; init; } = 0.92f;
    public int   VramPressureConfirmSeconds     { get; init; } = 180;

    // GPU
    public float GpuUtilizationThreshold        { get; init; } = 0.90f;
    public int   GpuComputeConfirmSeconds       { get; init; } = 120;

    // Thermal
    public float CpuThermalCelsius              { get; init; } = 90f;
    public float CpuClockDegradationRatio       { get; init; } = 0.10f;
    public int   ThermalConfirmSeconds          { get; init; } = 60;

    // Process
    public float ProcessCpuThreshold           { get; init; } = 0.40f;
    public float ProcessMemoryThresholdMb      { get; init; } = 500f;
    public int   ProcessAnomalyConfirmSeconds  { get; init; } = 120;
}
```
