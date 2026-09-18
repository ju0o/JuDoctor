using Dapper;
using MainPCDoctor.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace MainPCDoctor.Storage;

public sealed class RetentionManager
{
    private readonly DatabaseFactory          _db;
    private readonly IIncidentStore           _incidentStore;
    private readonly ILogger<RetentionManager> _logger;

    public RetentionManager(
        DatabaseFactory           db,
        IIncidentStore            incidentStore,
        ILogger<RetentionManager> logger)
    {
        _db            = db;
        _incidentStore = incidentStore;
        _logger        = logger;
    }

    public async Task RunAsync(int metricsRetentionDays = 30, CancellationToken ct = default)
    {
        await RecoverOrphansAsync(ct);
        await PurgeMetricsAsync(metricsRetentionDays, ct);
        await PurgeIncidentsAsync(90, ct);
    }

    public async Task RecoverOrphansAsync(CancellationToken ct = default)
    {
        await _incidentStore.RecoverOrphansAsync(TimeSpan.FromMinutes(10), ct);
        _logger.LogDebug("Orphan recovery complete");
    }

    private async Task PurgeMetricsAsync(int days, CancellationToken ct)
    {
        await using var conn = _db.Open();
        var deleted = await conn.ExecuteAsync(
            "DELETE FROM metrics_samples_1min WHERE sampled_at < datetime('now', @Days)",
            new { Days = $"-{days} days" });
        if (deleted > 0) _logger.LogInformation("Purged {N} old metric rows", deleted);
    }

    private async Task PurgeIncidentsAsync(int days, CancellationToken ct)
    {
        await using var conn = _db.Open();
        var deleted = await conn.ExecuteAsync(
            "DELETE FROM incidents WHERE started_at < datetime('now', @Days)",
            new { Days = $"-{days} days" });
        if (deleted > 0) _logger.LogInformation("Purged {N} old incident rows", deleted);
    }
}
