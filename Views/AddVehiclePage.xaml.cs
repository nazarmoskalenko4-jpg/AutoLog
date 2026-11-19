using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class AddVehiclePage : ContentPage
{
    public AddVehiclePage(AddVehicleViewModel viewModel)
    {
        InitializeComponent();
        BasePage.AttachMenu(this);
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}