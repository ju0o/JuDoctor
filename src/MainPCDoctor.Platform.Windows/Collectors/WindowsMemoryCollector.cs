using System.Diagnostics;
using System.Runtime.InteropServices;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Platform.Windows.Collectors;

internal sealed class WindowsMemoryCollector : IDisposable
{
    private PerformanceCounter? _commitBytes;
    private PerformanceCounter? _commitLimit;
    private float               _totalPhysicalGb;

    public float TotalPhysicalGb => _totalPhysicalGb;

    public void Initialize()
    {
        var info = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref info))
            _totalPhysicalGb = info.ullTotalPhys / (1024f * 1024f * 1024f);

        try
        {
            _commitBytes = new PerformanceCounter("Memory", "Committed Bytes", readOnly: true);
            _commitLimit = new PerformanceCounter("Memory", "Commit Limit", readOnly: true);
            _commitBytes.NextValue();
            _commitLimit.NextValue();
        }
        catch { }
    }

    public MemoryMetrics Collect()
    {
        var info = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        float available = 0f;
        if (GlobalMemoryStatusEx(ref info))
            available = info.ullAvailPhys / (1024f * 1024f * 1024f);

        float commitGb  = (_commitBytes?.NextValue() ?? 0f) / (1024f * 1024f * 1024f);
        float limitGb   = (_commitLimit?.NextValue() ?? 0f) / (1024f * 1024f * 1024f);
        bool  pagefile  = commitGb > _totalPhysicalGb && _totalPhysicalGb > 0;

        return new MemoryMetrics(
            TotalPhysicalGb:      _totalPhysicalGb,
            AvailableGb:         available,
            CommitChargeGb:      commitGb,
            CommitLimitGb:       limitGb,
            PagefilePressureProxy: pagefile);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint  dwLength;
        public uint  dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    public void Dispose()
    {
        _commitBytes?.Dispose();
        _commitLimit?.Dispose();
    }
}
