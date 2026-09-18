using MainPCDoctor.Storage.Migrations;
using Microsoft.Data.Sqlite;

namespace MainPCDoctor.Storage;

public sealed class DatabaseFactory
{
    private readonly string _dbPath;
    private readonly string _connectionString;
    private bool _initialized;

    public DatabaseFactory(string dbPath)
    {
        _dbPath           = dbPath;
        _connectionString = $"Data Source={dbPath};";
    }

    public string ConnectionString => _connectionString;

    public void EnsureInitialized()
    {
        if (_initialized) return;

        var dir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        new MigrationRunner(_connectionString).Apply();
        _initialized = true;
    }

    public SqliteConnection Open()
    {
        EnsureInitialized();
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
