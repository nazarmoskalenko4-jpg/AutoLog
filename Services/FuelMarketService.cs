using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoLog.Services;

public class FuelPriceItem
{
    public string FuelName { get; set; } = string.Empty;
    public string PriceDisplay { get; set; } = string.Empty;
}

public class StationWidgetInfo
{
    public string Name { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#3B82F6";
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public List<FuelPriceItem> Prices { get; set; } = new();

    public string StarsDisplay
    {
        get
        {
            int filled = (int)Math.Round(AverageRating, MidpointRounding.AwayFromZero);
            filled = Math.Clamp(filled, 0, 5);
            return new string('★', filled) + new string('☆', 5 - filled);
        }
    }

    public string RatingSummary => RatingCount > 0 ? $"{AverageRating:F1} ({RatingCount})" : "Тарифи України";
}

/// <summary>Ціна одного виду пального в гривнях (так зберігаємо в кеші).</summary>
public class FuelPrice
{
    public string Fuel { get; set; } = string.Empty;
    public double Uah { get; set; }
}

public class FuelMarketService
{
    private const string FuelUrl = "https://index.minfin.com.ua/ua/markets/fuel/tm/";
    private const string EvUrl = "https://index.minfin.com.ua/ua/tariff/electric/";
    private const string FuelCacheKey = "FuelPricesCache";
    private const string FuelCacheTimeKey = "FuelPricesCacheTime";

    private const RegexOptions Opts = RegexOptions.Singleline | RegexOptions.IgnoreCase;
    private static readonly HttpClient Http = CreateClient();

    // Які види пального показуємо у віджеті (порядок = порядок рядків)
    private static readonly string[] ShownFuels = { "А-95", "А-95+", "ДП", "Газ" };

    private static readonly (string Name, string Color, int BaseSum, int BaseCount, string[] Aliases)[] Stations =
    {
        ("UPG",      "#10B981", 456, 96,  new[] { "upg" }),
        ("OKKO",     "#22C55E", 648, 138, new[] { "окко", "okko" }),
        ("WOG",      "#84CC16", 579, 126, new[] { "wog", "вог" }),
        ("Укрнафта", "#3B82F6", 437, 104, new[] { "укрнафта", "ukrnafta" }),
    };

    /// <summary>true, якщо ціни щойно завантажені з мережі (а не взяті з кешу).</summary>
    public bool IsLiveDataLoaded { get; private set; }

    /// <summary>Текст для підпису у віджеті: "Live ціни…" або "Кеш від 20.11 01:05".</summary>
    public string StatusText { get; private set; } = string.Empty;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept-Language", "uk-UA,uk;q=0.9");
        return client;
    }

    // ───────────────────────── Дашборд ─────────────────────────

    public async Task<List<StationWidgetInfo>> GetStationsDashboardAsync(LocalDatabase database)
    {
        var currency = Preferences.Get("Currency", "грн");
        string symbol = currency switch { "$ (USD)" => "$", "€ (EUR)" => "€", _ => "грн" };

        var rateTask = GetRateAsync(currency);
        var fuelTask = LoadFuelPricesAsync();
        var evTask = FetchHomeTariffUahAsync();
        var recordsTask = database.GetAllFuelRecordsAsync();

        await Task.WhenAll(rateTask, fuelTask, evTask, recordsTask);

        double rate = rateTask.Result;
        var fuelMap = fuelTask.Result;
        double? homeTariff = evTask.Result;
        var allRecords = recordsTask.Result;

        var result = new List<StationWidgetInfo>();

        foreach (var st in Stations)
        {
            var userRatings = allRecords
                .Where(r => r.QualityRating > 0 &&
                            string.Equals(r.StationName?.Trim(), st.Name, StringComparison.OrdinalIgnoreCase))
                .Select(r => r.QualityRating)
                .ToList();

            double avg = Math.Round((st.BaseSum + userRatings.Sum()) / (double)(st.BaseCount + userRatings.Count), 1);

            var prices = new List<FuelPriceItem>();
            if (fuelMap.TryGetValue(st.Name, out var list))
            {
                foreach (var fuel in ShownFuels)
                {
                    var p = list.FirstOrDefault(x => x.Fuel == fuel);
                    if (p != null)
                        prices.Add(new FuelPriceItem { FuelName = p.Fuel, PriceDisplay = Format(p.Uah, rate, symbol) });
                }
            }

            if (prices.Count == 0)
                prices.Add(new FuelPriceItem { FuelName = "Статус", PriceDisplay = "Немає даних" });

            result.Add(new StationWidgetInfo
            {
                Name = st.Name,
                AccentColor = st.Color,
                AverageRating = avg,
                RatingCount = st.BaseCount + userRatings.Count,
                Prices = prices
            });
        }

        var ev = new List<FuelPriceItem>();
        if (homeTariff.HasValue)
        {
            ev.Add(new FuelPriceItem { FuelName = "Дім (день)", PriceDisplay = Format(homeTariff.Value, rate, symbol, "/кВт") });
            // Нічний тариф для двозонних лічильників в Україні = 0.5 від денного
            ev.Add(new FuelPriceItem { FuelName = "Дім (ніч)", PriceDisplay = Format(homeTariff.Value * 0.5, rate, symbol, "/кВт") });
        }
        else
        {
            ev.Add(new FuelPriceItem { FuelName = "Дім (тариф)", PriceDisplay = "Немає даних" });
        }

        result.Add(new StationWidgetInfo
        {
            Name = "⚡ Зарядка EV",
            AccentColor = "#06B6D4",
            AverageRating = 5.0,
            RatingCount = 0,
            Prices = ev
        });

        return result;
    }

    private static string Format(double uah, double rate, string symbol, string suffix = "")
        => $"{Math.Round(uah / rate, 2):F2} {symbol}{suffix}";

    // ───────────────────────── Курс валют (НБУ) ─────────────────────────

    private async Task<double> GetRateAsync(string currency)
    {
        string? code = currency switch { "$ (USD)" => "USD", "€ (EUR)" => "EUR", _ => null };
        if (code == null) return 1.0;

        string cacheKey = $"Rate_{code}";
        try
        {
            string json = await Http.GetStringAsync(
                $"https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?valcode={code}&json");
            using var doc = JsonDocument.Parse(json);
            double rate = doc.RootElement[0].GetProperty("rate").GetDouble();
            if (rate > 0)
            {
                Preferences.Set(cacheKey, rate);
                return rate;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка курсу НБУ: {ex.Message}");
        }

        // Остання відома ціна, і лише в крайньому разі — запасне значення
        double cached = Preferences.Get(cacheKey, 0.0);
        return cached > 0 ? cached : (code == "USD" ? 41.5 : 45.0);
    }

    // ───────────────────────── Ціни на пальне ─────────────────────────

    private async Task<Dictionary<string, List<FuelPrice>>> LoadFuelPricesAsync()
    {
        try
        {
            string html = await GetHtmlAsync(FuelUrl);
            SaveDebugHtml("minfin_fuel.html", html);

            var parsed = ParseFuelTables(html);
            if (parsed.Count > 0)
            {
                Preferences.Set(FuelCacheKey, JsonSerializer.Serialize(parsed));
                Preferences.Set(FuelCacheTimeKey, DateTime.Now.ToString("O"));
                IsLiveDataLoaded = true;
                StatusText = "Live ціни (index.minfin.com.ua)";
                return parsed;
            }

            Console.WriteLine("[AutoLog] Таблицю з цінами на сторінці не розпізнано. Структура могла змінитися.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoLog] Помилка завантаження цін АЗС: {ex.Message}");
        }

        // Мережа недоступна або сторінка змінилась: показуємо останні реальні ціни з кешу
        IsLiveDataLoaded = false;
        var cached = LoadFuelCache(out DateTime? savedAt);
        StatusText = cached.Count > 0 && savedAt.HasValue
            ? $"Кеш від {savedAt.Value:dd.MM HH:mm}"
            : "Немає зв'язку з minfin.com.ua";
        return cached;
    }

    private static Dictionary<string, List<FuelPrice>> LoadFuelCache(out DateTime? savedAt)
    {
        savedAt = null;
        try
        {
            string json = Preferences.Get(FuelCacheKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new();

            string time = Preferences.Get(FuelCacheTimeKey, string.Empty);
            if (DateTime.TryParse(time, null, DateTimeStyles.RoundtripKind, out var dt)) savedAt = dt;

            return JsonSerializer.Deserialize<Dictionary<string, List<FuelPrice>>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Шукає таблицю, де є рядок із заголовками видів пального (А-95, ДП, Газ…),
    /// і читає ціни кожної мережі за назвою колонки, а не за номером.
    /// </summary>
    private static Dictionary<string, List<FuelPrice>> ParseFuelTables(string html)
    {
        var result = new Dictionary<string, List<FuelPrice>>();

        foreach (Match table in Regex.Matches(html, @"<table\b[^>]*>(.*?)</table>", Opts))
        {
            var rows = ExtractRows(table.Groups[1].Value);
            if (rows.Count < 2) continue;

            // 1. Знаходимо рядок заголовків: у ньому щонайменше 2 колонки — види пального
            int headerIdx = -1;
            List<string?> labels = new();
            for (int i = 0; i < Math.Min(4, rows.Count); i++)
            {
                var l = rows[i].Select(NormalizeFuelHeader).ToList();
                if (l.Count(x => x != null) >= 2)
                {
                    headerIdx = i;
                    labels = l;
                    break;
                }
            }
            if (headerIdx < 0) continue;

            // 2. Рядки з мережами
            for (int r = headerIdx + 1; r < rows.Count; r++)
            {
                var row = rows[r];

                // Назва мережі = текст усіх клітинок, які не є цінами (також alt логотипу)
                string nameText = string.Join(" ", row.Where(c => !TryParsePrice(c, out _))).ToLowerInvariant();
                var station = Stations.FirstOrDefault(s => s.Aliases.Any(a => nameText.Contains(a)));
                if (station.Name == null || result.ContainsKey(station.Name)) continue;

                // Якщо в рядку більше клітинок, ніж у заголовку (логотип, окрема назва),
                // додаткові клітинки стоять на початку, тому вирівнюємо справа.
                int offset = row.Count > labels.Count ? row.Count - labels.Count : 0;

                var prices = new List<FuelPrice>();
                for (int c = offset; c < row.Count; c++)
                {
                    int h = c - offset;
                    if (h >= labels.Count) break;

                    string? fuel = labels[h];
                    if (fuel == null) continue;
                    if (!TryParsePrice(row[c], out double uah)) continue;
                    if (prices.Any(p => p.Fuel == fuel)) continue;

                    prices.Add(new FuelPrice { Fuel = fuel, Uah = uah });
                }

                if (prices.Count > 0) result[station.Name] = prices;
            }
        }

        return result;
    }

    /// <summary>Повертає назву виду пального за текстом заголовка, або null, якщо це не пальне.</summary>
    private static string? NormalizeFuelHeader(string raw)
    {
        string s = Regex.Replace(raw.ToLowerInvariant(), @"[\s\-–—]", "");
        if (s.Length == 0) return null;

        bool plus = s.Contains('+') || s.Contains("преміум") || s.Contains("премиум") || s.Contains("premium");

        if (s.Contains("100")) return "А-100";
        if (s.Contains("95")) return plus ? "А-95+" : "А-95";
        if (s.Contains("92")) return "А-92";
        if (s.StartsWith("дп") || s.StartsWith("дт") || s.Contains("диз")) return plus ? "ДП+" : "ДП";
        if (s.Contains("газ") || s.Contains("lpg") || s.Contains("пропан")) return "Газ";
        return null;
    }

    // ───────────────────────── Тариф на електроенергію ─────────────────────────

    /// <summary>Денний тариф для населення, грн/кВт·год. Null, якщо знайти не вдалося.</summary>
    private async Task<double?> FetchHomeTariffUahAsync()
    {
        try
        {
            string html = await GetHtmlAsync(EvUrl);
            SaveDebugHtml("minfin_electric.html", html);

            return ExtractHomeTariffUah(html);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoLog] Помилка завантаження тарифу електроенергії: {ex.Message}");
        }
        return null;
    }

    /// <summary>
    /// Мінфін показує тарифи в КОПІЙКАХ за 1 кВт·год (наприклад "432,0" = 4.32 грн).
    /// Беремо першу таблицю тарифів і найвище значення в межах 1–15 грн (стандартний тариф).
    /// </summary>
    private static double? ExtractHomeTariffUah(string html)
    {
        foreach (Match table in Regex.Matches(html, @"<table\b[^>]*>(.*?)</table>", Opts))
        {
            string text = StripHtml(table.Groups[1].Value);
            if (!text.Contains("коп", StringComparison.OrdinalIgnoreCase) ||
                !text.Contains("кВт", StringComparison.OrdinalIgnoreCase))
                continue;

            double best = 0;
            foreach (Match m in Regex.Matches(text, @"(?<![\d.,])(\d{2,4})[.,](\d)(?!\d)"))
            {
                if (!double.TryParse($"{m.Groups[1].Value}.{m.Groups[2].Value}", NumberStyles.Any,
                        CultureInfo.InvariantCulture, out double kop))
                    continue;

                double uah = kop / 100.0;
                if (uah >= 1.0 && uah <= 15.0 && uah > best) best = uah;
            }

            if (best > 0) return best;
        }

        // Запасний варіант: тариф записано в гривнях, "4,32 грн/кВт"
        string pageText = StripHtml(Regex.Replace(html, @"<(script|style)\b.*?</\1>", " ", Opts));
        var g = Regex.Match(pageText, @"(\d{1,2}[.,]\d{2})\s*грн\.?\s*(?:/|за|на)\s*кВт", Opts);
        if (g.Success &&
            double.TryParse(g.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double v) &&
            v >= 1.0 && v <= 15.0)
            return v;

        return null;
    }

    // ───────────────────────── Допоміжне ─────────────────────────

    private static async Task<string> GetHtmlAsync(string url)
    {
        for (int attempt = 1; ; attempt++)
        {
            try
            {
                return await Http.GetStringAsync(url);
            }
            catch when (attempt < 2)
            {
                await Task.Delay(700);
            }
        }
    }

    /// <summary>Рядки таблиці → список текстів клітинок (colspan розгортається порожніми клітинками).</summary>
    private static List<List<string>> ExtractRows(string tableHtml)
    {
        var rows = new List<List<string>>();
        foreach (Match tr in Regex.Matches(tableHtml, @"<tr\b[^>]*>(.*?)</tr>", Opts))
        {
            var cells = new List<string>();
            foreach (Match td in Regex.Matches(tr.Groups[1].Value, @"<t[dh]\b([^>]*)>(.*?)</t[dh]>", Opts))
            {
                cells.Add(StripHtml(td.Groups[2].Value));

                var span = Regex.Match(td.Groups[1].Value, @"colspan\s*=\s*[""']?(\d+)", RegexOptions.IgnoreCase);
                if (span.Success && int.TryParse(span.Groups[1].Value, out int n))
                    for (int i = 1; i < Math.Clamp(n, 1, 10); i++) cells.Add(string.Empty);
            }
            if (cells.Count > 0) rows.Add(cells);
        }
        return rows;
    }

    private static bool TryParsePrice(string cell, out double value)
    {
        value = 0;
        var m = Regex.Match(cell, @"(?<![\d.,])(\d{2,3})[.,](\d{1,2})(?![\d])");
        if (!m.Success) return false;

        string num = $"{m.Groups[1].Value}.{m.Groups[2].Value}";
        if (!double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out value)) return false;
        return value >= 20 && value <= 250; // відсікаємо дати, відсотки, зміни ціни
    }

    private static string StripHtml(string input)
    {
        // alt/title логотипів мереж теж корисні для розпізнавання назви
        string withAlt = Regex.Replace(input, @"<img\b[^>]*?(?:alt|title)\s*=\s*[""']([^""']*)[""'][^>]*>", " $1 ", Opts);
        string noTags = Regex.Replace(withAlt, "<.*?>", " ", Opts);
        return Regex.Replace(WebUtility.HtmlDecode(noTags), @"\s+", " ").Trim();
    }

    /// <summary>Зберігає останній отриманий HTML, щоб можна було перевірити, що саме віддає сайт.</summary>
    private static void SaveDebugHtml(string fileName, string html)
    {
        try
        {
            string path = Path.Combine(FileSystem.CacheDirectory, fileName);
            File.WriteAllText(path, html);
            Console.WriteLine($"[AutoLog] HTML збережено: {path}");
        }
        catch { }
    }
}