using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Models;
using Microsoft.Extensions.Logging;

namespace MainPCDoctor.Storage;

public sealed class MetricsAggregator
{
    private readonly SQLiteMetricsStore          _store;
    private readonly RollingMetricsBuffer        _buffer;
    private readonly ILogger<MetricsAggregator>  _logger;
    private DateTimeOffset                       _lastFlush = DateTimeOffset.MinValue;
    private readonly TimeSpan                    _flushInterval = TimeSpan.FromSeconds(60);
    private readonly TimeProvider                _timeProvider;

    public MetricsAggregator(
        SQLiteMetricsStore         store,
        RollingMetricsBuffer       buffer,
        ILogger<MetricsAggregator> logger,
        TimeProvider? timeProvider = null)
    {
        _store  = store;
        _buffer = buffer;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task TryFlushAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (now - _lastFlush < _flushInterval) return;

        var window = _buffer.GetRecent(60);
        if (window.Count == 0) return;

        try
        {
            var agg = Aggregate(window, now);
            await _store.WriteMinuteAggregateAsync(agg, ct);
            _lastFlush = now;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Metrics flush failed");
        }
    }

    private static MinuteAggregate Aggregate(IReadOnlyList<SystemSnapshot> w, DateTimeOffset at)
    {
        float cpuAvg   = w.Average(s => s.Cpu.TotalUtilizationPercent);
        float cpuMax   = w.Max(s => s.Cpu.TotalUtilizationPercent);
        float memAvg   = w.Average(s => s.Memory.AvailableGb);
        float memMin   = w.Min(s => s.Memory.AvailableGb);
        float commitR  = w.Average(s => s.Memory.CommitChargeRatio);
        bool  pagefile = w.Any(s => s.Memory.PagefilePressureProxy);

        float? diskUtilAvg = w.Any(s => s.Disks.Count > 0)
            ? w.Where(s => s.Disks.Count > 0).Average(s => s.Disks.Average(d => d.UtilizationPercent))
            : null;
        float? diskLatAvg  = w.SelectMany(s => s.Disks)
                               .Where(d => d.AverageLatencyMs.HasValue)
                               .Select(d => d.AverageLatencyMs!.Value)
                               .DefaultIfEmpty(float.NaN)
                               .Average() is float la && !float.IsNaN(la) ? la : null;

        float? gpuUtil    = w.Any(s => s.Gpu != null) ? w.Where(s => s.Gpu != null).Average(s => s.Gpu!.UtilizationPercent) : null;
        float? gpuVramUsed = w.Any(s => s.Gpu != null) ? w.Where(s => s.Gpu != null).Average(s => s.Gpu!.VramUsedGb) : null;
        float? gpuVramTotal = w.LastOrDefault(s => s.Gpu?.VramTotalGb > 0)?.Gpu?.VramTotalGb;

        return new MinuteAggregate(at, cpuAvg, cpuMax, memAvg, memMin, commitR, pagefile,
            diskUtilAvg, diskLatAvg, gpuUtil, gpuVramUsed, gpuVramTotal,
            w[^1].CollectedAt, w[^1].SelfPrivateBytes, w[^1].MonitoringState,
            w[^1].Memory.TotalPhysicalGb);
    }
}
