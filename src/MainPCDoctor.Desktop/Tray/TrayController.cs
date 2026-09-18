using System.Windows.Forms;
using System.Windows.Threading;

namespace MainPCDoctor.Desktop.Tray;

public sealed class TrayController : IDisposable
{
    private NotifyIcon?       _icon;
    private ContextMenuStrip? _menu;
    private Dispatcher?       _dispatcher;

    private Action? _openDashboard;
    private Action? _exitApp;

    public void Initialize(Action openDashboard, Action exitApp)
    {
        _openDashboard = openDashboard;
        _exitApp       = exitApp;
        _dispatcher    = Dispatcher.CurrentDispatcher;

        _menu = new ContextMenuStrip();
        _menu.Items.Add("Open Dashboard", null, (_, _) => _openDashboard?.Invoke());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => OnExit());

        _icon = new NotifyIcon
        {
            Text    = "MainPC Doctor",
            Icon    = SystemIcons.Application,
            Visible = true,
        };
        _icon.ContextMenuStrip = _menu;
        _icon.DoubleClick     += (_, _) => _openDashboard?.Invoke();
    }

    public void SetIconNormal() => RunOnUiThread(() =>
    {
        if (_icon != null) _icon.Icon = SystemIcons.Application;
    });

    public void SetIconAmber() => RunOnUiThread(() =>
    {
        if (_icon != null) _icon.Icon = SystemIcons.Warning;
    });

    public void ShowBalloon(string title, string text, ToolTipIcon tipIcon = ToolTipIcon.Info)
        => RunOnUiThread(() => _icon?.ShowBalloonTip(5000, title, text, tipIcon));

    private void RunOnUiThread(Action action)
    {
        if (_dispatcher == null || _dispatcher.CheckAccess())
            action();
        else
            _dispatcher.Invoke(action);
    }

    private void OnExit()
    {
        _exitApp?.Invoke();
        Application.Current?.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    public void Dispose()
    {
        if (_icon != null)
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
        _menu?.Dispose();
    }
}
