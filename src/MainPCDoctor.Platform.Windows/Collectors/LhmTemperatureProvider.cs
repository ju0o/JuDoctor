namespace MainPCDoctor.Platform.Windows.Collectors;

// LibreHardwareMonitor temperature wrapper — best-effort, graceful null when sensors are unavailable.
// Requires LibreHardwareMonitorLib NuGet package and elevation for EC sensors.
// Without the package the class compiles and returns (null, null) on every call.
internal sealed class LhmTemperatureProvider : IDisposable
{
#if LIBREHARDWAREMONITOR
    private LibreHardwareMonitor.Hardware.Computer? _computer;
#endif

    public void Initialize()
    {
#if LIBREHARDWAREMONITOR
        try
        {
            _computer = new LibreHardwareMonitor.Hardware.Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
            };
            _computer.Open();
        }
        catch { _computer = null; }
#endif
    }

    public (float? CpuCelsius, float? GpuCelsius) ReadTemperatures()
    {
#if LIBREHARDWAREMONITOR
        if (_computer == null) return (null, null);
        try
        {
            float? cpu = null, gpu = null;
            foreach (var hw in _computer.Hardware)
            {
                hw.Update();
                foreach (var sensor in hw.Sensors)
                {
                    if (sensor.SensorType != LibreHardwareMonitor.Hardware.SensorType.Temperature) continue;
                    if (hw.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.Cpu
                        && sensor.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                        cpu = sensor.Value;
                    if ((hw.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuNvidia
                         || hw.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuAmd
                         || hw.HardwareType == LibreHardwareMonitor.Hardware.HardwareType.GpuIntel)
                        && sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                        gpu = sensor.Value;
                }
            }
            return (cpu, gpu);
        }
        catch { return (null, null); }
#else
        return (null, null);
#endif
    }

    public void Dispose()
    {
#if LIBREHARDWAREMONITOR
        try { _computer?.Close(); } catch { }
#endif
    }
}
