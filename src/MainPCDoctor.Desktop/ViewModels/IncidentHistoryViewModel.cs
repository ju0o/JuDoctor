using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Models;

namespace MainPCDoctor.Desktop.ViewModels;

public sealed partial class IncidentHistoryViewModel : ObservableObject
{
    private readonly IIncidentStore _store;

    [ObservableProperty] private IReadOnlyList<Incident> _incidents = [];
    [ObservableProperty] private bool                    _isLoading;

    public IncidentHistoryViewModel(IIncidentStore store)
    {
        _store = store;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var from = DateTimeOffset.UtcNow.AddDays(-30);
            var to   = DateTimeOffset.UtcNow;
            Incidents = await _store.GetByDateRangeAsync(from, to);
        }
        finally { IsLoading = false; }
    }
}
