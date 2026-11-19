using AutoLog.ViewModels;

namespace AutoLog.Views;

public partial class OptimizerPage : ContentPage
{
    public OptimizerPage(OptimizerViewModel viewModel)
    {
        InitializeComponent();
        BasePage.AttachMenu(this);
        BindingContext = viewModel;
    }
}