using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ORYS.WebApi.Services
{
    public interface IMailService
    {
        Task SendReservationUpdateAsync(string toEmail, string guestName, string status, string resCode, string? reason = null);
    }

    public class MailService : IMailService
    {
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendReservationUpdateAsync(string toEmail, string guestName, string status, string resCode, string? reason = null)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:Mail"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                
                string subject = status == "Onaylandi" ? "✅ Rezervasyonunuz Onaylandı!" : "❌ Rezervasyon Talebi Güncellemesi";
                email.Subject = $"AFM Grand Hotel - {subject} (#{resCode})";

                var builder = new BodyBuilder();
                
                string color = status == "Onaylandi" ? "#27AE60" : "#C0392B";
                string statusText = status == "Onaylandi" ? "ONAYLANDI" : "REDDEDİLDİ";
                
                builder.HtmlBody = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; overflow: hidden;'>
                        <div style='background: #080910; padding: 30px; text-align: center; color: #C9A84C;'>
                            <h1 style='margin:0;'>AFM GRAND HOTEL</h1>
                            <p style='margin:5px 0 0; font-size: 12px; letter-spacing: 2px;'>LÜKS KONAKLAMA DENEYİMİ</p>
                        </div>
                        <div style='padding: 30px; line-height: 1.6; color: #333;'>
                            <h2 style='color: {color}; text-align: center;'>Rezervasyon Durumu: {statusText}</h2>
                            <p>Sayın <strong>{guestName}</strong>,</p>
                            <p>Otelimize yapmış olduğunuz rezervasyon talebi incelenmiş ve durumu güncellenmiştir.</p>
                            
                            <div style='background: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <p style='margin: 0 0 10px;'><strong>Rezervasyon Kodu:</strong> {resCode}</p>
                                <p style='margin: 0 0 10px;'><strong>Durum:</strong> <span style='color: {color}; font-weight: bold;'>{statusText}</span></p>
                                {(reason != null ? $"<p style='margin: 0; color: #c0392b;'><strong>Açıklama:</strong> {reason}</p>" : "")}
                            </div>

                            <p>Detayları görmek ve takip etmek için web sitemizdeki profil panelini kullanabilirsiniz.</p>
                            
                            <div style='text-align: center; margin-top: 30px;'>
                                <a href='http://localhost:5050' style='background: #C9A84C; color: #000; padding: 12px 30px; text-decoration: none; border-radius: 50px; font-weight: bold; display: inline-block;'>Sisteme Giriş Yap</a>
                            </div>
                        </div>
                        <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #777;'>
                            <p>© 2026 AFM Grand Hotel. Tüm hakları saklıdır.<br/>İstanbul, Türkiye</p>
                        </div>
                    </div>";

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                // Test ortamı için SSL kontrolünü devredışı bırakabiliriz (Gerekirse)
                // smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

                await smtp.ConnectAsync(_config["MailSettings:Host"], int.Parse(_config["MailSettings:Port"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["MailSettings:Mail"], _config["MailSettings:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Mail gönderilemezse logla ama işlemi bozma
                Console.WriteLine($"❌ Mail gönderim hatası: {ex.Message}");
            }
        }
    }
}
