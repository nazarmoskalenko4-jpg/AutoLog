using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Models;
using AutoLog.Services;

namespace AutoLog.ViewModels;

[QueryProperty(nameof(VehicleId), "VehicleId")]
[QueryProperty(nameof(EditingRecord), "Record")]
public class AddFuelRecordViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabase _database;
    private int _vehicleId;
    private int _recordId;

    private static readonly string[] TopStations = { "UPG", "OKKO", "WOG", "Укрнафта" };

    public int VehicleId
    {
        get => _vehicleId;
        set { _vehicleId = value; OnPropertyChanged(nameof(VehicleId)); }
    }

    private string _selectedStationOption = "UPG";
    public string SelectedStationOption
    {
        get => _selectedStationOption;
        set
        {
            _selectedStationOption = value;
            OnPropertyChanged(nameof(SelectedStationOption));
            OnPropertyChanged(nameof(IsCustomStation));
            OnPropertyChanged(nameof(CanRateStation));
            OnPropertyChanged(nameof(UpgBg));
            OnPropertyChanged(nameof(OkkoBg));
            OnPropertyChanged(nameof(WogBg));
            OnPropertyChanged(nameof(UkrnaftaBg));
            OnPropertyChanged(nameof(CustomBg));
        }
    }

    // Якщо вибрано "Своя АЗС" — показуємо поле вводу, але ХОВАЄМО оцінку зірками
    public bool IsCustomStation => SelectedStationOption == "Своя АЗС";
    public bool CanRateStation => !IsCustomStation;

    public string UpgBg => SelectedStationOption == "UPG" ? "#10B981" : "#313B54";
    public string OkkoBg => SelectedStationOption == "OKKO" ? "#22C55E" : "#313B54";
    public string WogBg => SelectedStationOption == "WOG" ? "#84CC16" : "#313B54";
    public string UkrnaftaBg => SelectedStationOption == "Укрнафта" ? "#3B82F6" : "#313B54";
    public string CustomBg => SelectedStationOption == "Своя АЗС" ? "#8B5CF6" : "#313B54";

    public string? CustomStationName { get; set; }
    public string? OdometerText { get; set; }
    public string? QuantityText { get; set; }
    public string? PricePerUnitText { get; set; }
    public bool IsFullFill { get; set; }

    private int _currentRating = 5;
    public int CurrentRating
    {
        get => _currentRating;
        set
        {
            _currentRating = value;
            OnPropertyChanged(nameof(CurrentRating));
            OnPropertyChanged(nameof(Star1));
            OnPropertyChanged(nameof(Star2));
            OnPropertyChanged(nameof(Star3));
            OnPropertyChanged(nameof(Star4));
            OnPropertyChanged(nameof(Star5));
            OnPropertyChanged(nameof(RatingDescription));
        }
    }

    public string Star1 => CurrentRating >= 1 ? "★" : "☆";
    public string Star2 => CurrentRating >= 2 ? "★" : "☆";
    public string Star3 => CurrentRating >= 3 ? "★" : "☆";
    public string Star4 => CurrentRating >= 4 ? "★" : "☆";
    public string Star5 => CurrentRating >= 5 ? "★" : "☆";

    public string RatingDescription => CurrentRating switch
    {
        1 => "1/5 — Жахливо, авто втрачає тягу",
        2 => "2/5 — Погано",
        3 => "3/5 — Нормально",
        4 => "4/5 — Добре",
        5 => "5/5 — Відмінне пальне!",
        _ => ""
    };

    public FuelRecord? EditingRecord
    {
        set
        {
            if (value != null)
            {
                _recordId = value.Id;
                VehicleId = value.VehicleId;

                var matchedTop = TopStations.FirstOrDefault(s =>
                    string.Equals(s, value.StationName?.Trim(), StringComparison.OrdinalIgnoreCase));

                if (matchedTop != null)
                {
                    SelectedStationOption = matchedTop;
                    CurrentRating = value.QualityRating > 0 ? value.QualityRating : 5;
                }
                else
                {
                    SelectedStationOption = "Своя АЗС";
                    CustomStationName = value.StationName;
                    OnPropertyChanged(nameof(CustomStationName));
                }

                OdometerText = value.Odometer.ToString();
                QuantityText = value.Quantity.ToString();
                PricePerUnitText = value.PricePerUnit.ToString();
                IsFullFill = value.IsFullFill;

                OnPropertyChanged(nameof(OdometerText));
                OnPropertyChanged(nameof(QuantityText));
                OnPropertyChanged(nameof(PricePerUnitText));
                OnPropertyChanged(nameof(IsFullFill));
            }
        }
    }

    public ICommand SelectStationCommand { get; }
    public ICommand SetRatingCommand { get; }
    public ICommand SaveCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public AddFuelRecordViewModel(LocalDatabase database)
    {
        _database = database;
        SelectStationCommand = new Command<string>((station) => SelectedStationOption = station);
        SetRatingCommand = new Command<string>((ratingStr) =>
        {
            if (int.TryParse(ratingStr, out int rating))
                CurrentRating = rating;
        });
        SaveCommand = new Command(async () => await SaveRecord());
    }

    private async Task SaveRecord()
    {
        string finalStationName = IsCustomStation
            ? (CustomStationName ?? "").Trim()
            : SelectedStationOption;

        if (string.IsNullOrWhiteSpace(finalStationName))
        {
            await Shell.Current.DisplayAlert("Помилка вводу", "Введіть назву вашої АЗС або оберіть одну з популярних мереж.", "ОК");
            return;
        }

        string cleanQuantity = (QuantityText ?? "").Replace(',', '.');
        string cleanPrice = (PricePerUnitText ?? "").Replace(',', '.');

        if (!int.TryParse(OdometerText, out int odometer) || odometer < 0)
        {
            await Shell.Current.DisplayAlert("Помилка вводу", "Введіть коректний пробіг (ціле додатне число).", "ОК");
            return;
        }

        if (!double.TryParse(cleanQuantity, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double quantity) || quantity <= 0)
        {
            await Shell.Current.DisplayAlert("Помилка вводу", "Введіть коректну кількість літрів або кВт·год (більше 0).", "ОК");
            return;
        }

        if (!double.TryParse(cleanPrice, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double price) || price <= 0)
        {
            await Shell.Current.DisplayAlert("Помилка вводу", "Введіть коректну ціну за одиницю (більше 0).", "ОК");
            return;
        }

        var record = new FuelRecord
        {
            Id = _recordId,
            VehicleId = VehicleId,
            Date = DateTime.Now,
            StationName = finalStationName,
            Odometer = odometer,
            Quantity = quantity,
            PricePerUnit = price,
            TotalCost = Math.Round(quantity * price, 2),
            IsFullFill = IsFullFill,
            // Для власної АЗС оцінка не зберігається (0)
            QualityRating = CanRateStation ? CurrentRating : 0
        };

        await _database.SaveFuelRecordAsync(record);
        await Shell.Current.GoToAsync("..");
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}