# ORYS - Modern Hotel Management System (PMS)

ORYS is a modern Property Management System (PMS) built with C# and .NET WinForms, designed to streamline hotel operations from reception to accounting.

## 🚀 Recent Key Features

- **Modern Dashboard**: Real-time overview of room status (Occupied, Available, Reserved, Maintenance) and today's arrival/departure lists.
- **Cascading Address System**: Integration with Türkiye API for dynamic city and district selection during guest registration, ensuring data consistency.
- **Enhanced Reservation Workflow**: 
    - Automated payment-to-checkout integration.
    - Real-time guest check-in/check-out tracking.
    - Support for multiple currencies (TRY, USD, EUR, GBP) with real-time exchange rates.
- **Scrollable & Modern Forms**: User-friendly registration dialogs with fixed footer buttons and sections for a premium UI experience.
- **Data Integrity**: Secure handling of TC Identification numbers, passport data, and detailed reservation logs.

## 🛠 Technology Stack

- **Frontend**: C# WinForms (Modernized Layouts, Rich Aesthetics)
- **Backend**: .NET Framework / .NET Core
- **Database**: MySQL (optimized with `ReservationHelper` and `DatabaseHelper`)
- **API**: [turkiyeapi.dev](https://turkiyeapi.dev) for geolocation data.

---

# ORYS - Modern Otel Yönetim Sistemi (PMS)

ORYS, resepsiyondan muhasebeye kadar otel operasyonlarını kolaylaştırmak için C# ve .NET WinForms ile geliştirilmiş modern bir Otel Yönetim Sistemi (PMS) çözümüdür.

## 🚀 Önemli Özellikler

- **Modern Dashboard**: Oda durumlarının (Dolu, Boş, Rezerve, Bakımda) ve günlük giriş/çıkış listelerinin anlık takibi.
- **Dinamik Adres Sistemi**: Misafir kaydı sırasında Türkiye illeri ve ilçelerini otomatik listeleyen API entegrasyonu.
- **Gelişmiş Rezervasyon Akışı**:
    - Ödeme sonrası otomatik çıkış (check-out) entegrasyonu.
    - Gerçek zamanlı giriş/çıkış takibi (Ahmet Yılmaz gibi erken çıkış yapanların listede görünmeye devam etmesi sağlandı).
    - Döviz desteği (TRY, USD, EUR, GBP) ve anlık kur hesaplama.
- **Modern Arayüz**: Sabit butonlu, kaydırılabilir ve bölümlere ayrılmış dinamik kayıt formları.

## 🔧 Kurulum ve Çalıştırma

1. Projeyi bilgisayarınıza indirin.
2. `config.json` dosyasındaki MySQL veritabanı ayarlarınızı yapılandırın.
3. Visual Studio ile `ORYS.sln` dosyasını açın ve projeyi derleyin.

---
*Developed as part of the ORYS modernization project.*
