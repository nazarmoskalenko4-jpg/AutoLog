using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class AddFuelRecordPage : ContentPage
{
    public AddFuelRecordPage(AddFuelRecordViewModel viewModel)
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