using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;
using ORYS.WebApi.Models;
using System.IO;

namespace ORYS.WebApi.Services
{
    public interface IPdfService
    {
        string GenerateReservationPdf(OnlineReservationRequest req, RoomDto room, decimal totalPrice, int nights);
    }

    public class PdfService : IPdfService
    {
        private readonly IWebHostEnvironment _env;

        public PdfService(IWebHostEnvironment env)
        {
            _env = env;
            // QuestPDF License (Required for community use)
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string GenerateReservationPdf(OnlineReservationRequest req, RoomDto room, decimal totalPrice, int nights)
        {
            var fileName = $"res_{Guid.NewGuid():N}.pdf";
            var filePath = Path.Combine(_env.WebRootPath, "pdfs", fileName);
            
            // Ensure directory exists
            var dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("AFM GRAND HOTEL").FontSize(20).SemiBold().FontColor(Colors.Amber.Medium);
                            col.Item().Text("Lüks Konaklama Deneyimi").FontSize(10).Italic();
                        });

                        row.RelativeItem().AlignRight().Column(col =>
                        {
                            col.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}");
                            col.Item().Text("Rezervasyon Önizleme Belgesi");
                        });
                    });

                    page.Content().PaddingVertical(20).Column(x =>
                    {
                        x.Spacing(10);

                        x.Item().Text("MİSAFİR BİLGİLERİ").SemiBold().FontSize(12).Underline();
                        x.Item().Text($"Ad Soyad: {req.FullName}");
                        x.Item().Text($"E-posta: {req.Email}");
                        x.Item().Text($"Telefon: {req.Phone ?? "-"}");
                        x.Item().Text($"TC/Pasaport: {req.TcNo ?? "-"}");

                        x.Item().PaddingTop(10).Text("REZERVASYON DETAYLARI").SemiBold().FontSize(12).Underline();
                        x.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Cell().Text("Oda Tipi:");
                            table.Cell().Text($"{room.RoomTypeName} (Oda {room.RoomNumber})");

                            table.Cell().Text("Giriş Tarihi:");
                            table.Cell().Text(DateTime.Parse(req.CheckInDate).ToString("dd.MM.yyyy"));

                            table.Cell().Text("Çıkış Tarihi:");
                            table.Cell().Text(DateTime.Parse(req.CheckOutDate).ToString("dd.MM.yyyy"));

                            table.Cell().Text("Konaklama Süresi:");
                            table.Cell().Text($"{nights} Gece");

                            table.Cell().Text("Misafir Sayısı:");
                            table.Cell().Text($"{req.Adults} Yetişkin, {req.Children} Çocuk");
                        });

                        x.Item().PaddingTop(10).Text("FİNANSAL ÖZET").SemiBold().FontSize(12).Underline();
                        x.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(y =>
                        {
                            y.Item().Row(row =>
                            {
                                row.RelativeItem().Text("Gecelik Oda Ücreti:");
                                row.RelativeItem().AlignRight().Text($"{room.PricePerNight:N2} ₺");
                            });

                            y.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Oda Toplam ({nights} Gece):");
                                row.RelativeItem().AlignRight().Text($"{totalPrice:N2} ₺");
                            });

                            y.Item().Row(row =>
                            {
                                row.RelativeItem().Text("KDV & Vergiler:");
                                row.RelativeItem().AlignRight().Text("Dahil");
                            });

                            y.Item().PaddingTop(5).BorderTop(1).Row(row =>
                            {
                                row.RelativeItem().Text("GENEL TOPLAM:").Bold().FontSize(14);
                                row.RelativeItem().AlignRight().Text($"{totalPrice:N2} ₺").Bold().FontSize(14).FontColor(Colors.Amber.Medium);
                            });
                        });

                        if (!string.IsNullOrEmpty(req.Notes))
                        {
                            x.Item().PaddingTop(10).Text("NOTLAR").SemiBold();
                            x.Item().Text(req.Notes).Italic();
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Bu belge bir rezervasyon onayı değildir, sadece ön bilgilendirme amaçlıdır. ");
                        x.CurrentPageNumber();
                    });
                });
            }).GeneratePdf(filePath);

            return $"/pdfs/{fileName}";
        }
    }
}
