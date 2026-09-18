using System.Windows;
using System.Windows.Controls;
using MainPCDoctor.Desktop.ViewModels;

namespace MainPCDoctor.Desktop.Views;

public partial class SettingsView : Page
{
    private readonly SettingsViewModel _vm;

    public SettingsView()
    {
        InitializeComponent();
        _vm = new SettingsViewModel();
        Loaded += (_, _) =>
        {
            StartupCheck.IsChecked = _vm.LaunchOnStartup;
            VersionText.Text       = _vm.AppVersion;
        };
    }

    private void StartupCheck_Changed(object sender, RoutedEventArgs e)
    {
        _vm.LaunchOnStartup = StartupCheck.IsChecked ?? false;
        _vm.ToggleStartupCommand.Execute(null);
    }
}
