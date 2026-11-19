using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Models;
using AutoLog.Services;
using AutoLog.Views;

namespace AutoLog.ViewModels;

public class VehiclesViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabase _database;
    private readonly FuelMarketService _fuelMarketService;

    public ObservableCollection<Vehicle> Vehicles { get; set; } = new();
    public ObservableCollection<StationWidgetInfo> FuelStations { get; set; } = new();

    private string _pricesStatusText = "Оновлення цін...";
    public string PricesStatusText
    {
        get => _pricesStatusText;
        set { _pricesStatusText = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PricesStatusText))); }
    }

    public string WelcomeMessage
    {
        get
        {
            var name = Preferences.Get("UserName", "");
            return string.IsNullOrWhiteSpace(name) ? "Ваш розумний гараж" : $"Вітаємо, {name}";
        }
    }

    public int TotalVehiclesCount => Vehicles.Count;
    public int EvCount => Vehicles.Count(v => v.EngineType != null && v.EngineType.Contains("EV"));
    public int IceAndHybridCount => Vehicles.Count - EvCount;
    public string AverageFleetConsumption => Vehicles.Count > 0
        ? $"{Math.Round(Vehicles.Average(v => v.FuelConsumption > 0 ? v.FuelConsumption : 7.8), 1)} л/кВт"
        : "0.0";

    public ICommand LoadDataCommand { get; }
    public ICommand AddVehicleCommand { get; }
    public ICommand VehicleSelectedCommand { get; }
    public ICommand DeleteVehicleCommand { get; }
    public ICommand EditVehicleCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public VehiclesViewModel(LocalDatabase database, FuelMarketService fuelMarketService)
    {
        _database = database;
        _fuelMarketService = fuelMarketService;

        LoadDataCommand = new Command(async () => await LoadDashboardData());
        AddVehicleCommand = new Command(async () => await Shell.Current.GoToAsync(nameof(AddVehiclePage)));
        VehicleSelectedCommand = new Command<Vehicle>(async (v) => await GoToFuelHistory(v));
        DeleteVehicleCommand = new Command<Vehicle>(async (v) => await DeleteVehicle(v));
        EditVehicleCommand = new Command<Vehicle>(async (v) =>
        {
            if (v == null) return;
            var navParam = new Dictionary<string, object> { { "Vehicle", v } };
            await Shell.Current.GoToAsync(nameof(AddVehiclePage), navParam);
        });
    }

    private async Task LoadDashboardData()
    {
        // 1. Завантажуємо авто користувача
        var list = await _database.GetVehiclesAsync();
        Vehicles.Clear();
        foreach (var v in list)
            Vehicles.Add(v);

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WelcomeMessage)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalVehiclesCount)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EvCount)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IceAndHybridCount)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AverageFleetConsumption)));

        // 2. Завантажуємо віджети Топ-4 АЗС (ціни + оновлені рейтинги)
        var stations = await _fuelMarketService.GetStationsDashboardAsync(_database);
        FuelStations.Clear();
        foreach (var st in stations)
            FuelStations.Add(st);

        PricesStatusText = _fuelMarketService.IsLiveDataLoaded
          ? "● Live ціни (index.minfin.com.ua)"
          : "● Немає зв'язку з сервером цін";
    }

    private async Task GoToFuelHistory(Vehicle vehicle)
    {
        if (vehicle == null) return;
        var navigationParameter = new Dictionary<string, object> { { "Vehicle", vehicle } };
        await Shell.Current.GoToAsync("FuelHistoryPage", navigationParameter);
    }

    private async Task DeleteVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;
        bool answer = await Shell.Current.DisplayAlert("Підтвердження", $"Ви дійсно хочете видалити {vehicle.Make} {vehicle.Model}?", "Так", "Ні");
        if (answer)
        {
            await _database.DeleteVehicleAsync(vehicle);
            await LoadDashboardData();
        }
    }
}