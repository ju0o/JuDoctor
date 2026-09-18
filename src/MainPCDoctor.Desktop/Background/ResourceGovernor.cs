using System.Diagnostics;

namespace MainPCDoctor.Desktop.Background;

internal record GovernorReading(bool Throttled, long? PrivateBytes, bool ReadFailed = false);

// One instance per worker; hysteresis prevents flapping near the entry boundary.
internal sealed class ResourceGovernor
{
    internal const long PrivateMemoryBudgetBytes = 400L * 1_048_576;
    internal const long RecoveryBudgetBytes = 350L * 1_048_576;
    internal static readonly TimeSpan PressureInterval = TimeSpan.FromSeconds(60);

    private readonly Func<long> _readPrivateBytes;
    private bool _throttled;

    internal ResourceGovernor(Func<long>? readPrivateBytes = null)
        => _readPrivateBytes = readPrivateBytes ?? ReadPrivateBytes;

    private static long ReadPrivateBytes()
    {
        using var self = Process.GetCurrentProcess();
        self.Refresh();
        return self.PrivateMemorySize64;
    }

    internal GovernorReading Read()
    {
        try
        {
            long bytes = _readPrivateBytes();
            if (bytes < 0) throw new InvalidOperationException("Invalid private-memory reading");
            if (bytes > PrivateMemoryBudgetBytes) _throttled = true;
            else if (bytes < RecoveryBudgetBytes) _throttled = false;
            return new(_throttled, bytes);
        }
        catch
        {
            // Reading failure reduces work, never disables minimum monitoring.
            _throttled = true;
            return new(true, null, ReadFailed: true);
        }
    }
}
