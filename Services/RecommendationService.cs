using AutoLog.Models;

namespace AutoLog.Services;

public class RecommendationService
{
    public List<string> GetEngineRecommendations(Vehicle vehicle)
    {
        var tips = new List<string>();

        if (vehicle == null) return tips;

        if (vehicle.HasTurbo)
        {
            tips.Add("✦ Турбонагнітач: для уникнення детонації та збереження ресурсу турбіни рекомендовано високооктанове пальне (А-95+ / А-98).");
        }

        if (vehicle.HasDirectInjection)
        {
            tips.Add("✦ Пряме впорскування (GDI/TSI): клапани не омиваються паливом. Кожні 3–4 баки використовуйте пальне з мийними присадками (Pulls, V-Power, Mustang).");
        }

        if (vehicle.HasDpf)
        {
            tips.Add("✦ Сажовий фільтр (DPF): чутливий до вмісту сірки. Заправляйте виключно дизельне пальне стандарту Євро-5 та вище.");
        }

        if (tips.Count == 0)
        {
            tips.Add("✦ Атмосферний / базовий двигун: специфічних обмежень немає. Рекомендовано сертифіковане пальне згідно з паспортом авто.");
        }

        return tips;
    }
}