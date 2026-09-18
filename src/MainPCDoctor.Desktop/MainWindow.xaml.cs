using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using MainPCDoctor.Desktop.Views;

namespace MainPCDoctor.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ContentFrame.Navigate(new DashboardView());
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        var tag = ((Button)sender).Tag?.ToString();
        Page page = tag switch
        {
            "Dashboard" => new DashboardView(),
            "History"   => new IncidentHistoryView(),
            "Capacity"  => new SystemCapacityView(),
            "WhySlow"   => new WhyWasSlowView(),
            "Settings"  => new SettingsView(),
            _            => new DashboardView(),
        };
        ContentFrame.Navigate(page);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
