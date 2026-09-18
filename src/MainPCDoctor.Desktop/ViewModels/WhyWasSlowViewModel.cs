using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class WhyWasSlowViewModel : ObservableObject
{
    private readonly IIncidentStore _store;

    [ObservableProperty] private DateTimeOffset      _queryStart = DateTimeOffset.UtcNow.AddHours(-24);
    [ObservableProperty] private DateTimeOffset      _queryEnd   = DateTimeOffset.UtcNow;
    [ObservableProperty] private IReadOnlyList<Incident> _results = [];
    [ObservableProperty] private string              _summary    = "Select a time range and press Analyze.";
    [ObservableProperty] private bool                _isLoading;

    public WhyWasSlowViewModel(IIncidentStore store)
    {
        _store = store;
    }

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        IsLoading = true;
        try
        {
            Results = await _store.GetByDateRangeAsync(QueryStart, QueryEnd);

            if (Results.Count == 0)
            {
                Summary = "No incidents recorded in this time range.";
                return;
            }

            var types = Results.GroupBy(i => i.BottleneckType)
                               .OrderByDescending(g => g.Count())
                               .Select(g => $"{g.Key} ×{g.Count()}")
                               .ToList();

            Summary = $"Found {Results.Count} incident(s): {string.Join(", ", types)}.";
        }
        finally { IsLoading = false; }
    }
}
