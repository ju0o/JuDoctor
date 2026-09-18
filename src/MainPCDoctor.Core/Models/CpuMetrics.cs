namespace MainPCDoctor.Core.Models;

public record CpuMetrics(
    float TotalUtilizationPercent,
    float[] PerCoreUtilizationPercent,
    int    LogicalCoreCount,
    float? BaseClockMhz,
    float? CurrentClockMhz,
    float? TemperatureCelsius
);
