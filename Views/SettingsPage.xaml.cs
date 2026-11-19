using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BasePage.AttachMenu(this);
        BindingContext = viewModel;
    }
}