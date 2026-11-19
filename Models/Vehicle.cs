using SQLite;

namespace AutoLog.Models;

public class Vehicle
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    
    public string Make { get; set; } // Марка (напр., Mazda)
    public string Model { get; set; } // Модель (напр., 6)
    public int Year { get; set; } // Рік випуску
    
    public string EngineType { get; set; } // "ICE" (ДВЗ), "EV" (Електро), "Hybrid"
    public string FuelType { get; set; } // "Petrol", "Diesel", "Electricity"
    
    // Для електромобілів
    public double? BatteryCapacity { get; set; } // Місткість батареї в кВт·год

    // Середня витрата (л/100км для ДВЗ, або кВт·год/100км для Електро)
    public double FuelConsumption { get; set; }

    // Характеристики для експертної системи
    public bool HasTurbo { get; set; } // Турбіна (потребує вищого октану)
    public bool HasDirectInjection { get; set; } // Пряме впорскування GDI/TSI (потребує мийних присадок)
    public bool HasDpf { get; set; } // Сажовий фільтр для дизеля

    [Ignore]
    public string VehicleIcon => (EngineType != null && EngineType.Contains("EV")) ? "⚡" 
                               : (EngineType != null && EngineType.Contains("Hybrid")) ? "❖" 
                               : "◈";

    [Ignore]
    public string SpecsSummary
    {
        get
        {
            var tags = new List<string>();
            if (!string.IsNullOrEmpty(FuelType)) tags.Add(FuelType);
            if (HasTurbo) tags.Add("Turbo");
            if (HasDirectInjection) tags.Add("GDI/TSI");
            if (HasDpf) tags.Add("DPF");
            return string.Join(" • ", tags);
        }
    }
}