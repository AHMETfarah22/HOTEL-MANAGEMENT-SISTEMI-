using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace ORYS.WebApi.Services
{
    public interface IMailService
    {
        Task SendReservationUpdateAsync(string toEmail, string guestName, string status, string resCode, string? reason = null, string? rejectMessage = null, string? roomNumber = null, string? roomTypeName = null, decimal? totalPrice = null, DateTime? checkIn = null, DateTime? checkOut = null);
        Task SendWelcomeCheckInEmailAsync(string toEmail, string guestName, string roomNumber, decimal totalPrice, DateTime checkIn, DateTime checkOut);
        Task SendAutoCancellationEmailAsync(string toEmail, string guestName, string resCode, DateTime checkIn);
    }

    public class MailService : IMailService
    {
        private readonly IConfiguration _config;

        public MailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAutoCancellationEmailAsync(string toEmail, string guestName, string resCode, DateTime checkIn)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:Mail"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "⚠️ Rezervasyonunuz Gecikme Nedeniyle İptal Edildi";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; overflow: hidden;'>
                        <div style='background: #080910; padding: 30px; text-align: center; color: #C9A84C;'>
                            <h1 style='margin:0;'>AFM GRAND HOTEL</h1>
                        </div>
                        <div style='padding: 30px; line-height: 1.6; color: #333;'>
                            <h2 style='color: #C0392B; text-align: center;'>Otomatik İptal Bilgilendirmesi</h2>
                            <p>Sayın <strong>{guestName}</strong>,</p>
                            <p><strong>{resCode}</strong> kodlu ve <strong>{checkIn:dd.MM.yyyy}</strong> tarihli rezervasyonunuz, giriş saatinin üzerinden uzun süre geçmesine rağmen giriş (Check-in) işlemi yapılmadığı için sistem tarafından otomatik olarak iptal edilmiştir.</p>
                            
                            <div style='background: #FFF5F5; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #C0392B;'>
                                <p style='margin: 0; color: #C0392B;'><strong>Bilgi:</strong> Otel politikamız gereği, varış günü saat 18:00'e kadar giriş yapılmayan rezervasyonlar otomatik olarak iptal edilmektedir.</p>
                            </div>

                            <p>Konuyla ilgili sorularınız için bizimle iletişime geçebilirsiniz.</p>
                            <p style='text-align:center; margin-top:20px;'>Saygılarımızla,<br/>AFM Grand Hotel Yönetimi</p>
                        </div>
                        <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #777;'>
                            <p>© 2026 AFM Grand Hotel. Tüm hakları saklıdır.</p>
                        </div>
                    </div>";

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["MailSettings:Host"], int.Parse(_config["MailSettings:Port"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["MailSettings:Mail"], _config["MailSettings:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Otomatik iptal maili hatası: {ex.Message}");
            }
        }

        public async Task SendWelcomeCheckInEmailAsync(string toEmail, string guestName, string roomNumber, decimal totalPrice, DateTime checkIn, DateTime checkOut)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_config["MailSettings:Mail"]));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "🏨 AFM Grand Hotel'e Hoş Geldiniz!";

                var builder = new BodyBuilder();
                builder.HtmlBody = $@"
                    <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; border-radius: 10px; overflow: hidden;'>
                        <div style='background: #080910; padding: 30px; text-align: center; color: #C9A84C;'>
                            <h1 style='margin:0;'>AFM GRAND HOTEL</h1>
                        </div>
                        <div style='padding: 30px; line-height: 1.6; color: #333;'>
                            <h2 style='color: #27AE60; text-align: center;'>Otelimize Hoş Geldiniz!</h2>
                            <p>Sayın <strong>{guestName}</strong>,</p>
                            <p>AFM Grand Hotel'e giriş işleminiz başarıyla tamamlanmıştır. Sizi ağırlamaktan mutluluk duyuyoruz.</p>
                            
                            <div style='background: #f9f9f9; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                                <table style='width:100%; border-collapse: collapse;'>
                                    <tr><td style='padding:5px 0; color:#777;'>Oda Numaranız:</td><td style='text-align:right; font-weight:bold; font-size:1.2em;'>{roomNumber}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Giriş Tarihi:</td><td style='text-align:right; font-weight:bold;'>{checkIn:dd.MM.yyyy}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Çıkış Tarihi:</td><td style='text-align:right; font-weight:bold;'>{checkOut:dd.MM.yyyy}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Toplam Tutar:</td><td style='text-align:right; font-weight:bold; color:#C9A84C;'>₺{totalPrice:N0}</td></tr>
                                </table>
                            </div>

                            <div style='background: #eef7ff; padding: 20px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #3498db;'>
                                <h3 style='margin-top:0; color: #2980b9;'>📶 Faydalı Bilgiler</h3>
                                <p style='margin: 5px 0;'><strong>WiFi Adı:</strong> afmgrandwiffi</p>
                                <p style='margin: 5px 0;'><strong>WiFi Şifresi:</strong> <code style='background:#fff; padding:2px 5px; border-radius:4px;'>Afmgrand190#</code></p>
                                <p style='margin: 10px 0 0;'><strong>🍴 Restoran:</strong> 1. Kat (Kahvaltı: 07:00 - 10:30)</p>
                            </div>

                            <p>Konaklamanız boyunca size en iyi hizmeti sunmak için buradayız. Herhangi bir ihtiyacınızda resepsiyonumuza (Dahili: 0) başvurabilirsiniz.</p>
                            <p style='text-align:center; font-style:italic; margin-top:20px;'>İyi istirahatler dileriz.</p>
                        </div>
                        <div style='background: #f1f1f1; padding: 20px; text-align: center; font-size: 12px; color: #777;'>
                            <p>© 2026 AFM Grand Hotel. Tüm hakları saklıdır.</p>
                        </div>
                    </div>";

                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(_config["MailSettings:Host"], int.Parse(_config["MailSettings:Port"]), SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_config["MailSettings:Mail"], _config["MailSettings:Password"]);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Hoş geldin maili hatası: {ex.Message}");
            }
        }

        public async Task SendReservationUpdateAsync(string toEmail, string guestName, string status, string resCode, string? reason = null, string? rejectMessage = null, string? roomNumber = null, string? roomTypeName = null, decimal? totalPrice = null, DateTime? checkIn = null, DateTime? checkOut = null)
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
                                
                                {(status == "Onaylandi" && roomNumber != null ? $@"
                                <hr style='border:0; border-top:1px solid #ddd; margin:15px 0;' />
                                <table style='width:100%; border-collapse: collapse;'>
                                    <tr><td style='padding:5px 0; color:#777;'>Oda Bilgisi:</td><td style='text-align:right; font-weight:bold;'>{roomNumber} {(string.IsNullOrEmpty(roomTypeName) ? "" : $"({roomTypeName})")}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Giriş Tarihi:</td><td style='text-align:right; font-weight:bold;'>{checkIn?.ToString("dd.MM.yyyy")}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Çıkış Tarihi:</td><td style='text-align:right; font-weight:bold;'>{checkOut?.ToString("dd.MM.yyyy")}</td></tr>
                                    <tr><td style='padding:5px 0; color:#777;'>Toplam Tutar:</td><td style='text-align:right; font-weight:bold; color:#C9A84C; font-size:1.1em;'>₺{totalPrice:N0}</td></tr>
                                </table>
                                " : "")}

                                {(reason != null ? $"<p style='margin: 15px 0 0; color: #c0392b;'><strong>Sebep:</strong> {reason}</p>" : "")}
                                {(rejectMessage != null ? $"<p style='margin: 10px 0 0; color: #555;'><strong>Mesaj:</strong> {rejectMessage}</p>" : "")}
                            </div>

                            <p>Otelimizde konaklayacağınız günü heyecanla bekliyoruz!</p>
                            
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
