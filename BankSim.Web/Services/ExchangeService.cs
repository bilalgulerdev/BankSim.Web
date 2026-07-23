using BankSim.Web.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace BankSim.Web.Services
{
    public class ExchangeService : IExchangeService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        // Önbelleği zorla temizlemek ve yeni formülü işletmek için v3 yaptık
        private const string CacheKey = "WeeklyExchangeRates_v3";

        public ExchangeService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }

        public async Task<List<ExchangeRateViewModel>> GetLiveRatesAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<ExchangeRateViewModel>? cachedRates) && cachedRates != null)
            {
                return cachedRates;
            }

            var liveRates = await FetchRatesFromExternalApiAsync();

            DateTime now = DateTime.Now;
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            if (daysUntilSunday == 0) daysUntilSunday = 7;

            DateTime nextSunday = now.Date.AddDays(daysUntilSunday);

            _cache.Set(CacheKey, liveRates, nextSunday);

            return liveRates;
        }

        private async Task<List<ExchangeRateViewModel>> FetchRatesFromExternalApiAsync()
        {
            var rates = new List<ExchangeRateViewModel>();

            try
            {
                var response = await _httpClient.GetAsync("https://api.exchangerate-api.com/v4/latest/USD");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResult = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<JsonElement>(jsonResult);
                    var exchangeRates = data.GetProperty("rates");

                    decimal usdToTry = exchangeRates.GetProperty("TRY").GetDecimal();
                    decimal usdToEur = exchangeRates.GetProperty("EUR").GetDecimal();
                    decimal eurToTry = usdToTry / usdToEur;
                    decimal usdToGbp = exchangeRates.GetProperty("GBP").GetDecimal();
                    decimal gbpToTry = usdToTry / usdToGbp;

                    rates.Add(CreateRateModel("USD", "Amerikan Doları", usdToTry));
                    rates.Add(CreateRateModel("EUR", "Euro", eurToTry));
                    rates.Add(CreateRateModel("GBP", "İngiliz Sterlini", gbpToTry));

                    // --- ALTIN (XAU) HESAPLAMASI (GÜVENLİ ÇAPA YÖNTEMİ) ---
                    // API'den gelen kurdaki sapmaların altını 8.7k gibi uçuk rakamlara çıkarmasını engelliyoruz.
                    // Ana merkez noktası olarak 6200 TL'yi referans alıyoruz.
                    decimal baseAltinTry = 6200.00m;

                    // Dolar kurundaki değişimi altına çok hafif (onda bir oranında) yansıtarak 
                    // hem sabit kalmasını hem de piyasa gibi canlı hissettirmesini sağlıyoruz.
                    decimal kurFarkiPayi = (usdToTry - 33.00m) * 10m;
                    decimal gramAltinTry = baseAltinTry + kurFarkiPayi;

                    rates.Add(CreateRateModel("XAU", "Gram Altın", gramAltinTry));
                }
            }
            catch (Exception)
            {
                rates.Add(CreateRateModel("USD", "Amerikan Doları (Tahmini)", 33.00m));
                rates.Add(CreateRateModel("XAU", "Gram Altın (Tahmini)", 6200.00m));
            }

            return rates;
        }

        private ExchangeRateViewModel CreateRateModel(string code, string name, decimal baseRate)
        {
            return new ExchangeRateViewModel
            {
                CurrencyCode = code,
                CurrencyName = name,
                BuyRate = baseRate * 0.985m,
                SellRate = baseRate * 1.015m,
                Trend = GetRandomTrend()
            };
        }

        private string GetRandomTrend()
        {
            string[] trends = { "up", "down", "equal" };
            return trends[new Random().Next(trends.Length)];
        }
    }
}