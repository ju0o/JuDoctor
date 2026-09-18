using System.Reflection;
using Dapper;
using Microsoft.Data.Sqlite;

namespace MainPCDoctor.Storage.Migrations;

public sealed class MigrationRunner
{
    private readonly string _connectionString;

    public MigrationRunner(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void Apply()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // WAL mode and foreign keys
        conn.Execute("PRAGMA journal_mode = WAL;");
        conn.Execute("PRAGMA synchronous = NORMAL;");
        conn.Execute("PRAGMA foreign_keys = ON;");

        // Ensure migrations table exists
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS schema_migrations (
                version    INTEGER PRIMARY KEY,
                applied_at TEXT NOT NULL
            );
            """);

        var applied = conn.Query<int>("SELECT version FROM schema_migrations").ToHashSet();

        foreach (var migration in GetMigrations().Where(m => !applied.Contains(m.Version)))
        {
            using var transaction = conn.BeginTransaction();
            conn.Execute(migration.Sql, transaction: transaction);
            conn.Execute("INSERT INTO schema_migrations (version, applied_at) VALUES (@v, @at)",
                new { v = migration.Version, at = DateTimeOffset.UtcNow.ToString("O") }, transaction);
            transaction.Commit();
        }
    }

    private static IEnumerable<(int Version, string Sql)> GetMigrations()
    {
        var asm  = Assembly.GetExecutingAssembly();
        var ns   = "MainPCDoctor.Storage.Migrations";

        return new[]
        {
            (1, ReadEmbeddedSql(asm, $"{ns}.001_InitialSchema.sql")),
            (2, ReadEmbeddedSql(asm, $"{ns}.002_MonitoringHealth.sql"))
        };
    }

    private static string ReadEmbeddedSql(Assembly asm, string resourceName)
    {
        using var stream = asm.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
