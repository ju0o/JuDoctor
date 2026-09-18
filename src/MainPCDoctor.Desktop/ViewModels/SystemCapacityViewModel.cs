using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Models;
using MainPCDoctor.Core.Recommendations;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class SystemCapacityViewModel : ObservableObject
{
    private readonly IIncidentStore               _store;
    private readonly UpgradeRecommendationEngine  _engine;

    // CPU
    [ObservableProperty] private string  _cpuRecommendation = "Collecting data…";
    [ObservableProperty] private string  _cpuConfidence     = "";
    [ObservableProperty] private bool    _cpuHasRec;

    // RAM
    [ObservableProperty] private string  _ramRecommendation = "Collecting data…";
    [ObservableProperty] private string  _ramConfidence     = "";
    [ObservableProperty] private bool    _ramHasRec;

    // GPU / Disk — monitoring status only, no recommendation in V1
    [ObservableProperty] private string  _gpuStatus   = "Monitoring — no recommendation in V1";
    [ObservableProperty] private string  _diskStatus  = "Monitoring — no recommendation in V1";

    public SystemCapacityViewModel(IIncidentStore store, UpgradeRecommendationEngine engine)
    {
        _store  = store;
        _engine = engine;
    }

    [RelayCommand]
    private async Task EvaluateAsync()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-90);
        var to   = DateTimeOffset.UtcNow;
        var incidents = await _store.GetByDateRangeAsync(from, to);

        var windowStart = from;
        var cpuResult = _engine.EvaluateCpu(incidents, windowStart);
        var ramResult = _engine.EvaluateRam(incidents, windowStart);

        CpuHasRec         = cpuResult != null;
        CpuRecommendation = cpuResult != null
            ? $"CPU upgrade recommended (score {cpuResult.EvidenceScore:F0}, {cpuResult.ObservationDays}d data)."
            : "No upgrade recommended at this time.";
        CpuConfidence     = cpuResult != null ? $"Confidence: {cpuResult.Confidence}" : "";

        RamHasRec         = ramResult != null;
        RamRecommendation = ramResult != null
            ? $"RAM upgrade recommended (score {ramResult.EvidenceScore:F0}, {ramResult.ObservationDays}d data)."
            : "No upgrade recommended at this time.";
        RamConfidence     = ramResult != null ? $"Confidence: {ramResult.Confidence}" : "";
    }
}
