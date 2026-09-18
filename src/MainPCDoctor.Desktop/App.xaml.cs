using MainPCDoctor.Desktop.Tray;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
// Application is globally aliased to System.Windows.Application in GlobalUsings.cs

namespace MainPCDoctor.Desktop;

public partial class App : Application
{
    // Both set by Program.Main before App.Run() so DI is available from OnStartup.
    public static IServiceProvider Services       { get; set; } = null!;
    public static bool             StartMinimized { get; set; }

    private TrayController? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _tray = Services.GetRequiredService<TrayController>();
        _tray.Initialize(
            openDashboard: OpenDashboard,
            exitApp:       () => { /* clean exit — Shutdown called inside TrayController */ });

        var dashboard = new MainWindow();
        MainWindow = dashboard;

        // --tray launch (Windows autostart): stay hidden in the tray.
        // Normal launch: show the dashboard immediately.
        if (!StartMinimized)
            dashboard.Show();
    }

    private void OpenDashboard()
    {
        if (MainWindow == null) return;
        MainWindow.Show();
        MainWindow.WindowState = WindowState.Normal;
        MainWindow.Activate();
    }

    protected override void OnSessionEnding(SessionEndingCancelEventArgs e)
    {
        base.OnSessionEnding(e);
        Shutdown();
    }
}
