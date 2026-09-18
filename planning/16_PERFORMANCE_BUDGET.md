# 16 — Performance Budget
*Revised: 2026-09-17 — Gate Review 01*

---

## Governing Principle

MainPC Doctor must never appear in its own incident list. The app's resource footprint must be invisible to the user.

---

## Runtime Budget

| Metric | Target | Hard Limit |
|---|---|---|
| CPU (idle, background) | < 0.5% average over 5 min | < 1.0% |
| CPU (during sample cycle) | < 2% instantaneous peak | < 3% (ResourceGovernor threshold) |
| RAM (working set, after 10 min running) | < 80 MB | < 120 MB |
| RAM (after 24 hours running) | < 100 MB | < 150 MB |
| Disk writes (metrics flush) | 1 write per 60 seconds | — |
| Disk write size (per flush) | < 2 KB | — |
| Startup time (to tray icon visible) | < 3 seconds | — |

**ResourceGovernor enforcement:** If MainPC Doctor's own process CPU exceeds 3% during a sample cycle, the cycle is skipped. If 5+ consecutive cycles are skipped, the scheduler transitions to Eco state (30-second interval).

---

## Sampling Overhead Budget

| Operation | Max Time | Notes |
|---|---|---|
| Full sample cycle (Normal mode) | < 50 ms | All collectors combined |
| PDH query (CPU, Memory, Disk, GPU) | < 20 ms | After warm-up; first call may be slow |
| ProcessCollector (every 30s) | < 30 ms | Enumerating ~200-400 processes |
| DiagnosisEngine.Evaluate() | < 5 ms | Pure in-memory rule evaluation |
| MetricsAggregator.Flush() | < 10 ms | SQLite write (WAL mode) |

**PDH warm-up note:** The first `PdhCollectQueryData()` call after query initialization may take 100–200 ms. This is normal and expected. The monitoring loop waits for the first valid sample before beginning diagnosis evaluation. The startup time budget (< 3 seconds to tray icon) accounts for this.

---

## Process Enumeration Rate Limiting

Process enumeration (`WindowsProcessCollector`) is invoked every 30 seconds, not every sample cycle. This is enforced in `WindowsSystemMetricsCollector`:

```csharp
// Only pass ProcessCollector if 30s has elapsed since last enumeration
if (now - _lastProcessCollection >= TimeSpan.FromSeconds(30))
{
    _lastProcessCollection = now;
    // collect processes
}
```

Rationale: Opening 200–400 process handles each 10-second cycle consumes ~10–20 ms of CPU time and several MB of kernel memory churn. At 30-second cadence, the overhead drops by 3×.

---

## Developer Performance Regression Tests

These are not user-facing metrics. They are developer tests that detect if a code change has introduced a performance regression.

Run as part of the integration test suite on CI:

| Test | Assertion |
|---|---|
| `SampleCycleOverheadTest` | Full sample collection completes in < 100 ms |
| `DiagnosisEngineLatencyTest` | Rule evaluation on 600-sample buffer completes in < 10 ms |
| `MetricsFlushLatencyTest` | SQLite flush of 6 aggregate rows completes in < 20 ms |
| `MemoryLeakTest` | Process working set after 1000 sample cycles is within 5 MB of baseline |
| `ProcessCollectorOverheadTest` | ProcessCollector returns in < 50 ms on a machine with 300+ processes |

These tests run on developer machines and CI, not on end-user machines. They are not surfaced in the application UI.

---

## SQLite Size Budget

| Data | Row size (estimated) | 30-day volume |
|---|---|---|
| `metrics_samples_1min` | ~100 bytes | ~43,200 rows, ~4.3 MB |
| `incidents` | ~300 bytes | ~50 rows, ~15 KB |
| `incident_processes` | ~100 bytes | ~500 rows, ~50 KB |
| `upgrade_recommendations` | ~200 bytes | ~2 rows, ~400 bytes |

**Total database size at 30 days: approximately 5 MB.** Well within any modern disk budget.

---

## Memory Budget Breakdown

| Component | Expected RAM |
|---|---|
| WPF application framework (hidden window) | ~25 MB |
| RollingMetricsBuffer (600 snapshots) | ~5 MB |
| SQLite connection + WAL file cache | ~5 MB |
| DI container + host overhead | ~5 MB |
| LibreHardwareMonitor sensor library | ~10 MB |
| Working headroom | ~30 MB |
| **Total** | **~80 MB** |

The 120 MB hard limit provides ~40 MB safety margin over the expected footprint.
