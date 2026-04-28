using System;
using System.Xml;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;

namespace ORYS.Helpers
{
    public static class ExchangeRateHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public static decimal GetRate(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode == "TRY") return 1m;

            // 1. Gerçek Zamanlı Piyasa Verisi (Global JSON API)
            try
            {
                // api.exchangerate-api.com ücretsiz sürümü hızlı ve stabil veriler sunar.
                string url = "https://api.exchangerate-api.com/v4/latest/TRY";
                var response = _httpClient.GetStringAsync(url).Result;
                using (JsonDocument doc = JsonDocument.Parse(response))
                {
                    var rates = doc.RootElement.GetProperty("rates");
                    // Bu API 1 TRY bazında kur verir. EUR: 0.026 vs.
                    // Biz 1 Birim Döviz = Kaç TL istiyoruz. Bu yüzden 1 / rate yapmalıyız.
                    if (rates.TryGetProperty(currencyCode, out JsonElement rateElement))
                    {
                        decimal tryBaseRate = rateElement.GetDecimal();
                        if (tryBaseRate > 0)
                            return 1 / tryBaseRate;
                    }
                }
            }
            catch
            {
                // Global API başarısız olursa TCMB'ye geç
            }

            // 2. Merkez Bankası (TCMB) - Resmi Veri
            try
            {
                // Not: HttpClient kullanmak XmlDocument.Load(url)'den daha güvenlidir.
                string xmlContent = _httpClient.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml").Result;
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);
                
                // ÖNEMLİ: Misafir döviz verirken otel "Döviz Alış" (ForexBuying) kurunu kullanmalıdır.
                // "ForexSelling" (Satış) kuru daha yüksektir ve misafire haksız kazanç (fazla TL) sağlar.
                XmlNode? node = xmlDoc.SelectSingleNode($"Tarih_Date/Currency[@CurrencyCode='{currencyCode}']/ForexBuying");
                
                if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
                {
                    string rateStr = node.InnerText.Replace(',', '.');
                    if (decimal.TryParse(rateStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rate))
                        return rate;
                }
            }
            catch { }
            
            // 3. Fallback (İnternet yoksa kullanılacak güvenli değerler - Nisan 2026 tahmini)
            return currencyCode switch {
                "USD" => 44.50m,
                "EUR" => 48.20m,
                "GBP" => 56.50m,
                _ => 1m
            };
        }
    }
}

