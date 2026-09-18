using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainPCDoctor.Core.History;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly RollingMetricsBuffer _buffer;

    [ObservableProperty] private string  _currentView = "Dashboard";
    [ObservableProperty] private float   _cpuPercent;
    [ObservableProperty] private float   _ramUsedGb;
    [ObservableProperty] private float   _ramTotalGb;
    [ObservableProperty] private float   _diskPercent;
    [ObservableProperty] private float   _gpuPercent;
    [ObservableProperty] private string  _statusText = "Monitoring…";
    [ObservableProperty] private bool    _isIncidentActive;

    public MainViewModel(RollingMetricsBuffer buffer)
    {
        _buffer = buffer;
    }

    public void Refresh()
    {
        var recent = _buffer.GetRecent(10);
        if (recent.Count == 0) return;

        var latest = recent[^1];
        CpuPercent    = latest.Cpu.TotalUtilizationPercent;
        RamUsedGb     = latest.Memory.UsedGb;
        RamTotalGb    = latest.Memory.TotalPhysicalGb;
        DiskPercent   = latest.Disks.Count > 0 ? latest.Disks.Max(d => d.UtilizationPercent) : 0f;
        GpuPercent    = latest.Gpu?.UtilizationPercent ?? 0f;
    }

    [RelayCommand]
    private void NavigateTo(string view) => CurrentView = view;
}
