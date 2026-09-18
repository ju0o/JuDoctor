ALTER TABLE metrics_samples_1min ADD COLUMN heartbeat_at TEXT;
ALTER TABLE metrics_samples_1min ADD COLUMN self_private_bytes INTEGER;
ALTER TABLE metrics_samples_1min ADD COLUMN monitoring_state TEXT NOT NULL DEFAULT 'unknown';
ALTER TABLE metrics_samples_1min ADD COLUMN mem_total_gb REAL;
