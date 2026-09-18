namespace MainPCDoctor.Core.Diagnosis;

public record DiagnosisThresholds
{
    // RAM
    public float RamFreeThresholdMinGb        { get; init; } = 2.0f;
    public float RamFreeThresholdRatio         { get; init; } = 0.05f;
    public float RamCommitChargeRatio          { get; init; } = 0.80f;
    public int   RamPressureConfirmSeconds     { get; init; } = 120;
    public int   RamPressureMinSignals         { get; init; } = 2;

    // CPU
    public float CpuUtilizationThreshold      { get; init; } = 0.85f;
    public float CpuAffectedCoreRatio         { get; init; } = 0.50f;
    public int   CpuBottleneckConfirmSeconds  { get; init; } = 300;

    // Disk
    public float DiskUtilizationThreshold     { get; init; } = 0.90f;
    public float DiskLatencyThresholdMs       { get; init; } = 50f;
    public int   DiskQueueDepthThreshold      { get; init; } = 5;
    public int   DiskBottleneckConfirmSeconds { get; init; } = 90;

    // VRAM
    public float VramUtilizationThreshold     { get; init; } = 0.92f;
    public int   VramPressureConfirmSeconds   { get; init; } = 180;

    // GPU
    public float GpuUtilizationThreshold      { get; init; } = 0.90f;
    public int   GpuComputeConfirmSeconds     { get; init; } = 120;

    // Thermal
    public float CpuThermalCelsius            { get; init; } = 90f;
    public float CpuClockDegradationRatio     { get; init; } = 0.10f;
    public int   ThermalConfirmSeconds        { get; init; } = 60;

    // Process
    public float ProcessCpuThreshold          { get; init; } = 0.40f;
    public float ProcessMemoryThresholdMb     { get; init; } = 500f;
    public int   ProcessAnomalyConfirmSeconds { get; init; } = 120;

    public static DiagnosisThresholds Default { get; } = new();
}
