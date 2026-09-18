using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainPCDoctor.Desktop.Background;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool   _launchOnStartup;
    [ObservableProperty] private string _appVersion = GetVersion();

    public SettingsViewModel()
    {
        _launchOnStartup = StartupRegistrar.IsRegistered();
    }

    [RelayCommand]
    private void ToggleStartup()
    {
        if (LaunchOnStartup)
        {
            var exe = Environment.ProcessPath ?? "";
            if (!string.IsNullOrEmpty(exe))
                StartupRegistrar.Register(exe);
        }
        else
        {
            StartupRegistrar.Unregister();
        }
    }

    private static string GetVersion()
    {
        var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}" : "v1.0.0";
    }
}
