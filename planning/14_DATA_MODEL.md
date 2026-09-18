# 14 — Data Model
*Revised: 2026-09-17 — Gate Review 01*

---

## Overview

SQLite database, WAL mode, located at `%LOCALAPPDATA%\MainPCDoctor\data\metrics.db`.

**Tables in V1:**

| Table | Purpose |
|---|---|
| `metrics_samples_1min` | 1-minute metric aggregates |
| `incidents` | Confirmed incident records |
| `incident_processes` | Top processes captured per incident |
| `upgrade_recommendations` | Recommendation records (CPU + RAM only) |
| `settings` | User configuration key-value store |
| `schema_migrations` | Migration tracking |

**Removed from V1:** `metrics_samples_1hour` — there is no consumer of hourly aggregates in V1. Removing this table eliminates a flush codepath and simplifies the schema.

---

## Schema

### `schema_migrations`

```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    version     INTEGER PRIMARY KEY,
    applied_at  TEXT NOT NULL
);
```

### `metrics_samples_1min`

One row per minute, per machine session. Stores aggregated values from 6 × 10-second samples.

```sql
CREATE TABLE IF NOT EXISTS metrics_samples_1min (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    sampled_at              TEXT NOT NULL,          -- ISO 8601 UTC

    -- CPU
    cpu_avg_pct             REAL NOT NULL,
    cpu_max_pct             REAL NOT NULL,

    -- Memory
    mem_available_gb_avg    REAL NOT NULL,
    mem_available_gb_min    REAL NOT NULL,
    mem_commit_ratio_avg    REAL NOT NULL,
    mem_pagefile_pressure   INTEGER NOT NULL,       -- 0/1 boolean

    -- Disk (primary drive only in aggregates)
    disk_util_avg_pct       REAL,                   -- NULL if unavailable
    disk_latency_avg_ms     REAL,                   -- NULL if unavailable

    -- GPU
    gpu_util_avg_pct        REAL,                   -- NULL if no GPU
    gpu_vram_used_gb_avg    REAL,                   -- NULL if no GPU
    gpu_vram_total_gb       REAL                    -- NULL if no GPU; from DXGI
);

CREATE INDEX IF NOT EXISTS ix_metrics_1min_sampled_at
    ON metrics_samples_1min(sampled_at);
```

### `incidents`

```sql
CREATE TABLE IF NOT EXISTS incidents (
    id                  TEXT PRIMARY KEY,            -- GUID
    bottleneck_type     TEXT NOT NULL,               -- 'RamPressure', 'CpuBottleneck', etc.
    status              TEXT NOT NULL,               -- 'Active', 'Resolved'
    started_at          TEXT NOT NULL,               -- ISO 8601 UTC
    confirmed_at        TEXT,                        -- when candidate became confirmed
    resolved_at         TEXT,                        -- NULL if still active
    resolve_reason      TEXT,                        -- 'Natural', 'OrphanClosed', 'UserClosed'
    peak_cpu_pct        REAL,
    peak_mem_used_gb    REAL,
    peak_disk_latency_ms REAL,
    peak_gpu_util_pct   REAL,
    duration_seconds    INTEGER,                     -- computed on resolve
    notification_level  INTEGER NOT NULL DEFAULT 0,
    notified_at         TEXT,                        -- NULL if no notification sent
    evidence_json       TEXT                         -- JSON object: signals that confirmed the incident
);

CREATE INDEX IF NOT EXISTS ix_incidents_started_at
    ON incidents(started_at);
CREATE INDEX IF NOT EXISTS ix_incidents_bottleneck_type
    ON incidents(bottleneck_type);
CREATE INDEX IF NOT EXISTS ix_incidents_status
    ON incidents(status);
```

### `incident_processes`

Top processes captured at incident confirmation. Stored for attribution in "Why Slow?" and Incident Detail views.

```sql
CREATE TABLE IF NOT EXISTS incident_processes (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    incident_id     TEXT NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
    process_name    TEXT NOT NULL,          -- no path, no args
    cpu_pct         REAL NOT NULL,
    memory_mb       REAL NOT NULL,
    captured_at     TEXT NOT NULL           -- ISO 8601 UTC
);
-- No disk_io_kbps column: System.Diagnostics.Process does not expose disk I/O
```

### `upgrade_recommendations`

```sql
CREATE TABLE IF NOT EXISTS upgrade_recommendations (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    component           TEXT NOT NULL,          -- 'CPU' or 'RAM' only in V1
    confidence          TEXT NOT NULL,          -- 'Low', 'Medium', 'High'
    evidence_score      REAL NOT NULL,
    observation_days    INTEGER NOT NULL,
    evidence_json       TEXT NOT NULL,          -- JSON: incident count, days, sessions, etc.
    generated_at        TEXT NOT NULL,          -- ISO 8601 UTC
    notification_sent_at TEXT                   -- NULL if not yet notified
);

CREATE INDEX IF NOT EXISTS ix_uprec_generated_at
    ON upgrade_recommendations(generated_at);
```

*V1 constraint:* The application only writes rows where `component IN ('CPU', 'RAM')`. GPU, VRAM, Disk, Thermal rows are never written.

### `settings`

```sql
CREATE TABLE IF NOT EXISTS settings (
    key     TEXT PRIMARY KEY,
    value   TEXT NOT NULL
);
```

Default keys:
| Key | Default |
|---|---|
| `startup_enabled` | `"true"` |
| `notification_level` | `"2"` |
| `monitoring_intensity` | `"balanced"` |
| `history_retention_days` | `"30"` |

---

## Retention Policy

Managed by `RetentionManager`, run at app startup after orphan recovery.

| Table | Retention |
|---|---|
| `metrics_samples_1min` | Configurable (default 30 days) |
| `incidents` | 90 days |
| `incident_processes` | Cascades with parent incident |
| `upgrade_recommendations` | Indefinite (small table) |
| `settings` | Indefinite |

```sql
-- Retention purge example
DELETE FROM metrics_samples_1min
WHERE sampled_at < datetime('now', '-30 days');
```

---

## WAL Mode

Enabled once at database creation:

```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA foreign_keys = ON;
```

WAL mode allows concurrent reads during writes. The monitoring loop writes every 60 seconds; the UI reads on demand. WAL prevents the UI from stalling the monitoring loop.

---

## Migration Strategy

Migrations are numbered SQL files in `Storage/Migrations/`. `MigrationRunner` applies any unapplied migrations on startup (idempotent).

```
001_InitialSchema.sql   ← Creates all V1 tables
```

All `CREATE TABLE` statements use `CREATE TABLE IF NOT EXISTS`. Migrations are never destructive in V1.
