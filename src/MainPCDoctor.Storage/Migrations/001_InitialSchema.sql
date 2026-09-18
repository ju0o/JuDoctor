-- Migration 001: Initial schema

CREATE TABLE IF NOT EXISTS schema_migrations (
    version     INTEGER PRIMARY KEY,
    applied_at  TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS metrics_samples_1min (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    sampled_at              TEXT NOT NULL,
    cpu_avg_pct             REAL NOT NULL,
    cpu_max_pct             REAL NOT NULL,
    mem_available_gb_avg    REAL NOT NULL,
    mem_available_gb_min    REAL NOT NULL,
    mem_commit_ratio_avg    REAL NOT NULL,
    mem_pagefile_pressure   INTEGER NOT NULL,
    disk_util_avg_pct       REAL,
    disk_latency_avg_ms     REAL,
    gpu_util_avg_pct        REAL,
    gpu_vram_used_gb_avg    REAL,
    gpu_vram_total_gb       REAL
);

CREATE INDEX IF NOT EXISTS ix_metrics_1min_sampled_at
    ON metrics_samples_1min(sampled_at);

CREATE TABLE IF NOT EXISTS incidents (
    id                   TEXT PRIMARY KEY,
    bottleneck_type      TEXT NOT NULL,
    status               TEXT NOT NULL,
    started_at           TEXT NOT NULL,
    confirmed_at         TEXT,
    resolved_at          TEXT,
    resolve_reason       TEXT,
    peak_cpu_pct         REAL,
    peak_mem_used_gb     REAL,
    peak_disk_latency_ms REAL,
    peak_gpu_util_pct    REAL,
    duration_seconds     INTEGER,
    notification_level   INTEGER NOT NULL DEFAULT 0,
    notified_at          TEXT,
    evidence_json        TEXT
);

CREATE INDEX IF NOT EXISTS ix_incidents_started_at
    ON incidents(started_at);
CREATE INDEX IF NOT EXISTS ix_incidents_bottleneck_type
    ON incidents(bottleneck_type);
CREATE INDEX IF NOT EXISTS ix_incidents_status
    ON incidents(status);

CREATE TABLE IF NOT EXISTS incident_processes (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    incident_id     TEXT NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
    process_name    TEXT NOT NULL,
    cpu_pct         REAL NOT NULL,
    memory_mb       REAL NOT NULL,
    captured_at     TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS ix_incproc_incident_id
    ON incident_processes(incident_id);

CREATE TABLE IF NOT EXISTS upgrade_recommendations (
    id                   INTEGER PRIMARY KEY AUTOINCREMENT,
    component            TEXT NOT NULL,
    confidence           TEXT NOT NULL,
    evidence_score       REAL NOT NULL,
    observation_days     INTEGER NOT NULL,
    evidence_json        TEXT NOT NULL,
    generated_at         TEXT NOT NULL,
    notification_sent_at TEXT
);

CREATE INDEX IF NOT EXISTS ix_uprec_generated_at
    ON upgrade_recommendations(generated_at);

CREATE TABLE IF NOT EXISTS settings (
    key     TEXT PRIMARY KEY,
    value   TEXT NOT NULL
);

INSERT OR IGNORE INTO settings (key, value) VALUES
    ('startup_enabled',        'true'),
    ('notification_level',     '2'),
    ('monitoring_intensity',   'balanced'),
    ('history_retention_days', '30');
