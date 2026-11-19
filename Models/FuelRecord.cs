using SQLite;

namespace AutoLog.Models;

public class FuelRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public DateTime Date { get; set; }
    public string StationName { get; set; }
    
    // У базі завжди зберігаються базові значення (в км та грн)
    public int Odometer { get; set; }
    public double Quantity { get; set; }
    public double PricePerUnit { get; set; }
    public double TotalCost { get; set; }
    public bool IsFullFill { get; set; }
    public int QualityRating { get; set; }

    [Ignore]
    public double? CalculatedConsumption { get; set; }

    [Ignore]
    public string QualityStars => QualityRating switch
    {
        1 => "★☆☆☆☆",
        2 => "★★☆☆☆",
        3 => "★★★☆☆",
        4 => "★★★★☆",
        5 => "★★★★★",
        _ => "★★★★★"
    };

    // Поточні налаштування з профілю
    [Ignore]
    public string Currency => Preferences.Get("Currency", "грн");

    [Ignore]
    public string DistanceUnit => Preferences.Get("DistanceUnit", "км");

    // Курс валют відносно базової гривні (можна змінити або пізніше винести в налаштування)
    private double CurrencyRate => Currency switch
    {
        "$ (USD)" => 41.5,
        "€ (EUR)" => 45.0,
        _ => 1.0 // грн
    };

    // 1. Перерахунок пробігу (км -> милі)
    [Ignore]
    public int DisplayOdometer => DistanceUnit == "милі" 
        ? (int)Math.Round(Odometer / 1.60934) 
        : Odometer;

    // 2. Перерахунок ціни за літр/кВт (грн -> валюта)
    [Ignore]
    public double DisplayPricePerUnit => Math.Round(PricePerUnit / CurrencyRate, 2);

    // 3. Перерахунок загальної суми чека (грн -> валюта)
    [Ignore]
    public double DisplayTotalCost => Math.Round(TotalCost / CurrencyRate, 2);

    // 4. Перерахунок реальної витрати (на 100 км -> на 100 миль)
    [Ignore]
    public double? DisplayConsumption
    {
        get
        {
            if (!CalculatedConsumption.HasValue) return null;
            // На 100 миль авто проїжджає в 1.609 раза більшу дистанцію, тому витрачає більше літрів
            if (DistanceUnit == "милі")
                return Math.Round(CalculatedConsumption.Value * 1.60934, 1);
            return CalculatedConsumption;
        }
    }

    [Ignore]
    public string ConsumptionLabel => DistanceUnit == "милі" ? "л/100 миль" : "л/100 км";
}