using System.Diagnostics;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Platform.Windows.Collectors;

internal sealed class WindowsGpuCollector : IDisposable
{
    private PerformanceCounter? _utilCounter;
    private PerformanceCounter? _vramUsedCounter;
    private float               _vramTotalGb;
    private bool                _available;

    public void Initialize()
    {
        _vramTotalGb = ReadVramTotalFromDxgi();
        InitPdhCounters();
    }

    public GpuMetrics? Collect()
    {
        if (!_available) return null;

        try
        {
            float util     = _utilCounter?.NextValue()    ?? 0f;
            float vramUsed = (_vramUsedCounter?.NextValue() ?? 0f) / (1024f * 1024f * 1024f);

            return new GpuMetrics(
                UtilizationPercent: util,
                VramUsedGb:         vramUsed,
                VramTotalGb:        _vramTotalGb,
                TemperatureCelsius: null  // set by LHM layer
            );
        }
        catch { return null; }
    }

    internal float VramTotalGb => _vramTotalGb;

    private static float ReadVramTotalFromDxgi() => DxgiVramReader.ReadDedicatedGb();

    private void InitPdhCounters()
    {
        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames()
                .Where(n => n.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .ToArray();

            if (instances.Length > 0)
            {
                _utilCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instances[0], readOnly: true);
                _utilCounter.NextValue();
                _available = true;
            }
        }
        catch { _available = false; }

        try
        {
            var cat  = new PerformanceCounterCategory("GPU Process Memory");
            var inst = cat.GetInstanceNames().FirstOrDefault();
            if (inst != null)
            {
                _vramUsedCounter = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", inst, readOnly: true);
                _vramUsedCounter.NextValue();
            }
        }
        catch { }
    }

    public void Dispose()
    {
        _utilCounter?.Dispose();
        _vramUsedCounter?.Dispose();
    }
}
