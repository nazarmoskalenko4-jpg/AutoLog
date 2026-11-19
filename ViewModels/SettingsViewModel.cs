using System.ComponentModel;
using System.Windows.Input;

namespace AutoLog.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string UserName
    {
        get => Preferences.Get("UserName", "");
        set { Preferences.Set("UserName", value); OnPropertyChanged(nameof(UserName)); }
    }

    public string Currency
    {
        get => Preferences.Get("Currency", "грн");
        set { Preferences.Set("Currency", value); OnPropertyChanged(nameof(Currency)); }
    }

    public string DistanceUnit
    {
        get => Preferences.Get("DistanceUnit", "км");
        set { Preferences.Set("DistanceUnit", value); OnPropertyChanged(nameof(DistanceUnit)); }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}