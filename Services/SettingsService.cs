namespace AutoLog.Services;

public class SettingsService
{
    // 1 миля = 1.60934 км
    private const double MilesToKmRatio = 1.60934;

    public string Currency => Preferences.Get("Currency", "грн");
    public string DistanceUnit => Preferences.Get("DistanceUnit", "км");

    // Якщо користувач обрав милі, ми множимо відстань на 1.6, щоб зберегти в базу кілометри
    public double ConvertToKilometers(double inputDistance)
    {
        if (DistanceUnit == "милі")
            return inputDistance * MilesToKmRatio;
        
        return inputDistance; // Якщо вже вибрано км, повертаємо як є
    }

    // При виведенні на екран: якщо користувач хоче милі, ділимо кілометри на 1.6
    public double ConvertFromKilometers(double dbKilometers)
    {
        if (DistanceUnit == "милі")
            return dbKilometers / MilesToKmRatio;
        
        return dbKilometers;
    }
}