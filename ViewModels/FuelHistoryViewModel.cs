using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using AutoLog.Models;
using AutoLog.Services;

namespace AutoLog.ViewModels;

[QueryProperty(nameof(Vehicle), "Vehicle")]
public class FuelHistoryViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabase _database;
    private readonly RecommendationService _recommendationService;
    private Vehicle _vehicle;

    public Vehicle Vehicle 
    { 
        get => _vehicle; 
        set 
        { 
            _vehicle = value; 
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Vehicle)));
            LoadRecordsCommand.Execute(null); 
        
            // Генеруємо поради:
            var tips = _recommendationService.GetEngineRecommendations(_vehicle);
            Recommendations.Clear();
            foreach (var tip in tips) Recommendations.Add(tip);
        } 
    }

    public ObservableCollection<FuelRecord> Records { get; set; } = new();
    public ObservableCollection<string> Recommendations { get; set; } = new();
    
    public ICommand LoadRecordsCommand { get; }
    public ICommand AddRecordCommand { get; }
    public ICommand DeleteRecordCommand { get; }
    public ICommand EditRecordCommand { get; }

    public event PropertyChangedEventHandler PropertyChanged;

   public FuelHistoryViewModel(LocalDatabase database, RecommendationService recommendationService)
{
    _database = database;
    _recommendationService = recommendationService;
    
    LoadRecordsCommand = new Command(async () => await LoadRecords());
    AddRecordCommand = new Command(async () => 
{
    try 
    {
        await Shell.Current.GoToAsync($"AddFuelRecordPage?VehicleId={Vehicle.Id}");
    }
    catch (Exception ex)
    {
        // Тепер програма виведе вікно з помилкою замість того, щоб намертво зависнути
        await Shell.Current.DisplayAlert("Помилка навігації", ex.Message, "ОК");
    }
});
    DeleteRecordCommand = new Command<FuelRecord>(async (r) => await DeleteRecord(r));
    EditRecordCommand = new Command<FuelRecord>(async (r) => 
{
    if (r == null) return;
    var navParam = new Dictionary<string, object> { { "Record", r } };
    await Shell.Current.GoToAsync("AddFuelRecordPage", navParam);
});
}

private async Task LoadRecords()
    {
        if (Vehicle == null) return;
        var list = await _database.GetFuelRecordsAsync(Vehicle.Id);

        // 1. Сортуємо чеки за одометром (від найменшого до найбільшого пробігу)
        var sortedList = list.OrderBy(r => r.Odometer).ThenBy(r => r.Id).ToList();

        // Скидаємо попередні розрахунки
        foreach (var r in sortedList)
            r.CalculatedConsumption = null;

        // 2. Правильний алгоритм "від повного до повного" з урахуванням проміжних дозаправок
        int? lastFullOdometer = null;
        double accumulatedFuel = 0;

        foreach (var curr in sortedList)
        {
            if (lastFullOdometer == null)
            {
                // Чекаємо першої заправки "до повного", щоб мати точку відліку
                if (curr.IsFullFill)
                {
                    lastFullOdometer = curr.Odometer;
                    accumulatedFuel = 0;
                }
            }
            else
            {
                // Додаємо літри/кВт кожної заправки після попереднього повного бака
                accumulatedFuel += curr.Quantity;

                if (curr.IsFullFill)
                {
                    double distance = curr.Odometer - lastFullOdometer.Value;
                    if (distance > 0)
                    {
                        double rawConsumption = (accumulatedFuel / distance) * 100.0;
                        curr.CalculatedConsumption = Math.Round(rawConsumption, 1);
                    }

                    // Оновлюємо точку відліку для наступного циклу
                    lastFullOdometer = curr.Odometer;
                    accumulatedFuel = 0;
                }
            }
        }

        // 3. Виводимо на екран (найновіші за пробігом зверху)
        Records.Clear();
        foreach (var item in sortedList.OrderByDescending(r => r.Odometer).ThenByDescending(r => r.Id))
        {
            Records.Add(item);
        }
    }

 private async Task DeleteRecord(FuelRecord record)
{
    if (record == null) return;
    bool answer = await Shell.Current.DisplayAlert("Підтвердження", "Ви дійсно хочете видалити цей чек?", "Так", "Ні");
    if (answer)
    {
        await _database.DeleteFuelRecordAsync(record);
        Records.Remove(record); // Миттєво прибираємо з екрану
    }
}

}