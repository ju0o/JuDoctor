using System.Diagnostics;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Platform.Windows.Collectors;

internal sealed class WindowsDiskCollector : IDisposable
{
    private readonly List<DriveCounters> _drives = new();
    private bool _latencyAvailable;

    private sealed class DriveCounters(string name) : IDisposable
    {
        public string Name { get; } = name;
        public PerformanceCounter? Util    { get; set; }
        public PerformanceCounter? Latency { get; set; }
        public PerformanceCounter? Queue   { get; set; }
        public void Dispose() { Util?.Dispose(); Latency?.Dispose(); Queue?.Dispose(); }
    }

    public void Initialize()
    {
        try
        {
            var category = new PerformanceCounterCategory("PhysicalDisk");
            var instances = category.GetInstanceNames()
                .Where(n => n != "_Total")
                .ToArray();

            foreach (var inst in instances)
            {
                var dc = new DriveCounters(inst);
                try
                {
                    dc.Util    = new PerformanceCounter("PhysicalDisk", "% Disk Time", inst, readOnly: true);
                    dc.Latency = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Transfer", inst, readOnly: true);
                    dc.Queue   = new PerformanceCounter("PhysicalDisk", "Current Disk Queue Length", inst, readOnly: true);
                    dc.Util.NextValue();
                    dc.Latency.NextValue();
                    dc.Queue.NextValue();
                    _latencyAvailable = true;
                }
                catch { /* individual counter may be unavailable */ }
                _drives.Add(dc);
            }
        }
        catch { }
    }

    public IReadOnlyList<DiskMetrics> Collect()
    {
        var results = new List<DiskMetrics>(_drives.Count);
        foreach (var d in _drives)
        {
            try
            {
                float util   = d.Util?.NextValue()    ?? 0f;
                float queue  = d.Queue?.NextValue()   ?? 0f;
                float? latMs = d.Latency != null
                    ? d.Latency.NextValue() * 1000f  // sec → ms
                    : null;

                results.Add(new DiskMetrics(d.Name, util, latMs, queue));
            }
            catch { }
        }
        return results;
    }

    public bool LatencyAvailable => _latencyAvailable;

    public void Dispose()
    {
        foreach (var d in _drives) d.Dispose();
    }
}
