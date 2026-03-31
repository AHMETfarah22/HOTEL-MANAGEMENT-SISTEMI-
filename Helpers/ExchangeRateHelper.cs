using System;
using System.Xml;

namespace ORYS.Helpers
{
    public static class ExchangeRateHelper
    {
        public static decimal GetRate(string currencyCode)
        {
            if (currencyCode == "TRY") return 1m;
            try
            {
                // Merkez Bankası anlık kurlarını çeker (Satış Kuru)
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load("https://www.tcmb.gov.tr/kurlar/today.xml");
                XmlNode? node = xmlDoc.SelectSingleNode($"Tarih_Date/Currency[@CurrencyCode='{currencyCode}']/ForexSelling");
                if (node != null && !string.IsNullOrWhiteSpace(node.InnerText))
                {
                    string rateStr = node.InnerText.Replace(',', '.');
                    if (decimal.TryParse(rateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal rate))
                        return rate;
                }
            }
            catch { }
            
            // Çevrimdışı (Offline) kalınırsa veya hata olursa kullanılacak yedek (fallback) kurlar
            return currencyCode switch {
                "USD" => 34.50m,
                "EUR" => 37.80m,
                "GBP" => 44.20m,
                _ => 1m
            };
        }
    }
}
