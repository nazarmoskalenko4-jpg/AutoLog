using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Models;
using AutoLog.Services;

namespace AutoLog.ViewModels;

[QueryProperty(nameof(EditingVehicle), "Vehicle")]
public class AddVehicleViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabase _database;
    private readonly CarCatalogService _catalogService;
    private int _vehicleId;
    private List<CarJsonData> _allCars = new();

    public ObservableCollection<string> AvailableBrands { get; } = new();
    public ObservableCollection<CarJsonData> AvailableModels { get; } = new();

    // Керування кастомними випадаючими списками
    private bool _isBrandDropdownOpen;
    public bool IsBrandDropdownOpen
    {
        get => _isBrandDropdownOpen;
        set { _isBrandDropdownOpen = value; OnPropertyChanged(nameof(IsBrandDropdownOpen)); }
    }

    private bool _isModelDropdownOpen;
    public bool IsModelDropdownOpen
    {
        get => _isModelDropdownOpen;
        set { _isModelDropdownOpen = value; OnPropertyChanged(nameof(IsModelDropdownOpen)); }
    }

    private string _selectedCategory = "Усі";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            _selectedCategory = value;
            OnPropertyChanged(nameof(SelectedCategory));
            UpdateCategoryColors();
            UpdateBrandsList();
        }
    }

    public string AllChipBg => SelectedCategory == "Усі" ? "#3B82F6" : "#252A3A";
    public string EvChipBg => SelectedCategory == "Електро" ? "#10B981" : "#252A3A";
    public string HybridChipBg => SelectedCategory == "Гібрид" ? "#8B5CF6" : "#252A3A";
    public string PetrolChipBg => SelectedCategory == "Бензин" ? "#F59E0B" : "#252A3A";
    public string DieselChipBg => SelectedCategory == "Дизель" ? "#EC4899" : "#252A3A";

    private string? _selectedBrand;
    public string? SelectedBrand
    {
        get => _selectedBrand;
        set
        {
            _selectedBrand = value;
            OnPropertyChanged(nameof(SelectedBrand));
            OnPropertyChanged(nameof(SelectedBrandDisplay));
            UpdateModelsList();
        }
    }

    public string SelectedBrandDisplay => string.IsNullOrEmpty(SelectedBrand) ? "Оберіть марку зі списку..." : SelectedBrand;

    private CarJsonData? _selectedPresetCar;
    public CarJsonData? SelectedPresetCar
    {
        get => _selectedPresetCar;
        set
        {
            _selectedPresetCar = value;
            OnPropertyChanged(nameof(SelectedPresetCar));
            OnPropertyChanged(nameof(SelectedModelDisplay));
            if (value != null)
            {
                _catalogService.ApplyTemplateToVehicle(value, this);
                NotifyAllProperties();
            }
        }
    }

    public string SelectedModelDisplay => SelectedPresetCar == null ? "Оберіть модель та двигун..." : SelectedPresetCar.ModelDisplayName;

    public string? Make { get; set; }
    public string? Model { get; set; }
    public int Year { get; set; } = 2020;
    public string? EngineType { get; set; } = "ICE (ДВЗ)";
    public string? FuelType { get; set; } = "Petrol (Бензин)";
    public double FuelConsumption { get; set; } = 7.5;

    public bool HasTurbo { get; set; }
    public bool HasDirectInjection { get; set; }
    public bool HasDpf { get; set; }

    public string CounterText => $"Доступно в базі: {AvailableBrands.Count} марок • {AvailableModels.Count} модифікацій";

    public Vehicle? EditingVehicle
    {
        set
        {
            if (value != null)
            {
                _vehicleId = value.Id;
                Make = value.Make;
                Model = value.Model;
                Year = value.Year;
                EngineType = value.EngineType;
                FuelType = value.FuelType;
                FuelConsumption = value.FuelConsumption;
                HasTurbo = value.HasTurbo;
                HasDirectInjection = value.HasDirectInjection;
                HasDpf = value.HasDpf;
                NotifyAllProperties();
            }
        }
    }

    public ICommand SelectCategoryCommand { get; }
    public ICommand ToggleBrandDropdownCommand { get; }
    public ICommand SelectBrandItemCommand { get; }
    public ICommand ToggleModelDropdownCommand { get; }
    public ICommand SelectModelItemCommand { get; }
    public ICommand SaveCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AddVehicleViewModel(LocalDatabase database, CarCatalogService catalogService)
    {
        _database = database;
        _catalogService = catalogService;

        SelectCategoryCommand = new Command<string>((cat) => SelectedCategory = cat);
        
        ToggleBrandDropdownCommand = new Command(() =>
        {
            IsBrandDropdownOpen = !IsBrandDropdownOpen;
            if (IsBrandDropdownOpen) IsModelDropdownOpen = false;
        });

        SelectBrandItemCommand = new Command<string>((brand) =>
        {
            SelectedBrand = brand;
            IsBrandDropdownOpen = false;
        });

        ToggleModelDropdownCommand = new Command(() =>
        {
            if (AvailableModels.Count == 0)
            {
                Shell.Current.DisplayAlert("Увага", "Спочатку оберіть марку авто у Кроці 2.", "ОК");
                return;
            }
            IsModelDropdownOpen = !IsModelDropdownOpen;
            if (IsModelDropdownOpen) IsBrandDropdownOpen = false;
        });

        SelectModelItemCommand = new Command<CarJsonData>((car) =>
        {
            SelectedPresetCar = car;
            IsModelDropdownOpen = false;
        });

        SaveCommand = new Command(async () => await SaveVehicle());

        _ = LoadCatalogAsync();
    }

    private async Task LoadCatalogAsync()
    {
        _allCars = await _catalogService.GetCatalogAsync();
        UpdateBrandsList();
    }

    private void UpdateBrandsList()
    {
        var filtered = SelectedCategory == "Усі"
            ? _allCars
            : _allCars.Where(c => c.Category == SelectedCategory);

        var brands = filtered
            .Select(c => c.FormattedBrand)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        AvailableBrands.Clear();
        foreach (var b in brands)
            AvailableBrands.Add(b);

        if (SelectedCategory == "Електро") { EngineType = "EV (Електро)"; FuelType = "Electricity (Електрика)"; }
        else if (SelectedCategory == "Гібрид") { EngineType = "Hybrid (Гібрид)"; FuelType = "Petrol (Бензин)"; }
        else if (SelectedCategory == "Дизель") { EngineType = "ICE (ДВЗ)"; FuelType = "Diesel (Дизель)"; }
        else if (SelectedCategory == "Бензин") { EngineType = "ICE (ДВЗ)"; FuelType = "Petrol (Бензин)"; }

        SelectedBrand = null;
        SelectedPresetCar = null;
        AvailableModels.Clear();
        IsBrandDropdownOpen = false;
        IsModelDropdownOpen = false;
        OnPropertyChanged(nameof(CounterText));
        NotifyAllProperties();
    }

    private void UpdateModelsList()
    {
        AvailableModels.Clear();
        SelectedPresetCar = null;

        if (!string.IsNullOrEmpty(SelectedBrand))
        {
            Make = SelectedBrand;
            OnPropertyChanged(nameof(Make));

            var models = _allCars.Where(c =>
                c.FormattedBrand == SelectedBrand &&
                (SelectedCategory == "Усі" || c.Category == SelectedCategory))
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var m in models)
                AvailableModels.Add(m);
        }

        OnPropertyChanged(nameof(CounterText));
    }

    private void UpdateCategoryColors()
    {
        OnPropertyChanged(nameof(AllChipBg));
        OnPropertyChanged(nameof(EvChipBg));
        OnPropertyChanged(nameof(HybridChipBg));
        OnPropertyChanged(nameof(PetrolChipBg));
        OnPropertyChanged(nameof(DieselChipBg));
    }

    private void NotifyAllProperties()
    {
        OnPropertyChanged(nameof(Make));
        OnPropertyChanged(nameof(Model));
        OnPropertyChanged(nameof(Year));
        OnPropertyChanged(nameof(EngineType));
        OnPropertyChanged(nameof(FuelType));
        OnPropertyChanged(nameof(FuelConsumption));
        OnPropertyChanged(nameof(HasTurbo));
        OnPropertyChanged(nameof(HasDirectInjection));
        OnPropertyChanged(nameof(HasDpf));
    }

    private async Task SaveVehicle()
    {
        if (string.IsNullOrWhiteSpace(Make) || string.IsNullOrWhiteSpace(Model))
        {
            await Shell.Current.DisplayAlert("Увага", "Будь ласка, оберіть марку та модель зі списку або введіть їх вручну.", "ОК");
            return;
        }

        if (Year < 1950 || Year > DateTime.Now.Year + 1)
        {
            await Shell.Current.DisplayAlert("Помилка вводу", $"Введіть коректний рік випуску (від 1950 до {DateTime.Now.Year}).", "ОК");
            return;
        }

        var vehicle = new Vehicle
        {
            Id = _vehicleId,
            Make = Make.Trim(),
            Model = Model.Trim(),
            Year = Year,
            EngineType = EngineType ?? "ICE (ДВЗ)",
            FuelType = FuelType ?? "Petrol (Бензин)",
            FuelConsumption = FuelConsumption > 0 ? FuelConsumption : 8.0,
            HasTurbo = HasTurbo,
            HasDirectInjection = HasDirectInjection,
            HasDpf = HasDpf
        };

        await _database.SaveVehicleAsync(vehicle);
        await Shell.Current.GoToAsync("..");
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}