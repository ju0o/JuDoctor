using System.Windows.Controls;
using MainPCDoctor.Core.Abstractions;
using MainPCDoctor.Core.Recommendations;
using MainPCDoctor.Desktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MainPCDoctor.Desktop.Views;

public partial class SystemCapacityView : Page
{
    private readonly SystemCapacityViewModel _vm;

    public SystemCapacityView()
    {
        InitializeComponent();
        _vm = new SystemCapacityViewModel(
            App.Services.GetRequiredService<IIncidentStore>(),
            App.Services.GetRequiredService<UpgradeRecommendationEngine>());

        Loaded += async (_, _) =>
        {
            await _vm.EvaluateCommand.ExecuteAsync(null);
            CpuRecText.Text  = _vm.CpuRecommendation;
            CpuConfText.Text = _vm.CpuConfidence;
            RamRecText.Text  = _vm.RamRecommendation;
            RamConfText.Text = _vm.RamConfidence;
        };
    }
}
