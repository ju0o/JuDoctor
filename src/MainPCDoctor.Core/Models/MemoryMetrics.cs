namespace MainPCDoctor.Core.Models;

public record MemoryMetrics(
    float TotalPhysicalGb,
    float AvailableGb,
    float CommitChargeGb,
    float CommitLimitGb,
    bool  PagefilePressureProxy
)
{
    public float UsedGb              => TotalPhysicalGb - AvailableGb;
    public float CommitChargeRatio   => CommitLimitGb > 0 ? CommitChargeGb / CommitLimitGb : 0f;
}
