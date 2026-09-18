using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MainPCDoctor.Core.History;
using MainPCDoctor.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MainPCDoctor.Desktop.Views;

public partial class DashboardView : Page
{
    private readonly DashboardViewModel _vm;
    private readonly DispatcherTimer    _timer;

    public DashboardView()
    {
        InitializeComponent();
        _vm = new DashboardViewModel(App.Services.GetRequiredService<RollingMetricsBuffer>());

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick   += (_, _) => Refresh();
        Loaded         += (_, _) => { _timer.Start(); Refresh(); };
        Unloaded       += (_, _) => _timer.Stop();
    }

    private void Track_SizeChanged(object sender, SizeChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        _vm.Refresh();

        double w = CpuTrack.ActualWidth > 0 ? CpuTrack.ActualWidth : 200;

        CpuValueText.Text = $"{_vm.CpuPercent:F0}%";
        CpuBar.Width      = Math.Max(0, w * (_vm.CpuPercent / 100.0));
        CpuTempText.Text  = _vm.CpuTempC.HasValue ? $"{_vm.CpuTempC:F0}°C" : "";

        float ramPct      = _vm.RamTotalGb > 0 ? _vm.RamPercent : 0f;
        RamValueText.Text = $"{ramPct:F0}%";
        RamBar.Width      = Math.Max(0, w * (ramPct / 100.0));
        RamFreeText.Text  = $"{_vm.RamFreeGb:F1} GB free";

        GpuValueText.Text = $"{_vm.GpuPercent:F0}%";
        GpuBar.Width      = Math.Max(0, w * (_vm.GpuPercent / 100.0));
        GpuTempText.Text  = _vm.GpuTempC.HasValue ? $"{_vm.GpuTempC:F0}°C" : "";

        DiskValueText.Text = $"{_vm.DiskPercent:F0}%";
        DiskBar.Width      = Math.Max(0, w * (_vm.DiskPercent / 100.0));

        TopCpuText.Text = _vm.TopCpuProcess;
        TopRamText.Text = _vm.TopRamProcess;
    }
}
