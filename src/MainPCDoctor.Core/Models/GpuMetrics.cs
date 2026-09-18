namespace MainPCDoctor.Core.Models;

public record GpuMetrics(
    float  UtilizationPercent,
    float  VramUsedGb,
    float  VramTotalGb,
    float? TemperatureCelsius
);
