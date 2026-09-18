using MainPCDoctor.Core.Models;
using MainPCDoctor.Storage;
using Xunit;

namespace MainPCDoctor.Storage.Tests;

public class SQLiteIncidentStoreTests : IDisposable
{
    private readonly string          _dbPath;
    private readonly DatabaseFactory _factory;
    private readonly SQLiteIncidentStore _store;

    public SQLiteIncidentStoreTests()
    {
        _dbPath  = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.db");
        _factory = new DatabaseFactory(_dbPath);
        _factory.EnsureInitialized();
        _store   = new SQLiteIncidentStore(_factory);
    }

    [Fact]
    public async Task SaveAsync_ThenGetActive_ReturnsIncident()
    {
        var incident = new Incident
        {
            BottleneckType    = BottleneckType.RamPressure,
            Status            = IncidentStatus.Active,
            StartedAt         = DateTimeOffset.UtcNow,
            NotificationLevel = 2,
        };

        await _store.SaveAsync(incident, default);

        var active = await _store.GetActiveAsync();
        Assert.Contains(active, i => i.BottleneckType == BottleneckType.RamPressure);
    }

    [Fact]
    public async Task GetByDateRange_ReturnsOnlyInRange()
    {
        var old = new Incident
        {
            BottleneckType = BottleneckType.CpuBottleneck,
            Status         = IncidentStatus.Resolved,
            StartedAt      = DateTimeOffset.UtcNow.AddDays(-60),
            ResolvedAt     = DateTimeOffset.UtcNow.AddDays(-59),
        };
        await _store.SaveAsync(old, default);

        var results = await _store.GetByDateRangeAsync(
            DateTimeOffset.UtcNow.AddDays(-30), DateTimeOffset.UtcNow);

        Assert.DoesNotContain(results, i => i.BottleneckType == BottleneckType.CpuBottleneck
                                         && i.StartedAt < DateTimeOffset.UtcNow.AddDays(-31));
    }

    public void Dispose()
    {
        // Best-effort cleanup; SQLite may still hold handles briefly
        GC.Collect();
        GC.WaitForPendingFinalizers();
        foreach (var f in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
            try { if (File.Exists(f)) File.Delete(f); } catch (IOException) { }
    }
}
