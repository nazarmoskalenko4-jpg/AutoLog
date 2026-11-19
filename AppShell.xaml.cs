using System.Windows.Input;
using AutoLog.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLog;

public partial class AppShell : Shell
{
    public static AppShell? Instance { get; private set; }
    private readonly List<WeakReference<VisualElement>> _drawers = new();

    private bool _isMenuOpen;
    public bool IsMenuOpen
    {
        get => _isMenuOpen;
        set
        {
            if (_isMenuOpen == value) return;
            _isMenuOpen = value;
            OnPropertyChanged(nameof(IsMenuOpen));
            _ = AnimateDrawersAsync(value);
        }
    }

    private string _activeRoute = "VehiclesPage";
    public string ActiveRoute
    {
        get => _activeRoute;
        set
        {
            _activeRoute = value;
            OnPropertyChanged(nameof(ActiveRoute));
            OnPropertyChanged(nameof(GarageBtnBg));
            OnPropertyChanged(nameof(OptimizerBtnBg));
            OnPropertyChanged(nameof(SettingsBtnBg));
            OnPropertyChanged(nameof(CurrentSectionTitle));
        }
    }

    public string CurrentSectionTitle => ActiveRoute switch
    {
        "OptimizerPage" => "Калькулятор вигоди АЗС",
        "SettingsPage" => "Параметри профілю",
        _ => "Керування автопарком"
    };

    public string GarageBtnBg => ActiveRoute == "VehiclesPage" ? "#3B82F6" : "#313B54";
    public string OptimizerBtnBg => ActiveRoute == "OptimizerPage" ? "#3B82F6" : "#313B54";
    public string SettingsBtnBg => ActiveRoute == "SettingsPage" ? "#3B82F6" : "#313B54";

    public string UserFullName
    {
        get
        {
            var name = Preferences.Get("UserName", "Користувач");
            return string.IsNullOrWhiteSpace(name) ? "Користувач" : name;
        }
    }

    public string UserEmail => Preferences.Get("UserEmail", "user@autolog.ua");

    public string UserInitials
    {
        get
        {
            var name = UserFullName.Trim();
            if (string.IsNullOrEmpty(name)) return "U";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
            return char.ToUpper(name[0]).ToString();
        }
    }

    public ICommand ToggleMenuCommand { get; }
    public ICommand CloseMenuCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public AppShell()
    {
        Instance = this;

        ToggleMenuCommand = new Command(() =>
        {
            RefreshUserHeader();
            IsMenuOpen = !IsMenuOpen;
        });

        CloseMenuCommand = new Command(() => IsMenuOpen = false);

        NavigateCommand = new Command<string>(async (route) =>
        {
            IsMenuOpen = false;
            ActiveRoute = route;
            await Current.GoToAsync($"///{route}");
        });

        LogoutCommand = new Command(() =>
        {
            IsMenuOpen = false;
            var authPage = Handler?.MauiContext?.Services.GetService<AuthPage>();
            if (authPage != null)
            {
                Application.Current!.MainPage = authPage;
            }
        });

        InitializeComponent();

        Routing.RegisterRoute(nameof(AddVehiclePage), typeof(AddVehiclePage));
        Routing.RegisterRoute("FuelHistoryPage", typeof(FuelHistoryPage));
        Routing.RegisterRoute("AddFuelRecordPage", typeof(AddFuelRecordPage));
    }

    public void RegisterDrawer(VisualElement drawer)
    {
        drawer.TranslationX = _isMenuOpen ? 0 : -310;
        _drawers.Add(new WeakReference<VisualElement>(drawer));
    }

    private async Task AnimateDrawersAsync(bool open)
    {
        double targetX = open ? 0 : -310;
        var tasks = new List<Task>();

        foreach (var weakRef in _drawers.ToList())
        {
            if (weakRef.TryGetTarget(out var drawer))
            {
                tasks.Add(drawer.TranslateTo(targetX, 0, open ? 260u : 220u, open ? Easing.CubicOut : Easing.CubicIn));
            }
        }

        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    public void RefreshUserHeader()
    {
        OnPropertyChanged(nameof(UserFullName));
        OnPropertyChanged(nameof(UserEmail));
        OnPropertyChanged(nameof(UserInitials));
    }
}