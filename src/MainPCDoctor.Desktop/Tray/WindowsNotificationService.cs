using MainPCDoctor.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace MainPCDoctor.Desktop.Tray;

public sealed class WindowsNotificationService : INotificationService
{
    private readonly TrayController                      _tray;
    private readonly ILogger<WindowsNotificationService> _logger;

    private DateTimeOffset _lastRamNotification = DateTimeOffset.MinValue;

    private static readonly TimeSpan RamCooldown = TimeSpan.FromMinutes(30);

    public WindowsNotificationService(TrayController tray, ILogger<WindowsNotificationService> logger)
    {
        _tray   = tray;
        _logger = logger;
    }

    public void NotifyLevel2Ram(string incidentId)
    {
        var now = DateTimeOffset.UtcNow;
        if (now - _lastRamNotification < RamCooldown) return;
        _lastRamNotification = now;

        _tray.SetIconAmber();
        _tray.ShowBalloon("RAM Pressure Detected",
            "Your system is running low on free memory. Consider closing unused applications.",
            ToolTipIcon.Warning);

        _logger.LogInformation("Level2 RAM notification sent. IncidentId={Id}", incidentId);
    }

    public void NotifyLevel4Upgrade(string component, string confidence)
    {
        // Deduplication is handled by UpgradeNotificationState (persistent across restarts).
        // This method fires unconditionally when called.
        string title = component.Contains("RAM", StringComparison.OrdinalIgnoreCase)
            ? "RAM Upgrade Recommended"
            : "CPU Upgrade Recommended";

        _tray.ShowBalloon(title,
            $"Confidence: {confidence}. Open MainPC Doctor for details.",
            ToolTipIcon.Info);

        _logger.LogInformation("Level4 upgrade notification sent: {Component} ({Confidence})", component, confidence);
    }
}
