using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Diagnosis;
using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Incidents;
using MainPCDoctor.Core.Models;
using MainPCDoctor.Core.Recommendations;
using MainPCDoctor.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MainPCDoctor.Desktop.Background;

public sealed class MonitoringWorker : BackgroundService
{
    private readonly ISystemMetricsCollector     _collector;
    private readonly IDiagnosisEngine            _diagnosis;
    private readonly RollingMetricsBuffer        _buffer;
    private readonly IncidentTracker             _tracker;
    private readonly MetricsAggregator           _aggregator;
    private readonly RetentionManager            _retention;
    private readonly IIncidentStore              _incidentStore;
    private readonly UpgradeRecommendationEngine _upgradeEngine;
    private readonly INotificationService        _notifications;
    private readonly UpgradeNotificationState    _upgradeState;
    private readonly ILogger<MonitoringWorker>   _logger;

    private SamplingState _state    = SamplingState.Normal;
    private bool          _ramWired;
    internal Func<GovernorReading> ReadGovernor { get; init; } = new ResourceGovernor().Read;
    internal Func<TimeSpan, CancellationToken, Task> DelayAsync { get; init; }
        = (interval, ct) => Task.Delay(interval, ct);

    public MonitoringWorker(
        ISystemMetricsCollector     collector,
        IDiagnosisEngine            diagnosis,
        RollingMetricsBuffer        buffer,
        IncidentTracker             tracker,
        MetricsAggregator           aggregator,
        RetentionManager            retention,
        IIncidentStore              incidentStore,
        UpgradeRecommendationEngine upgradeEngine,
        INotificationService        notifications,
        UpgradeNotificationState    upgradeState,
        ILogger<MonitoringWorker>   logger)
    {
        _collector     = collector;
        _diagnosis     = diagnosis;
        _buffer        = buffer;
        _tracker       = tracker;
        _aggregator    = aggregator;
        _retention     = retention;
        _incidentStore = incidentStore;
        _upgradeEngine = upgradeEngine;
        _notifications = notifications;
        _upgradeState  = upgradeState;
        _logger        = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _collector.Initialize();
        _logger.LogInformation("MonitoringWorker started");

        // Recover orphaned incidents from a previous unclean shutdown
        await _retention.RecoverOrphansAsync(stoppingToken).ConfigureAwait(false);

        // Schedule daily retention and upgrade check once at midnight
        using var dailyCancellation = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        var retentionTask = RunDailyRetentionAsync(dailyCancellation.Token);
        var upgradeTask   = RunDailyUpgradeCheckAsync(dailyCancellation.Token);

        string previousHealth = "normal";
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var governor = ReadGovernor();
                bool underPressure = governor.Throttled;
                string health = governor.ReadFailed ? "governor_read_failed"
                    : underPressure ? "throttled" : "normal";
                if (health != previousHealth)
                {
                    if (governor.ReadFailed)
                        _logger.LogWarning("ResourceGovernor memory read failed; minimum monitoring remains active at 60 seconds");
                    else if (underPressure)
                        _logger.LogWarning("ResourceGovernor reducing sampling to 60 seconds; monitoring remains active");
                    else
                        _logger.LogInformation("ResourceGovernor recovered; resuming normal sampling");
                    previousHealth = health;
                }
                var interval = underPressure
                    ? ResourceGovernor.PressureInterval
                    : SamplingScheduler.GetInterval(_state);
                await DelayAsync(interval, stoppingToken).ConfigureAwait(false);
                // Persist a fresh self-health reading after the wait.
                governor = ReadGovernor();

                try
                {
                    var snapshot = governor.Throttled
                        ? await _collector.CollectMinimumAsync(stoppingToken).ConfigureAwait(false)
                        : await _collector.CollectAsync(stoppingToken).ConfigureAwait(false);
                    snapshot = snapshot with
                    {
                        SelfPrivateBytes = governor.PrivateBytes,
                        MonitoringState = governor.ReadFailed ? "governor_read_failed"
                            : governor.Throttled ? "throttled" : "normal"
                    };
                    _buffer.Add(snapshot);
                    await _aggregator.TryFlushAsync(stoppingToken).ConfigureAwait(false);

                    // Missing detail is not evidence that an incident resolved.
                    // Minimum health is persisted above; full diagnosis is deferred.
                    if (governor.Throttled) continue;

                    // Wire total RAM into diagnosis engine on first real snapshot
                    if (!_ramWired && snapshot.Memory.TotalPhysicalGb > 0)
                    {
                        if (_diagnosis is DiagnosisEngine de)
                            de.SetTotalPhysicalRam(snapshot.Memory.TotalPhysicalGb);
                        _ramWired = true;
                    }

                    var window = _buffer.GetRecent(seconds: 600).Reverse()
                        .TakeWhile(s => s.MonitoringState == "normal").Reverse().ToArray();
                    var result = _diagnosis.Evaluate(window);
                    await _tracker.UpdateAsync(result, stoppingToken).ConfigureAwait(false);

                    _state = SelectNextState(result);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Monitoring cycle error");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            dailyCancellation.Cancel();
            await Task.WhenAll(retentionTask, upgradeTask).ConfigureAwait(false);
            _logger.LogInformation("MonitoringWorker stopped");
        }
    }

    private async Task RunDailyRetentionAsync(CancellationToken ct)
    {
        try
        {
            // Wait until tomorrow midnight, then run and repeat
            while (!ct.IsCancellationRequested)
            {
                var nextMidnight = DateTime.Today.AddDays(1);
                var delay = nextMidnight - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromHours(1);
                await Task.Delay(delay, ct).ConfigureAwait(false);
                await _retention.RunAsync(ct: ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunDailyUpgradeCheckAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                // Run immediately on startup (or resume) if today's check has not yet been done.
                // This handles the case where the PC was off or asleep at the scheduled boundary.
                if (!_upgradeState.WasCheckedToday())
                {
                    _upgradeState.RecordCheck(); // mark before running so a crash-restart doesn't repeat
                    await CheckAndNotifyUpgradeAsync(ct).ConfigureAwait(false);
                }

                // Wait until the next local-calendar-day boundary (midnight local time).
                var delay = DateTime.Today.AddDays(1) - DateTime.Now;
                if (delay < TimeSpan.Zero) delay = TimeSpan.FromMinutes(1);
                await Task.Delay(delay, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
    }

    internal async Task CheckAndNotifyUpgradeAsync(CancellationToken ct = default)
    {
        try
        {
            var windowStart = DateTimeOffset.UtcNow.AddDays(-90);
            var incidents   = await _incidentStore.GetByDateRangeAsync(windowStart, DateTimeOffset.UtcNow, ct)
                .ConfigureAwait(false);

            var cpuRec = _upgradeEngine.EvaluateCpu(incidents, windowStart);
            if (cpuRec != null && _upgradeState.ShouldNotify(cpuRec.Component, cpuRec.Confidence))
            {
                _notifications.NotifyLevel4Upgrade(cpuRec.Component, cpuRec.Confidence.ToString());
                _upgradeState.RecordNotification(cpuRec.Component, cpuRec.Confidence);
            }

            var ramRec = _upgradeEngine.EvaluateRam(incidents, windowStart);
            if (ramRec != null && _upgradeState.ShouldNotify(ramRec.Component, ramRec.Confidence))
            {
                _notifications.NotifyLevel4Upgrade(ramRec.Component, ramRec.Confidence.ToString());
                _upgradeState.RecordNotification(ramRec.Component, ramRec.Confidence);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Daily upgrade check failed");
        }
    }

    private static SamplingState SelectNextState(DiagnosisResult result)
    {
        if (result.Primary?.Outcome == DiagnosisOutcome.Confirmed)
            return SamplingState.Incident;
        if (result.Primary?.Outcome == DiagnosisOutcome.Candidate)
            return SamplingState.Normal;
        return SamplingState.Eco;
    }
}
