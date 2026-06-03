<div align="center">

# 🏨 AFM GRAND HOTEL — Otel Yönetim Sistemi

### Modern, Güvenli ve Kapsamlı Otel Yönetim Platformu (PMS)

![C#](https://img.shields.io/badge/C%23-.NET_WinForms-239120?style=for-the-badge&logo=csharp&logoColor=white)
![ASP.NET](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-Veritabanı-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![License](https://img.shields.io/badge/Lisans-MIT-green?style=for-the-badge)

**AFM Grand Hotel**, resepsiyondan muhasebeye, restorandan teknik servise kadar tüm otel operasyonlarını tek bir platformda yöneten profesyonel bir **Property Management System (PMS)** çözümüdür.

---

</div>

## ✨ Öne Çıkan Özellikler

### 📊 Akıllı Dashboard
- Tüm odaların anlık durumu: **Müsait**, **Dolu**, **Rezerve**, **Bakımda**
- Bugünkü giriş/çıkış (check-in/check-out) listeleri
- Gerçek zamanlı bildirim sistemi ve uyarı rozetleri
- Canlı saat ve günlük özet bilgi çubuğu

### 🌐 Online Rezervasyon Sistemi (Web)
- Müşterilerin tarayıcıdan otel odası arayıp rezervasyon yapabildiği **responsive web arayüzü**
- Oda müsaitlik kontrolü ve anlık fiyat hesaplama
- Online ödeme desteği (kredi kartı bilgi toplama)
- Rezervasyon özeti **PDF** oluşturma ve indirme
- Rezervasyon durum sorgulama (e-posta + kod ile)
- Admin panelinden tek tıkla **Onayla / Reddet** işlemleri
- Onay/red durumunda misafire **otomatik e-posta** (HTML şablonlu)

### 🛏️ Oda Yönetimi
- 40+ oda kapasitesi, 5 farklı oda tipi (Standart, Deluxe, Suite, Aile, Kral Dairesi)
- Oda durumu takibi ve filtreli listeleme
- Kat bazlı oda haritası
- Oda fiyat yönetimi ve kapasite ayarları

### 📅 Rezervasyon Yönetimi
- Gelişmiş rezervasyon oluşturma ve düzenleme
- Durum takibi: Bekliyor → Onaylandı → Giriş Yapıldı → Çıkış Yapıldı
- Otomatik iptal sistemi (geciken giriş için arka plan servisi)
- Çakışma kontrolü ve müsaitlik doğrulaması

### 👥 Misafir Yönetimi
- Detaylı misafir kayıt sistemi (TC Kimlik, Pasaport, İletişim)
- **Türkiye API** entegrasyonu ile dinamik il/ilçe seçimi
- Uyruk desteği (yerli/yabancı)
- Misafir geçmişi ve konaklama takibi

### 👁️ Misafir İzleme
- Anlık olarak otelde bulunan misafirlerin takibi
- Check-in / Check-out durumlarının gerçek zamanlı izlenmesi

### 💳 Ödeme ve Muhasebe
- Çoklu ödeme yöntemi: **Nakit, Kredi Kartı, Havale, Diğer**
- Çoklu döviz desteği: **₺ TRY, $ USD, € EUR, £ GBP**
- Anlık döviz kuru hesaplama
- Ödeme geçmişi ve makbuz takibi
- Online ödemelerin otomatik kaydı

### 🍽️ Restoran Yönetimi
- Kategori bazlı menü yönetimi (Sıcak/Soğuk İçecekler, Ana Yemekler, Tatlılar)
- Ürün ekleme, düzenleme ve fiyatlandırma
- Sipariş yönetimi (Masa / Oda servisi)
- Oda hesabına yazma desteği

### 🧹 Kat Hizmetleri (Housekeeping)
- Oda temizlik durumu takibi: **Temiz, Kirli, Temizleniyor**
- Kirli oda sayısı rozet bildirimi
- Temizlik personeli atama ve log tutma
- Check-out sonrası otomatik "Kirli" işaretleme

### 🔧 Teknik Servis ve Bakım
- Arıza bildirim sistemi (Elektrik, Su, Mobilya, Klima vb.)
- Öncelik seviyeleri: **Düşük, Orta, Yüksek, Acil**
- Durum takibi: Bekliyor → Devam Ediyor → Tamamlandı
- Bekleyen arıza sayısı rozet bildirimi

### 🧑‍💼 Personel Yönetimi
- Kullanıcı rolleri: **Admin, Resepsiyonist, Muhasebe**
- Personel detayları (Pozisyon, Maaş, Vardiya, IBAN)
- Güvenli giriş sistemi (BCrypt şifreleme)
- Rol tabanlı yetkilendirme (her rol farklı menü görür)

### 📦 Stok / Envanter Yönetimi
- Otel malzemeleri takibi (Temizlik, Restoran, Teknik, Tekstil)
- Minimum stok uyarısı
- Giriş/Çıkış log kayıtları

### 📈 Raporlama
- Doluluk oranları ve gelir raporları
- Günlük/aylık istatistikler

### 🔔 Bildirim Sistemi
- Masaüstü bildirim desteği (system tray)
- Yeni online talep bildirimleri
- Giriş tarihi geçen rezervasyon uyarıları
- Bildirim merkezi paneli

### 🔐 Güvenlik
- Şifreler `.env` dosyasında saklanır (GitHub'a yüklenmez)
- BCrypt ile şifre hashleme
- Çevre değişkenleri ile hassas veri yönetimi
- `.gitignore` ile otomatik koruma

---

## 🛠️ Teknoloji Altyapısı

| Katman | Teknoloji |
|--------|-----------|
| **Masaüstü Uygulama** | C# .NET WinForms (Premium UI, Dark Theme, Altın Aksan) |
| **Web API** | ASP.NET Core (RESTful API) |
| **Web Arayüz** | HTML5, CSS3, JavaScript (Responsive SPA) |
| **Veritabanı** | MySQL (InnoDB, UTF-8) |
| **E-posta** | MailKit + Gmail SMTP (HTML şablonlu e-postalar) |
| **PDF** | Dinamik PDF oluşturma servisi |
| **Güvenlik** | BCrypt.Net, Çevre Değişkenleri (.env) |
| **API** | [turkiyeapi.dev](https://turkiyeapi.dev) (İl/İlçe verileri) |
| **Deployment** | Docker desteği, Render uyumlu |

---

## 📁 Proje Yapısı

```
AFM-GRAND-HOTEL/
│
├── 📄 ORYS.sln                       # Visual Studio Solution dosyası
├── 📄 ORYS.csproj                     # Ana proje yapılandırması (.NET)
├── 📄 Program.cs                      # WinForms uygulama giriş noktası
├── 📄 Dockerfile                      # Docker konteyner yapılandırması
├── 📄 config.json                     # Veritabanı ve mail ayarları (yerel)
├── 📄 .env                           # 🔒 Şifreler burada (Git'e yüklenmez)
├── 📄 .env.example                    # Şifre şablonu (Git'e yüklenir)
├── 📄 .gitignore                      # Git'in yoksaydığı dosyalar
│
├── 📂 Forms/                          # WinForms Arayüz Dosyaları
│   ├── LoginForm.cs                   #   → Giriş ekranı (kullanıcı adı + şifre)
│   ├── RegisterForm.cs                #   → Yeni kullanıcı kayıt formu
│   └── MainForm.cs                    #   → Ana ekran (Dashboard, tüm modüller)
│
├── 📂 Database/                       # Veritabanı Katmanı
│   └── DatabaseHelper.cs              #   → MySQL bağlantısı, tablo oluşturma, seed data
│
├── 📂 Models/                         # Veri Modelleri
│   ├── User.cs                        #   → Kullanıcı modeli (Admin, Resepsiyonist, Muhasebe)
│   ├── Room.cs                        #   → Oda modeli (numara, tip, fiyat, durum)
│   ├── Guest.cs                       #   → Misafir modeli (TC, pasaport, iletişim)
│   ├── Reservation.cs                 #   → Rezervasyon modeli (tarih, durum, fiyat)
│   ├── Payment.cs                     #   → Ödeme modeli (yöntem, tutar, döviz)
│   ├── RestaurantModels.cs            #   → Restoran modelleri (kategori, ürün, sipariş)
│   ├── HousekeepingLog.cs             #   → Kat hizmeti log modeli
│   ├── MaintenanceRequest.cs          #   → Teknik servis arıza modeli
│   └── EmployeeInventoryModels.cs     #   → Personel detay ve stok modelleri
│
├── 📂 Helpers/                        # İş Mantığı (Business Logic) Katmanı
│   ├── AuthHelper.cs                  #   → Kimlik doğrulama ve BCrypt şifreleme
│   ├── RoomHelper.cs                  #   → Oda CRUD işlemleri
│   ├── GuestHelper.cs                 #   → Misafir CRUD işlemleri
│   ├── ReservationHelper.cs           #   → Rezervasyon yönetimi ve durum geçişleri
│   ├── PaymentHelper.cs               #   → Ödeme kayıt ve sorgulama
│   ├── OnlineReservationHelper.cs     #   → Online rezervasyon tablo yönetimi
│   ├── PricingHelper.cs               #   → Fiyat hesaplama ve döviz kuru
│   ├── UserHelper.cs                  #   → Kullanıcı/personel yönetimi
│   ├── RestaurantHelper.cs            #   → Restoran menü ve sipariş işlemleri
│   ├── HousekeepingHelper.cs          #   → Kat hizmetleri ve temizlik takibi
│   ├── MaintenanceHelper.cs           #   → Teknik servis arıza yönetimi
│   ├── EmployeeInventoryHelper.cs     #   → Personel detay ve stok yönetimi
│   ├── LocationHelper.cs              #   → Türkiye API (il/ilçe) entegrasyonu
│   └── ExchangeRateHelper.cs          #   → Döviz kuru API entegrasyonu
│
├── 📂 Resources/                      # Görsel Kaynaklar
│   └── logo.png                       #   → Otel logosu
│
├── 📂 ORYS.WebApi/                    # 🌐 Web API Projesi (Online Rezervasyon)
│   ├── Program.cs                     #   → API giriş noktası ve .env yükleme
│   ├── ORYS.WebApi.csproj             #   → WebApi proje yapılandırması
│   │
│   ├── 📂 Controllers/               #   API Endpoint'leri
│   │   ├── ReservationsController.cs  #     → Online rezervasyon CRUD + onay/red
│   │   ├── RoomsController.cs         #     → Oda listeleme ve müsaitlik API
│   │   └── PaymentsController.cs      #     → Ödeme işlemleri API
│   │
│   ├── 📂 Database/                   #   WebApi Veritabanı Katmanı
│   │   └── DbConnectionFactory.cs     #     → MySQL bağlantı fabrikası
│   │
│   ├── 📂 Models/                     #   API DTO Modelleri
│   │   └── Dtos.cs                    #     → Request/Response veri transfer objeleri
│   │
│   ├── 📂 Services/                   #   Arka Plan Servisleri
│   │   ├── MailService.cs             #     → Gmail SMTP ile HTML e-posta gönderimi
│   │   ├── PdfService.cs             #     → Rezervasyon özeti PDF oluşturma
│   │   └── ReservationCleanupService.cs #   → Geciken rezervasyonları otomatik iptal
│   │
│   └── 📂 wwwroot/                    #   Web Arayüzü (Frontend)
│       ├── index.html                 #     → Ana sayfa (oda arama + rezervasyon)
│       ├── payment.html               #     → Online ödeme sayfası
│       ├── checkout-payment.html      #     → Ödeme onay sayfası
│       ├── 📂 css/style.css           #     → Responsive CSS tasarım
│       ├── 📂 js/app.js              #     → Frontend JavaScript mantığı
│       └── 📂 pdfs/                   #     → Oluşturulan PDF dosyaları
│
├── 📄 BASLAT_VE_YAYINLA.bat          # Tek tıkla başlatma scripti
└── 📄 ONLINE_REZERVASYON_BASLAT.bat   # Online rezervasyon sistemi başlatma
```

---

## 🚀 Kurulum ve Çalıştırma

### Gereksinimler
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) veya üzeri
- [MySQL Server](https://dev.mysql.com/downloads/) (5.7+ veya 8.0+)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (önerilen)

### Adım 1: Projeyi İndirin
```bash
git clone https://github.com/AHMETfarah22/HOTEL-MANAGEMENT-SISTEMI-.git
cd HOTEL-MANAGEMENT-SISTEMI-
```

### Adım 2: Çevre Değişkenlerini Ayarlayın
```bash
# .env.example dosyasını kopyalayın
cp .env.example .env

# .env dosyasını açıp kendi bilgilerinizi yazın
```

`.env` dosyası içeriği:
```env
DB_SERVER=localhost
DB_PORT=3306
DB_NAME=orys_db
DB_USER=root
DB_PASSWORD=sizin_mysql_sifreniz

EMAIL_USER=sizin-email@gmail.com
EMAIL_APP_PASSWORD=xxxx xxxx xxxx xxxx
EMAIL_HOST=smtp.gmail.com
EMAIL_PORT=587
```

> 💡 **Gmail App Password** almak için: [Google Hesap Ayarları](https://myaccount.google.com/apppasswords) → Uygulama Şifresi Oluştur

### Adım 3: Masaüstü Uygulamayı Çalıştırın
```bash
# Visual Studio ile ORYS.sln açın ve F5 ile başlatın
# veya komut satırından:
dotnet run --project ORYS.csproj
```

### Adım 4: Online Rezervasyon Sistemini Başlatın
```bash
cd ORYS.WebApi
dotnet run
# Tarayıcıda: http://localhost:5050
```

> 📌 **Hızlı Başlatma:** `BASLAT_VE_YAYINLA.bat` dosyasına çift tıklayarak her iki sistemi aynı anda başlatabilirsiniz.

### Varsayılan Giriş Bilgileri

| Rol | Kullanıcı Adı | Şifre |
|-----|---------------|-------|
| 🔴 Admin | `admin` | `admin123` |
| 🟢 Resepsiyonist | `resepsiyon` | `resepsiyon123` |
| 🔵 Muhasebe | `muhasebe` | `muhasebe123` |

---

## 🐳 Docker ile Çalıştırma

```bash
docker build -t afm-grand-hotel .
docker run -p 5050:5050 --env-file .env afm-grand-hotel
```

---

## 📸 Ekran Görüntüleri

> *Uygulama koyu tema (dark mode) ve altın aksan renkleriyle premium bir arayüz sunar.*

- **Dashboard:** Oda durumları, günlük giriş/çıkış, bildirimler
- **Online Rezervasyon:** Müşterilerin web üzerinden oda araması ve rezervasyon yapması
- **Oda Yönetimi:** Kat planı, fiyat ve kapasite ayarları
- **Ödeme Ekranı:** Çoklu döviz ve ödeme yöntemi desteği

---

## 🤝 Katkıda Bulunma

1. Bu repoyu **fork** edin
2. Yeni bir **branch** oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi **commit** edin (`git commit -m 'Yeni özellik eklendi'`)
4. Branch'inizi **push** edin (`git push origin feature/yeni-ozellik`)
5. Bir **Pull Request** açın

---

## 📄 Lisans

Bu proje **MIT Lisansı** ile lisanslanmıştır.

---

<div align="center">

**⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!**

*AFM Grand Hotel — Otel Yönetim Sistemi © 2026*

</div>
