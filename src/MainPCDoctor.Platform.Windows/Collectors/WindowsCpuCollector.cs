using System.Diagnostics;
using System.Management;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Platform.Windows.Collectors;

internal sealed class WindowsCpuCollector : IDisposable
{
    private PerformanceCounter?   _totalCounter;
    private PerformanceCounter[]? _coreCounters;
    private float?                _baseClockMhz;
    private int                   _coreCount;
    private bool                  _initialized;

    public void Initialize()
    {
        _coreCount = Environment.ProcessorCount;

        try
        {
            _totalCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _totalCounter.NextValue(); // prime

            _coreCounters = new PerformanceCounter[_coreCount];
            for (int i = 0; i < _coreCount; i++)
            {
                _coreCounters[i] = new PerformanceCounter("Processor", "% Processor Time", i.ToString(), readOnly: true);
                _coreCounters[i].NextValue(); // prime
            }

            _baseClockMhz = ReadBaseClockMhz();
            _initialized  = true;
        }
        catch
        {
            // Graceful degradation — counters may be unavailable in some environments
        }
    }

    public CpuMetrics Collect()
    {
        if (!_initialized) return Fallback();

        float total     = _totalCounter?.NextValue() ?? 0f;
        var   perCore   = _coreCounters != null
            ? Array.ConvertAll(_coreCounters, c => c.NextValue())
            : Array.Empty<float>();

        float? currentClock = ReadCurrentClockMhz();

        return new CpuMetrics(
            TotalUtilizationPercent:     total,
            PerCoreUtilizationPercent:   perCore,
            LogicalCoreCount:            _coreCount,
            BaseClockMhz:               _baseClockMhz,
            CurrentClockMhz:            currentClock,
            TemperatureCelsius:         null  // set by LHM layer
        );
    }

    private float? ReadBaseClockMhz()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT MaxClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
                return Convert.ToSingle(obj["MaxClockSpeed"]);
        }
        catch { }
        return null;
    }

    private float? ReadCurrentClockMhz()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT CurrentClockSpeed FROM Win32_Processor");
            foreach (ManagementObject obj in searcher.Get())
                return Convert.ToSingle(obj["CurrentClockSpeed"]);
        }
        catch { }
        return null;
    }

    private CpuMetrics Fallback() => new(0f, Array.Empty<float>(), _coreCount, null, null, null);

    public void Dispose()
    {
        _totalCounter?.Dispose();
        if (_coreCounters != null) foreach (var c in _coreCounters) c.Dispose();
    }
}
