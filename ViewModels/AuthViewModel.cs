using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Models;
using AutoLog.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AutoLog.ViewModels;

public class AuthViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabase _database;

    private ImageSource? _backgroundImageSource;
    public ImageSource? BackgroundImageSource
    {
        get => _backgroundImageSource;
        set { _backgroundImageSource = value; OnPropertyChanged(nameof(BackgroundImageSource)); }
    }

    private bool _isLoginMode = true;
    public bool IsLoginMode
    {
        get => _isLoginMode;
        set
        {
            _isLoginMode = value;
            OnPropertyChanged(nameof(IsLoginMode));
            OnPropertyChanged(nameof(IsRegisterMode));
            OnPropertyChanged(nameof(HeaderTitle));
            OnPropertyChanged(nameof(SubmitButtonText));
            OnPropertyChanged(nameof(SwitchPromptText));
            OnPropertyChanged(nameof(SwitchActionText));
        }
    }

    public bool IsRegisterMode => !IsLoginMode;

    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }

    public string HeaderTitle => IsLoginMode ? "Вхід у систему" : "Створіть акаунт";
    public string SubmitButtonText => IsLoginMode ? "Увійти" : "Зареєструватися";
    public string SwitchPromptText => IsLoginMode ? "Немає облікового запису?" : "Вже зареєстровані?";
    public string SwitchActionText => IsLoginMode ? "Створити акаунт" : "Увійти";

    public ICommand SubmitCommand { get; }
    public ICommand ToggleModeCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AuthViewModel(LocalDatabase database)
    {
        _database = database;
        SubmitCommand = new Command(async () => await ExecuteSubmit());
        ToggleModeCommand = new Command(() => IsLoginMode = !IsLoginMode);

        _ = LoadBackgroundImageAsync();
    }

    private byte[]? _cachedImageBytes;

    private async Task LoadBackgroundImageAsync()
    {
        try
        {
            // 1. Найнадійніший спосіб на macOS: читаємо вбудований ресурс прямо з .dll програми
            var assembly = typeof(AuthViewModel).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => (r.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                      r.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                      r.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) &&
                                     !r.Contains("dotnet_bot", StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    _cachedImageBytes = ms.ToArray();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        BackgroundImageSource = ImageSource.FromStream(() => new MemoryStream(_cachedImageBytes));
                    });
                    return;
                }
            }

            // 2. Читаємо через пакет ресурсів MAUI (Resources/Raw)
            string[] rawNames = { "auth_bg.png", "auth_bg.jpg", "auth_bg.jpeg" };
            foreach (var name in rawNames)
            {
                try
                {
                    using var stream = await FileSystem.OpenAppPackageFileAsync(name);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    _cachedImageBytes = ms.ToArray();
                    if (_cachedImageBytes.Length > 0)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            BackgroundImageSource = ImageSource.FromStream(() => new MemoryStream(_cachedImageBytes));
                        });
                        return;
                    }
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка завантаження фону: {ex.Message}");
        }
    }

    private async Task ExecuteSubmit()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            await Application.Current!.MainPage!.DisplayAlert("Помилка", "Заповніть електронну пошту та пароль.", "ОК");
            return;
        }

        if (IsLoginMode)
        {
            var user = await _database.GetUserByEmailAsync(Email);
            if (user == null || user.Password != Password)
            {
                await Application.Current!.MainPage!.DisplayAlert("Помилка входу", "Невірна пошта або пароль.", "ОК");
                return;
            }

            Preferences.Set("UserName", user.FullName);
            Preferences.Set("UserEmail", user.Email);
            OpenMainShell();
        }
        else
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                await Application.Current!.MainPage!.DisplayAlert("Увага", "Вкажіть ваше ім'я та прізвище.", "ОК");
                return;
            }

            if (Password.Length < 6)
            {
                await Application.Current!.MainPage!.DisplayAlert("Увага", "Пароль має містити не менше 6 символів.", "ОК");
                return;
            }

            if (Password != ConfirmPassword)
            {
                await Application.Current!.MainPage!.DisplayAlert("Увага", "Введені паролі не збігаються.", "ОК");
                return;
            }

            var existing = await _database.GetUserByEmailAsync(Email);
            if (existing != null)
            {
                await Application.Current!.MainPage!.DisplayAlert("Увага", "Ця електронна пошта вже використовується.", "ОК");
                return;
            }

            var newUser = new User
            {
                FullName = FullName.Trim(),
                Email = Email.Trim(),
                Password = Password
            };

            await _database.RegisterUserAsync(newUser);
            Preferences.Set("UserName", newUser.FullName);
            Preferences.Set("UserEmail", newUser.Email);

            OpenMainShell();
        }
    }

    private void OpenMainShell()
    {
        var shell = new AppShell();
        shell.RefreshUserHeader();
        Application.Current!.MainPage = shell;
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}