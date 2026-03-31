using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ORYS.Helpers
{
    public class CityInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DistrictInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public static class LocationHelper
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Şehir listesini API'den çeker
        public static List<CityInfo> GetProvinces()
        {
            var result = new List<CityInfo>();
            try
            {
                string url = "https://turkiyeapi.dev/api/v1/provinces?fields=name,id";
                string json = _http.GetStringAsync(url).GetAwaiter().GetResult();
                var doc = JsonNode.Parse(json);
                var data = doc?["data"]?.AsArray();
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        result.Add(new CityInfo
                        {
                            Id = item?["id"]?.GetValue<int>() ?? 0,
                            Name = item?["name"]?.GetValue<string>() ?? ""
                        });
                    }
                }
            }
            catch
            {
                // Fallback: En büyük şehirler
                result.AddRange(new[]
                {
                    new CityInfo { Id = 34, Name = "İstanbul" },
                    new CityInfo { Id = 6,  Name = "Ankara" },
                    new CityInfo { Id = 35, Name = "İzmir" },
                    new CityInfo { Id = 7,  Name = "Antalya" },
                    new CityInfo { Id = 16, Name = "Bursa" },
                });
            }
            return result;
        }

        // Seçilen şehrin ilçelerini API'den çeker
        public static List<DistrictInfo> GetDistricts(int provinceId)
        {
            var result = new List<DistrictInfo>();
            try
            {
                string url = $"https://turkiyeapi.dev/api/v1/provinces/{provinceId}?fields=name,districts";
                string json = _http.GetStringAsync(url).GetAwaiter().GetResult();
                var doc = JsonNode.Parse(json);
                var districts = doc?["data"]?["districts"]?.AsArray();
                if (districts != null)
                {
                    foreach (var d in districts)
                    {
                        result.Add(new DistrictInfo
                        {
                            Id = d?["id"]?.GetValue<int>() ?? 0,
                            Name = d?["name"]?.GetValue<string>() ?? ""
                        });
                    }
                    result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCulture));
                }
            }
            catch
            {
                // Fallback boş liste
            }
            return result;
        }
    }
}
