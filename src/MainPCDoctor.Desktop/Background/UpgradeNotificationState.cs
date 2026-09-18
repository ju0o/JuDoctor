using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Desktop.Background;

// Persists Level-4 upgrade notification state across process restarts.
// Stored as JSON in %APPDATA%\MainPCDoctor\upgrade-state.json.
public sealed class UpgradeNotificationState
{
    internal static readonly TimeSpan ReminderInterval = TimeSpan.FromDays(7);

    private readonly string  _path;
    private readonly PersistedState _state;

    internal UpgradeNotificationState(string appDataDir)
    {
        _path  = Path.Combine(appDataDir, "upgrade-state.json");
        _state = Load();
    }

    // Returns true if the daily upgrade evaluation has already been recorded for today.
    internal bool WasCheckedToday(DateTime? today = null) =>
        _state.LastCheckDate == (today ?? DateTime.Today).ToString("yyyy-MM-dd");

    // Call before running CheckAndNotifyUpgradeAsync so a same-day restart is a no-op.
    internal void RecordCheck(DateTime? today = null)
    {
        _state.LastCheckDate = (today ?? DateTime.Today).ToString("yyyy-MM-dd");
        Save();
    }

    // Returns true when a Level-4 toast for this component should be sent.
    // Allows: first notification, confidence increase, or ≥ 7-day gap.
    internal bool ShouldNotify(string component, ConfidenceLevel newConfidence)
    {
        if (!_state.Notifications.TryGetValue(component, out var prior))
            return true;

        if ((int)newConfidence > prior.ConfidenceValue)
            return true;

        return DateTimeOffset.UtcNow - prior.NotifiedAt >= ReminderInterval;
    }

    // Call only after the notification service has accepted the toast.
    internal void RecordNotification(string component, ConfidenceLevel confidence)
    {
        _state.Notifications[component] = new ComponentEntry((int)confidence, DateTimeOffset.UtcNow);
        Save();
    }

    private void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_state)); }
        catch { /* non-fatal: loss causes potential one extra notification on next startup */ }
    }

    private PersistedState Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<PersistedState>(File.ReadAllText(_path)) ?? new();
        }
        catch { /* corrupt → reset */ }
        return new();
    }

    internal sealed class PersistedState
    {
        [JsonPropertyName("lastCheckDate")]
        public string LastCheckDate { get; set; } = "";

        [JsonPropertyName("notifications")]
        public Dictionary<string, ComponentEntry> Notifications { get; set; } = new();
    }

    internal sealed class ComponentEntry
    {
        [JsonConstructor]
        public ComponentEntry(int confidenceValue, DateTimeOffset notifiedAt)
        {
            ConfidenceValue = confidenceValue;
            NotifiedAt      = notifiedAt;
        }

        [JsonPropertyName("confidence")]
        public int ConfidenceValue { get; }

        [JsonPropertyName("notifiedAt")]
        public DateTimeOffset NotifiedAt { get; }
    }
}
