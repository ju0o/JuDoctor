namespace MainPCDoctor.Core.Models;

public enum BottleneckType
{
    RamPressure,
    CpuBottleneck,
    DiskBottleneck,
    VramPressure,
    GpuCompute,
    ThermalThrottling,
    ProcessAnomaly
}
