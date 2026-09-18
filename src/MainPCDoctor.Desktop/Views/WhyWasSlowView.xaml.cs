using System.Windows;
using System.Windows.Controls;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MainPCDoctor.Desktop.Views;

public partial class WhyWasSlowView : Page
{
    private readonly WhyWasSlowViewModel _vm;

    public WhyWasSlowView()
    {
        InitializeComponent();
        _vm = new WhyWasSlowViewModel(App.Services.GetRequiredService<IIncidentStore>());
    }

    private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        await _vm.AnalyzeCommand.ExecuteAsync(null);
        SummaryText.Text     = _vm.Summary;
        ResultList.ItemsSource = _vm.Results;
    }
}
