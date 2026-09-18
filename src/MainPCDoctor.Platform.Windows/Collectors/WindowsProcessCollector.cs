using System.Diagnostics;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Platform.Windows.Collectors;

internal sealed class WindowsProcessCollector
{
    private Dictionary<int, TimeSpan> _previousCpuTimes = new();
    private DateTimeOffset            _lastSampleTime   = DateTimeOffset.MinValue;

    public ProcessSummary Collect()
    {
        var now       = DateTimeOffset.UtcNow;
        var elapsed   = (now - _lastSampleTime).TotalSeconds;
        if (elapsed < 0.1) elapsed = 0.1;
        int cpuCount  = Environment.ProcessorCount;

        var processes = Process.GetProcesses();
        var metrics   = new List<ProcessMetric>(processes.Length);
        var newTimes  = new Dictionary<int, TimeSpan>(processes.Length);

        foreach (var proc in processes)
        {
            try
            {
                float cpu = 0f;
                if (_previousCpuTimes.TryGetValue(proc.Id, out var prevTime))
                {
                    var delta = proc.TotalProcessorTime - prevTime;
                    cpu = (float)(delta.TotalSeconds / (elapsed * cpuCount) * 100.0);
                    cpu = Math.Clamp(cpu, 0f, 100f);
                }

                newTimes[proc.Id] = proc.TotalProcessorTime;
                float memMb = proc.WorkingSet64 / 1_048_576f;

                metrics.Add(new ProcessMetric(proc.ProcessName, cpu, memMb));
            }
            catch { /* process may have exited */ }
        }

        foreach (var p in processes) try { p.Dispose(); } catch { }

        _previousCpuTimes = newTimes;
        _lastSampleTime   = now;

        var topByCpu = metrics.OrderByDescending(m => m.CpuPercent).Take(5).ToList();
        var topByRam = metrics.OrderByDescending(m => m.MemoryMb).Take(5).ToList();

        return new ProcessSummary(topByCpu, topByRam, now);
    }
}
