using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Servcies.PriceСlculationServcies
{
    public class CurrencyRateFetcher
    {
        private static readonly HttpClient client = new HttpClient();

        public async Task<decimal> GetUSDRateAsync()
        {
            return await GetCurrencyRateAsync("USD");
        }

        public async Task<decimal> GetEURRateAsync()
        {
            return await GetCurrencyRateAsync("EUR");
        }

        public async Task<decimal> GetBYNRateAsync()
        {
            return await GetCurrencyRateAsync("BYN");
        }


        private async Task<decimal> GetCurrencyRateAsync(string currencyCode)
        {
            try
            {
                string url = $"https://www.cbr-xml-daily.ru/daily_json.js";
                var response = await client.GetStringAsync(url);
                var json = JObject.Parse(response);

                var rate = json["Valute"][currencyCode]["Value"].Value<decimal>();
                return rate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }
}
