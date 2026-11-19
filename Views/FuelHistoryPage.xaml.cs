using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class FuelHistoryPage : ContentPage
{
    private readonly FuelHistoryViewModel _viewModel;

    public FuelHistoryPage(FuelHistoryViewModel viewModel)
    {
        InitializeComponent();
        BasePage.AttachMenu(this);
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_viewModel.Vehicle != null)
        {
            _viewModel.LoadRecordsCommand.Execute(null);
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}