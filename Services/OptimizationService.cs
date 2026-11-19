using AutoLog.Models;

namespace AutoLog.Services;

public class OptimizationService
{
    /// <summary>
    /// Розраховує економічну доцільність поїздки на дешевшу АЗС.
    /// </summary>
    /// <returns>Чисту вигоду (в гривнях). Якщо число від'ємне — поїздка збиткова.</returns>
    public double CalculateNetBenefit(
        Vehicle vehicle, 
        double refillVolume, // Скільки літрів/кВт плануємо заправити
        double currentStationPrice, // Ціна на найближчій АЗС
        double targetStationPrice, // Ціна на віддаленій (дешевшій) АЗС
        double distanceToTargetKm) // Відстань до віддаленої АЗС (в один бік)
    {
        // 1. Скільки ми зекономимо на самій різниці в ціні
        double priceDifference = currentStationPrice - targetStationPrice;
        double grossSavings = priceDifference * refillVolume;

        // 2. Скільки пального/енергії ми витратимо на дорогу ТУДИ і НАЗАД
        double totalDistanceKm = distanceToTargetKm * 2;
        double fuelSpentOnTrip = (vehicle.FuelConsumption / 100) * totalDistanceKm;

        // 3. Вартість спаленого пального (рахуємо по ціні дешевшої АЗС, де ми заправимось)
        double costOfTrip = fuelSpentOnTrip * targetStationPrice;

        // 4. Чиста вигода
        double netBenefit = grossSavings - costOfTrip;

        return Math.Round(netBenefit, 2);
    }
}