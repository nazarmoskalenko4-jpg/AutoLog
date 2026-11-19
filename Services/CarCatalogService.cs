using System.Text.Json;
using System.Text.Json.Serialization;
using AutoLog.Models;
using AutoLog.ViewModels;

namespace AutoLog.Services;

public class CarJsonItem
{
    [JsonPropertyName("data")]
    public CarJsonData? Data { get; set; }
}

public class CarJsonData
{
    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("generation")]
    public string? Generation { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("specs")]
    public CarJsonSpecs? Specs { get; set; }

    // Форматована марка (напр., "Volkswagen", "Tesla")
    public string FormattedBrand => string.IsNullOrEmpty(Brand) 
        ? "Невідомо" 
        : char.ToUpper(Brand[0]) + Brand.Substring(1).ToLower();

    // Назва моделі для 3-го кроку (без дублювання бренду)
    public string ModelDisplayName => $"{Name} [{Generation?.ToUpper()}] ({Year} р.)";

    // Автоматичне визначення категорії авто з JSON
    public string Category
    {
        get
        {
            string raw = Specs?.FuelType?.Value?.ToLower() ?? "";
            if (raw.Contains("electric")) return "Електро";
            if (raw.Contains("hybrid")) return "Гібрид";
            if (raw.Contains("diesel")) return "Дизель";
            return "Бензин";
        }
    }
}

public class CarJsonSpecs
{
    [JsonPropertyName("fuel_type")]
    public SpecItem<string>? FuelType { get; set; }

    [JsonPropertyName("cons_combined")]
    public SpecItem<double>? ConsCombined { get; set; }

    [JsonPropertyName("power_hp")]
    public SpecItem<double>? PowerHp { get; set; }
}

public class SpecItem<T>
{
    [JsonPropertyName("v")]
    public T? Value { get; set; }
}

public class CarCatalogService
{
    private List<CarJsonData> _catalog = new();

    public async Task<List<CarJsonData>> GetCatalogAsync()
    {
        if (_catalog.Count > 0) return _catalog;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("cars_catalog.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var items = JsonSerializer.Deserialize<List<CarJsonItem>>(json);
            if (items != null)
            {
                _catalog = items.Where(x => x.Data != null).Select(x => x.Data!).ToList();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка читання каталогу: {ex.Message}");
        }

        return _catalog;
    }

    public void ApplyTemplateToVehicle(CarJsonData preset, AddVehicleViewModel vm)
    {
        if (preset == null) return;

        vm.Make = preset.FormattedBrand;
        vm.Model = $"{preset.Name} ({preset.Generation?.ToUpper()})";
        vm.Year = preset.Year;

        string rawFuel = preset.Specs?.FuelType?.Value?.ToLower() ?? "";
        string nameLower = preset.Name?.ToLower() ?? "";

        if (rawFuel.Contains("electric"))
        {
            vm.EngineType = "EV (Електро)";
            vm.FuelType = "Electricity (Електрика)";
        }
        else if (rawFuel.Contains("hybrid"))
        {
            vm.EngineType = "Hybrid (Гібрид)";
            vm.FuelType = "Petrol (Бензин)";
        }
        else if (rawFuel.Contains("diesel"))
        {
            vm.EngineType = "ICE (ДВЗ)";
            vm.FuelType = "Diesel (Дизель)";
        }
        else
        {
            vm.EngineType = "ICE (ДВЗ)";
            vm.FuelType = "Petrol (Бензин)";
        }

        if (preset.Specs?.ConsCombined?.Value > 0)
        {
            vm.FuelConsumption = preset.Specs.ConsCombined.Value;
        }

        vm.HasTurbo = nameLower.Contains("tsi") || nameLower.Contains("tfsi") || 
                      nameLower.Contains("tdi") || nameLower.Contains("dci") || 
                      nameLower.Contains("ecoboost") || nameLower.Contains("tce") || 
                      nameLower.Contains("turbo") || rawFuel.Contains("diesel");

        vm.HasDirectInjection = nameLower.Contains("tsi") || nameLower.Contains("tfsi") || 
                                nameLower.Contains("gdi") || nameLower.Contains("fsi") || 
                                nameLower.Contains("skyactiv");

        vm.HasDpf = rawFuel.Contains("diesel") && preset.Year >= 2009;
    }
}