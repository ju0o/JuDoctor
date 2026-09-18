using CommunityToolkit.Mvvm.ComponentModel;
using MainPCDoctor.Core.History;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly RollingMetricsBuffer _buffer;

    [ObservableProperty] private float  _cpuPercent;
    [ObservableProperty] private float  _ramPercent;
    [ObservableProperty] private float  _diskPercent;
    [ObservableProperty] private float  _gpuPercent;
    [ObservableProperty] private float  _ramFreeGb;
    [ObservableProperty] private float  _ramTotalGb;
    [ObservableProperty] private float? _cpuTempC;
    [ObservableProperty] private float? _gpuTempC;
    [ObservableProperty] private string _topCpuProcess = "—";
    [ObservableProperty] private string _topRamProcess = "—";

    public DashboardViewModel(RollingMetricsBuffer buffer)
    {
        _buffer = buffer;
    }

    public void Refresh()
    {
        var window = _buffer.GetRecent(10);
        if (window.Count == 0) return;
        var s = window[^1];

        CpuPercent   = s.Cpu.TotalUtilizationPercent;
        RamFreeGb    = s.Memory.AvailableGb;
        RamTotalGb   = s.Memory.TotalPhysicalGb;
        RamPercent   = RamTotalGb > 0 ? (s.Memory.UsedGb / RamTotalGb) * 100f : 0f;
        DiskPercent  = s.Disks.Count > 0 ? s.Disks.Max(d => d.UtilizationPercent) : 0f;
        GpuPercent   = s.Gpu?.UtilizationPercent ?? 0f;
        CpuTempC     = s.Cpu.TemperatureCelsius;
        GpuTempC     = s.Gpu?.TemperatureCelsius;
        TopCpuProcess = s.Processes?.TopByCpu.FirstOrDefault()?.Name ?? "—";
        TopRamProcess = s.Processes?.TopByRam.FirstOrDefault()?.Name ?? "—";
    }
}
