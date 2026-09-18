namespace MainPCDoctor.Core.Models;

public record ProcessMetric(
    string Name,
    float  CpuPercent,
    float  MemoryMb
);

public record ProcessSummary(
    IReadOnlyList<ProcessMetric> TopByCpu,
    IReadOnlyList<ProcessMetric> TopByRam,
    DateTimeOffset               CollectedAt
);
