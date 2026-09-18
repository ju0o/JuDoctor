using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Models;
using Microsoft.Extensions.Logging;

namespace MainPCDoctor.Core.Incidents;

public sealed class IncidentTracker
{
    private readonly IIncidentStore              _store;
    private readonly INotificationService        _notifications;
    private readonly ILogger<IncidentTracker>    _logger;
    private readonly Dictionary<BottleneckType, Incident> _active = new();
    private readonly TimeSpan                    _mergeWindow = TimeSpan.FromMinutes(10);

    public IncidentTracker(
        IIncidentStore           store,
        INotificationService     notifications,
        ILogger<IncidentTracker> logger)
    {
        _store         = store;
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task UpdateAsync(DiagnosisResult result, CancellationToken ct = default)
    {
        var confirmedTypes = result.Primary is { Outcome: DiagnosisOutcome.Confirmed } p
            ? new[] { p }.Concat(result.Secondary.Where(s => s.Outcome == DiagnosisOutcome.Confirmed))
            : result.Secondary.Where(s => s.Outcome == DiagnosisOutcome.Confirmed);

        var confirmedSet = confirmedTypes.Select(f => f.BottleneckType).ToHashSet();

        // Open or extend active incidents
        foreach (var finding in confirmedTypes)
        {
            if (_active.TryGetValue(finding.BottleneckType, out var existing))
            {
                // Extend (dedup within merge window)
                UpdatePeaks(existing, result.EvaluatedAt);
            }
            else
            {
                var incident = new Incident
                {
                    BottleneckType    = finding.BottleneckType,
                    Status            = IncidentStatus.Active,
                    StartedAt         = result.EvaluatedAt,
                    ConfirmedAt       = result.EvaluatedAt,
                    NotificationLevel = finding.NotificationLevel
                };

                _active[finding.BottleneckType] = incident;
                await _store.SaveAsync(incident, ct);
                _logger.LogInformation("Incident confirmed: {Type}", finding.BottleneckType);

                if (finding.NotificationLevel >= 2)
                    _notifications.NotifyLevel2Ram(incident.Id);
            }
        }

        // Resolve incidents whose condition has cleared
        var toResolve = _active.Keys.Where(t => !confirmedSet.Contains(t)).ToList();
        foreach (var type in toResolve)
        {
            var incident          = _active[type];
            incident.Status       = IncidentStatus.Resolved;
            incident.ResolvedAt   = result.EvaluatedAt;
            incident.ResolveReason = "Natural";
            incident.DurationSeconds = (int)(result.EvaluatedAt - incident.StartedAt).TotalSeconds;

            await _store.UpdateAsync(incident, ct);
            _active.Remove(type);
            _logger.LogInformation("Incident resolved: {Type} after {Dur}s", type, incident.DurationSeconds);
        }
    }

    private static void UpdatePeaks(Incident incident, DateTimeOffset at)
    {
        // Peaks are updated by MonitoringWorker from live snapshot; tracker just extends the time
    }

    public IReadOnlyCollection<Incident> ActiveIncidents => _active.Values;
}
