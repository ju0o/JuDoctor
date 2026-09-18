namespace MainPCDoctor.Core.Models;

public record DiskMetrics(
    string DriveName,
    float  UtilizationPercent,
    float? AverageLatencyMs,
    float  QueueDepth
);
