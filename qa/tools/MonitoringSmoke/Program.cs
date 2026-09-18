using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Diagnosis;
using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Incidents;
using MainPCDoctor.Core.Models;
using MainPCDoctor.Desktop.Background;
using MainPCDoctor.Platform.Windows.Collectors;
using MainPCDoctor.Storage;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

if (args.Length < 1 || args.Length > 3) throw new ArgumentException("New DB path [durationSeconds] [governor-scenario]");
var path = Path.GetFullPath(args[0]);
if (File.Exists(path)) throw new IOException("Refusing to reuse an existing database.");
int seconds = args.Length > 1 ? int.Parse(args[1]) : 80;
bool scenario = args.Length > 2 && args[2] == "governor-scenario";
using var logs = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true));
var db = new DatabaseFactory(path);
var buffer = new RollingMetricsBuffer();
var incidents = new SQLiteIncidentStore(db);
var collector = new CountingCollector(new WindowsSystemMetricsCollector(logs.CreateLogger<WindowsSystemMetricsCollector>()));
var stopwatch = Stopwatch.StartNew();
// Only this QA host substitutes the governor sensor. No allocation pressure is applied.
var governor = new ResourceGovernor(scenario ? () =>
    (stopwatch.Elapsed.TotalSeconds is >= 75 and < 260 ? 450L : 200L) * 1_048_576 : null);
using var worker = new MonitoringWorker(collector, new DiagnosisEngine(), buffer,
    new IncidentTracker(incidents, new SilentNotifications(), logs.CreateLogger<IncidentTracker>()),
    new MetricsAggregator(new SQLiteMetricsStore(db), buffer, logs.CreateLogger<MetricsAggregator>()),
    new RetentionManager(db, incidents, logs.CreateLogger<RetentionManager>()),
    logs.CreateLogger<MonitoringWorker>()) { ReadGovernor = governor.Read };
await worker.StartAsync(CancellationToken.None);
try
{
    while (stopwatch.Elapsed.TotalSeconds < seconds)
    {
        await Task.Delay(TimeSpan.FromSeconds(10));
        using var self = Process.GetCurrentProcess();
        Console.WriteLine(JsonSerializer.Serialize(new { at = DateTimeOffset.Now, elapsed = stopwatch.Elapsed.TotalSeconds,
            snapshots = buffer.Count, collector.Full, collector.Minimum, actualPrivateBytes = self.PrivateMemorySize64,
            health = buffer.GetAll().LastOrDefault()?.MonitoringState, controlledGovernor = scenario }));
        if (worker.ExecuteTask!.IsFaulted) await worker.ExecuteTask;
    }
}
finally
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    await worker.StopAsync(timeout.Token);
    collector.Dispose();
}
using var conn = db.Open();
using var command = conn.CreateCommand();
command.CommandText = "SELECT COUNT(*) FROM metrics_samples_1min";
long rows = (long)command.ExecuteScalar()!;
Console.WriteLine(JsonSerializer.Serialize(new { rows, snapshots = buffer.Count, collector.Full, collector.Minimum,
    cleanStop = worker.ExecuteTask!.IsCompletedSuccessfully, latest = buffer.GetAll().LastOrDefault() }));
if (rows < 2 || !worker.ExecuteTask.IsCompletedSuccessfully || (scenario && (collector.Minimum < 2 || collector.Full < 3))) Environment.ExitCode = 1;

sealed class SilentNotifications : INotificationService
{
    public void NotifyLevel2Ram(string incidentId) { }
    public void NotifyLevel4Upgrade(string component, string confidence) { }
}
sealed class CountingCollector(WindowsSystemMetricsCollector inner) : ISystemMetricsCollector
{
    public int Full { get; private set; }
    public int Minimum { get; private set; }
    public void Initialize() => inner.Initialize();
    public void Dispose() => inner.Dispose();
    public async Task<SystemSnapshot> CollectAsync(CancellationToken cancellationToken = default)
    {
        Full++;
        return await inner.CollectAsync(cancellationToken);
    }
    public async Task<SystemSnapshot> CollectMinimumAsync(CancellationToken cancellationToken = default)
    {
        Minimum++;
        return await inner.CollectMinimumAsync(cancellationToken);
    }
}
