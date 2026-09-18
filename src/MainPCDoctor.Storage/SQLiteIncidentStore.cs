using Dapper;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Storage;

public sealed class SQLiteIncidentStore : IIncidentStore
{
    private readonly DatabaseFactory _db;

    public SQLiteIncidentStore(DatabaseFactory db)
    {
        _db = db;
    }

    public async Task SaveAsync(Incident incident, CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        await conn.ExecuteAsync("""
            INSERT INTO incidents
              (id, bottleneck_type, status, started_at, confirmed_at, resolved_at,
               resolve_reason, peak_cpu_pct, peak_mem_used_gb, peak_disk_latency_ms,
               peak_gpu_util_pct, duration_seconds, notification_level, notified_at, evidence_json)
            VALUES
              (@Id, @BottleneckType, @Status, @StartedAt, @ConfirmedAt, @ResolvedAt,
               @ResolveReason, @PeakCpuPercent, @PeakMemUsedGb, @PeakDiskLatencyMs,
               @PeakGpuUtilPct, @DurationSeconds, @NotificationLevel, @NotifiedAt, @EvidenceJson)
            """, ToParams(incident));
    }

    public async Task UpdateAsync(Incident incident, CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        await conn.ExecuteAsync("""
            UPDATE incidents SET
              status = @Status,
              confirmed_at = @ConfirmedAt,
              resolved_at = @ResolvedAt,
              resolve_reason = @ResolveReason,
              peak_cpu_pct = @PeakCpuPercent,
              peak_mem_used_gb = @PeakMemUsedGb,
              peak_disk_latency_ms = @PeakDiskLatencyMs,
              peak_gpu_util_pct = @PeakGpuUtilPct,
              duration_seconds = @DurationSeconds,
              notified_at = @NotifiedAt,
              evidence_json = @EvidenceJson
            WHERE id = @Id
            """, ToParams(incident));
    }

    public async Task<IReadOnlyList<Incident>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        var rows = await conn.QueryAsync<IncidentRow>("SELECT * FROM incidents WHERE status = 'Active'");
        return rows.Select(ToIncident).ToList();
    }

    public async Task<IReadOnlyList<Incident>> GetByDateRangeAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        var rows = await conn.QueryAsync<IncidentRow>(
            "SELECT * FROM incidents WHERE started_at >= @From AND started_at <= @To ORDER BY started_at DESC",
            new { From = from.ToString("O"), To = to.ToString("O") });
        return rows.Select(ToIncident).ToList();
    }

    public async Task<Incident?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        await using var conn = _db.Open();
        var row = await conn.QuerySingleOrDefaultAsync<IncidentRow>(
            "SELECT * FROM incidents WHERE id = @Id", new { Id = id });
        return row is null ? null : ToIncident(row);
    }

    public async Task RecoverOrphansAsync(TimeSpan maxActiveAge, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow - maxActiveAge;
        await using var conn = _db.Open();
        await conn.ExecuteAsync("""
            UPDATE incidents SET
              status = 'Resolved',
              resolved_at = @Now,
              resolve_reason = 'OrphanClosed'
            WHERE status = 'Active' AND started_at < @Cutoff
            """,
            new { Now = DateTimeOffset.UtcNow.ToString("O"), Cutoff = cutoff.ToString("O") });
    }

    private static object ToParams(Incident i) => new
    {
        i.Id,
        BottleneckType    = i.BottleneckType.ToString(),
        Status            = i.Status.ToString(),
        StartedAt         = i.StartedAt.ToString("O"),
        ConfirmedAt       = i.ConfirmedAt?.ToString("O"),
        ResolvedAt        = i.ResolvedAt?.ToString("O"),
        i.ResolveReason,
        i.PeakCpuPercent,
        i.PeakMemUsedGb,
        i.PeakDiskLatencyMs,
        i.PeakGpuUtilPct,
        i.DurationSeconds,
        i.NotificationLevel,
        NotifiedAt        = i.NotifiedAt?.ToString("O"),
        i.EvidenceJson
    };

    private static Incident ToIncident(IncidentRow r) => new()
    {
        Id                = r.id,
        BottleneckType    = Enum.Parse<BottleneckType>(r.bottleneck_type),
        Status            = Enum.Parse<IncidentStatus>(r.status),
        StartedAt         = DateTimeOffset.Parse(r.started_at),
        ConfirmedAt       = r.confirmed_at is null ? null : DateTimeOffset.Parse(r.confirmed_at),
        ResolvedAt        = r.resolved_at is null  ? null : DateTimeOffset.Parse(r.resolved_at),
        ResolveReason     = r.resolve_reason,
        PeakCpuPercent    = (float?)r.peak_cpu_pct,
        PeakMemUsedGb     = (float?)r.peak_mem_used_gb,
        PeakDiskLatencyMs = (float?)r.peak_disk_latency_ms,
        PeakGpuUtilPct    = (float?)r.peak_gpu_util_pct,
        DurationSeconds   = r.duration_seconds,
        NotificationLevel = r.notification_level,
        NotifiedAt        = r.notified_at is null ? null : DateTimeOffset.Parse(r.notified_at),
        EvidenceJson      = r.evidence_json
    };

    private sealed class IncidentRow
    {
        public string  id                { get; set; } = "";
        public string  bottleneck_type   { get; set; } = "";
        public string  status            { get; set; } = "";
        public string  started_at        { get; set; } = "";
        public string? confirmed_at      { get; set; }
        public string? resolved_at       { get; set; }
        public string? resolve_reason    { get; set; }
        public double? peak_cpu_pct      { get; set; }
        public double? peak_mem_used_gb  { get; set; }
        public double? peak_disk_latency_ms { get; set; }
        public double? peak_gpu_util_pct { get; set; }
        public int?    duration_seconds  { get; set; }
        public int     notification_level{ get; set; }
        public string? notified_at       { get; set; }
        public string? evidence_json     { get; set; }
    }
}
