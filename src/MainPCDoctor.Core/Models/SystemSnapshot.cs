namespace MainPCDoctor.Core.Models;

public record SystemSnapshot(
    DateTimeOffset                  CollectedAt,
    CpuMetrics                      Cpu,
    MemoryMetrics                   Memory,
    IReadOnlyList<DiskMetrics>      Disks,
    GpuMetrics?                     Gpu,
    ProcessSummary?                 Processes,
    long?                           SelfPrivateBytes = null,
    string                          MonitoringState = "normal"
);
