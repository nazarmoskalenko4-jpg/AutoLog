using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class VehiclesPage : ContentPage
{
    private readonly VehiclesViewModel _viewModel;

    public VehiclesPage(VehiclesViewModel viewModel)
    {
        InitializeComponent();
        BasePage.AttachMenu(this);
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadDataCommand.Execute(null);
    }
}