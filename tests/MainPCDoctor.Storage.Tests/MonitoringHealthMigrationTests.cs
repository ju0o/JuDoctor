using Microsoft.Data.Sqlite;
using MainPCDoctor.Storage;
using Xunit;

namespace MainPCDoctor.Storage.Tests;

public class MonitoringHealthMigrationTests
{
    [Fact]
    public async Task UpgradePreservesV1RowsAndPersistsMinimumHealth_Idempotently()
    {
        string path = Path.Combine(Path.GetTempPath(), $"health-{Guid.NewGuid():N}.db");
        try
        {
            using (var conn = new SqliteConnection($"Data Source={path}"))
            {
                conn.Open();
                using var command = conn.CreateCommand();
                using var stream = typeof(DatabaseFactory).Assembly.GetManifestResourceStream(
                    "MainPCDoctor.Storage.Migrations.001_InitialSchema.sql")!;
                command.CommandText = new StreamReader(stream).ReadToEnd();
                command.ExecuteNonQuery();
                command.CommandText = """
                    INSERT INTO schema_migrations VALUES(1, '2026-09-18');
                    INSERT INTO metrics_samples_1min(sampled_at,cpu_avg_pct,cpu_max_pct,mem_available_gb_avg,
                    mem_available_gb_min,mem_commit_ratio_avg,mem_pagefile_pressure)
                    VALUES('2026-09-18',10,20,8,7,0.5,0);
                    """;
                command.ExecuteNonQuery();
            }
            var db = new DatabaseFactory(path);
            db.EnsureInitialized();
            new DatabaseFactory(path).EnsureInitialized();
            var at = DateTimeOffset.UtcNow;
            await new SQLiteMetricsStore(db).WriteMinuteAggregateAsync(new MinuteAggregate(
                at, 12, 20, 8, 7, .5f, false, null, null, null, null, null,
                at, 450L * 1_048_576, "throttled", 16));
            using var upgraded = db.Open();
            using var query = upgraded.CreateCommand();
            query.CommandText = "SELECT COUNT(*) FROM metrics_samples_1min";
            Assert.Equal(2L, query.ExecuteScalar());
            query.CommandText = "SELECT monitoring_state FROM metrics_samples_1min ORDER BY id LIMIT 1";
            Assert.Equal("unknown", query.ExecuteScalar());
            query.CommandText = "SELECT COUNT(*) FROM metrics_samples_1min WHERE monitoring_state='throttled' AND self_private_bytes=471859200 AND heartbeat_at IS NOT NULL AND mem_total_gb=16 AND gpu_util_avg_pct IS NULL";
            Assert.Equal(1L, query.ExecuteScalar());
            query.CommandText = "PRAGMA quick_check";
            Assert.Equal("ok", query.ExecuteScalar());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (string suffix in new[] { "", "-wal", "-shm" }) File.Delete(path + suffix);
        }
    }
}
