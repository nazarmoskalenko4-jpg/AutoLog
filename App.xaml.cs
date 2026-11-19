using AutoLog.Views;

namespace AutoLog;

public partial class App : Application
{
    public App(AuthPage authPage)
    {
        InitializeComponent();
        MainPage = authPage;
    }
}