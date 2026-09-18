using System.Windows.Controls;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MainPCDoctor.Desktop.Views;

public partial class IncidentHistoryView : Page
{
    private readonly IncidentHistoryViewModel _vm;

    public IncidentHistoryView()
    {
        InitializeComponent();
        _vm = new IncidentHistoryViewModel(App.Services.GetRequiredService<IIncidentStore>());
        Loaded += async (_, _) =>
        {
            await _vm.LoadCommand.ExecuteAsync(null);
            IncidentList.ItemsSource = _vm.Incidents;
        };
    }
}
