using MySql.Data.MySqlClient;
using ORYS.WebApi.Database;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ORYS.WebApi.Services
{
    public class ReservationCleanupService : BackgroundService
    {
        private readonly DbConnectionFactory _db;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30); // 30 dakikada bir kontrol et

        public ReservationCleanupService(DbConnectionFactory db, IServiceScopeFactory scopeFactory, ILogger<ReservationCleanupService> logger)
        {
            _db = db;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Rezervasyon Temizlik Servisi başlatıldı.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessLateReservations();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Rezervasyon temizleme işlemi sırasında kritik hata oluştu.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessLateReservations()
        {
            _logger.LogInformation("🔍 Geç kalan rezervasyonlar kontrol ediliyor...");

            using var scope = _scopeFactory.CreateScope();
            var mailService = scope.ServiceProvider.GetRequiredService<IMailService>();

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            string query = @"
                SELECT id, full_name, email, res_code, check_in_date, internal_res_id 
                FROM online_reservations 
                WHERE status = 'Onaylandi' 
                AND (
                    check_in_date < CURDATE() 
                    OR (check_in_date = CURDATE() AND HOUR(CURRENT_TIME()) >= 18)
                )";

            var lateReservations = new List<dynamic>();
            using (var cmd = new MySqlCommand(query, conn))
            {
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    lateReservations.Add(new {
                        Id = rdr.GetInt32(rdr.GetOrdinal("id")),
                        FullName = rdr.GetString(rdr.GetOrdinal("full_name")),
                        Email = rdr.GetString(rdr.GetOrdinal("email")),
                        ResCode = rdr.IsDBNull(rdr.GetOrdinal("res_code")) ? $"#{rdr.GetInt32(rdr.GetOrdinal("id"))}" : rdr.GetString(rdr.GetOrdinal("res_code")),
                        CheckInDate = rdr.GetDateTime(rdr.GetOrdinal("check_in_date")),
                        InternalResId = rdr.IsDBNull(rdr.GetOrdinal("internal_res_id")) ? (int?)null : rdr.GetInt32(rdr.GetOrdinal("internal_res_id"))
                    });
                }
            }

            foreach (var res in lateReservations)
            {
                bool success = await CancelReservation(res);
                if (success)
                {
                    _logger.LogInformation($"✅ Rezervasyon otomatik iptal edildi: {res.ResCode} - {res.FullName}");
                    _ = mailService.SendAutoCancellationEmailAsync(res.Email, res.FullName, res.ResCode, res.CheckInDate);
                }
                else
                {
                    _logger.LogWarning($"⚠️ Rezervasyon iptal edilemedi: {res.ResCode}");
                }
            }
        }

        private async Task<bool> CancelReservation(dynamic res)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var tr = await conn.BeginTransactionAsync();

            try
            {
                string updateOnline = "UPDATE online_reservations SET status = 'IptalEdildi', reject_reason = 'Otomatik iptal: Giriş zamanı geçti' WHERE id = @id";
                using (var cmd = new MySqlCommand(updateOnline, conn, tr))
                {
                    cmd.Parameters.AddWithValue("@id", res.Id);
                    await cmd.ExecuteNonQueryAsync();
                }

                if (res.InternalResId != null)
                {
                    string updateInternal = "UPDATE reservations SET status = 'Iptal' WHERE id = @irid";
                    using (var cmd = new MySqlCommand(updateInternal, conn, tr))
                    {
                        cmd.Parameters.AddWithValue("@irid", res.InternalResId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    string updateRoom = "UPDATE rooms SET status = 'Available' WHERE id = (SELECT room_id FROM reservations WHERE id = @irid)";
                    using (var cmd = new MySqlCommand(updateRoom, conn, tr))
                    {
                        cmd.Parameters.AddWithValue("@irid", res.InternalResId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                await tr.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await tr.RollbackAsync();
                _logger.LogError(ex, $"❌ Rezervasyon iptal edilirken hata (ID: {res.Id})");
                return false;
            }
        }
    }
}
