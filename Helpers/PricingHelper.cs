using System;
using System.Collections.Generic;
using System.Linq;
using ORYS.Models;

namespace ORYS.Helpers
{
    public static class PricingHelper
    {
        public enum DemandLevel
        {
            Low,        // < 40%
            Normal,     // 40% - 70%
            High,       // 70% - 90%
            Critical    // > 90%
        }

        /// <summary>
        /// Belirli bir tarih aralığı için doluluk oranını ve önerilen fiyat katsayısını hesaplar.
        /// </summary>
        public static (decimal Multiplier, DemandLevel Level, double OccupancyRate) GetDynamicPricingInfo(DateTime checkIn, DateTime checkOut)
        {
            int totalRooms = RoomHelper.GetRoomCount();
            if (totalRooms == 0) return (1.0m, DemandLevel.Normal, 0);

            // O tarihlerde müsait OLMAYAN odaları bul (Bakım dışındakiler)
            // GetAvailableRooms zaten bakımda olmayan ve çakışmayan odaları getiriyor.
            // Dolayısıyla: Dolu Odalar = Toplam Oda - Müsait Oda
            var availableRooms = RoomHelper.GetAvailableRooms(checkIn, checkOut);
            int occupiedCount = totalRooms - availableRooms.Count;

            double occupancyRate = (double)occupiedCount / totalRooms;
            
            decimal multiplier = 1.0m;
            DemandLevel level = DemandLevel.Normal;

            if (occupancyRate < 0.4)
            {
                multiplier = 1.0m; // Düşük sezonda baz fiyatı koru veya %5 indirim yapabiliriz: 0.95m
                level = DemandLevel.Low;
            }
            else if (occupancyRate < 0.7)
            {
                multiplier = 1.10m; // %10 Artış
                level = DemandLevel.Normal;
            }
            else if (occupancyRate < 0.9)
            {
                multiplier = 1.25m; // %25 Artış
                level = DemandLevel.High;
            }
            else
            {
                multiplier = 1.50m; // %50 Artış (Yoğun dönem)
                level = DemandLevel.Critical;
            }

            return (multiplier, level, occupancyRate);
        }

        public static decimal CalculateSmartPrice(decimal basePrice, decimal multiplier)
        {
            return Math.Round(basePrice * multiplier, 2);
        }

        public static string GetDemandMessage(DemandLevel level, double rate)
        {
            string percentage = (rate * 100).ToString("F0");
            return level switch
            {
                DemandLevel.Low => $"📉 Düşük Talep (%{percentage} Doluluk). Standart fiyatlar uygulanıyor.",
                DemandLevel.Normal => $"✅ Normal Talep (%{percentage} Doluluk). Fiyatlar %10 optimize edildi.",
                DemandLevel.High => $"🔥 Yüksek Talep (%{percentage} Doluluk)! Fiyatlar %25 artırıldı.",
                DemandLevel.Critical => $"🚀 Zirve Talep (%{percentage} Doluluk)!! Fiyatlar %50 artırıldı.",
                _ => "Fiyatlar güncel."
            };
        }
    }
}
