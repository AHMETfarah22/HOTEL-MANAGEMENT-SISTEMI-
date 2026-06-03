using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ORYS.Helpers;
using ORYS.Models;
using ORYS.Database;
using MySql.Data.MySqlClient;

namespace ORYS.Forms
{
    public partial class MainForm : Form
    {
        private Button? activeNav = null;
        private string Role => AuthHelper.CurrentUser?.Role ?? "Resepsiyonist";
        private bool IsAdmin => Role == "Admin";
        private bool IsResepsiyon => Role == "Resepsiyonist";
        private bool IsMuhasebe => Role == "Muhasebe";
        // ── Premium Renk Paleti ──────────────────────────────────────────
        private readonly Color cBg     = Color.FromArgb(11, 15, 28);       // Ana arka plan
        private readonly Color cCard   = Color.FromArgb(18, 24, 45);       // Kart arka planı
        private readonly Color cSidebar= Color.FromArgb(10, 14, 26);       // Sidebar arka plan
        private readonly Color cHeader = Color.FromArgb(14, 20, 38);       // Panel başlık
        private readonly Color cGold   = Color.FromArgb(218, 165, 32);     // Parlak altın
        private readonly Color cGoldHover = Color.FromArgb(240, 196, 60);  // Hover altın
        private readonly Color cText   = Color.FromArgb(190, 202, 225);    // Ana metin
        private readonly Color cSubText= Color.FromArgb(110, 130, 168);    // Alt metin
        private readonly Color cGreen  = Color.FromArgb(34, 197, 94);      // Yeşil (müsait)
        private readonly Color cRed    = Color.FromArgb(239, 68, 68);      // Kırmızı (dolu)
        private readonly Color cYellow = Color.FromArgb(245, 158, 11);     // Sarı (rezerve)
        private readonly Color cBlue   = Color.FromArgb(59, 130, 246);     // Mavi (bilgi)
        private readonly Color cPurple = Color.FromArgb(139, 92, 246);     // Mor (online)
        private readonly Color cBorder = Color.FromArgb(30, 42, 70);       // Çerçeve rengi

        // Online talepler rozeti için label referansı ve bildirimler
        private Label? lblOnlineBadge;
        private Button? btnOnline;
        private NotifyIcon notifyIcon = new NotifyIcon();
        private System.Windows.Forms.Timer onlineTimer = new System.Windows.Forms.Timer();
        private int lastPendingCount = 0;
        private int lastDirtyCount = 0;

        // Kat Hizmetleri butonu ve rozeti
        private Button? btnNavHousekeeping;
        private Label? lblDirtyBadge;

        // Teknik Servis Butonu ve Rozeti
        private List<string> notificationList = new List<string>();
        private Button? btnNotifications;
        private Label? lblNotifyBadge;
        private Panel? pnlNotificationCenter;

        // Teknik Servis Butonu ve Rozeti
        private Button? btnNavMaintenance;
        private Label? lblMaintenanceBadge;

        // Navigasyon Butonları (Designer'da olmayanlar)
        private Button? btnNavMonitoring;
        private Button? btnNavAyarlar;

        public MainForm()
        {
            InitializeComponent();
            this.Load += (s, e) => { 
                SetupNav(); 
                ShowDashboard(); 
                OnlineReservationHelper.EnsureTableExists(); 
                UpdateOnlineBadge();
                StartOnlineCheck();
                ShowDailyNotificationBar();
            };
            this.Resize += (s, e) => { if (activeNav?.Text.Contains("Dashboard") == true) ShowDashboard(); };
            pnlHeader.Paint += (s, e) => { using var p = new Pen(cGold, 2); e.Graphics.DrawLine(p, 0, pnlHeader.Height - 2, pnlHeader.Width, pnlHeader.Height - 2); };
            
            // Bildirim simgesi ayarları
            notifyIcon.Icon = SystemIcons.Information;
            notifyIcon.Visible = true;
            notifyIcon.BalloonTipClicked += (s, e) => { 
                if (btnOnline != null) SetActive(btnOnline); 
                ShowOnlineReservations(); 
            };
        }

        private void StartOnlineCheck()
        {
            onlineTimer.Interval = 10000;
            onlineTimer.Tick += (s, e) => {
                UpdateOnlineBadge();
                UpdateDirtyBadge();
                UpdateMaintenanceBadge();
                CheckForArrivalWarnings();
            };
            onlineTimer.Start();
        }

        private void CheckForArrivalWarnings()
        {
            try
            {
                var checkIns = ReservationHelper.GetTodayCheckIns();
                foreach (var r in checkIns)
                {
                    if (r.Status != "GirisYapildi" && r.Status != "Iptal")
                    {
                        var room = RoomHelper.GetAllRooms().Find(x => x.Id == r.RoomId);
                        if (room != null && room.Status == "Occupied")
                        {
                            string msg = $"⚠️ UYARI: {r.GuestName} için giriş tarihi geldi ama Oda {r.RoomNumber} hala DOLU!";
                            if (!notificationList.Contains(msg))
                            {
                                AddNotification(msg, true);
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private void AddNotification(string msg, bool urgent = false)
        {
            if (notificationList.Contains(msg)) return;
            notificationList.Insert(0, msg);
            if (notificationList.Count > 20) notificationList.RemoveAt(20);

            UpdateNotifyBadge();
            notifyIcon.ShowBalloonTip(3000, urgent ? "⚠️ KRİTİK UYARI" : "🔔 Bildirim", msg, urgent ? ToolTipIcon.Warning : ToolTipIcon.Info);
        }

        private void UpdateNotifyBadge()
        {
            if (lblNotifyBadge != null)
            {
                lblNotifyBadge.Text = notificationList.Count.ToString();
                lblNotifyBadge.Visible = notificationList.Count > 0;
            }
        }

        private void ShowDailyNotificationBar()
        {
            pnlNotify.Controls.Clear();
            pnlNotify.Height = 46;

            try
            {
                var checkIns  = ReservationHelper.GetTodayCheckIns();
                var checkOuts = ReservationHelper.GetTodayCheckOuts();
                int ci = checkIns.Count;
                int co = checkOuts.Count;
                bool urgent = ci > 0 || co > 0;

                // Gradient arka plan renkleri
                Color bgFrom = urgent
                    ? Color.FromArgb(80, 50, 0)   // turuncu-koyu
                    : Color.FromArgb(8, 40, 80);  // mavi-koyu
                Color bgTo   = urgent
                    ? Color.FromArgb(50, 30, 0)
                    : Color.FromArgb(6, 24, 52);
                Color accentColor = urgent ? cYellow : cBlue;

                pnlNotify.BackColor = bgFrom;
                pnlNotify.Paint += (s, e) => {
                    var g = e.Graphics;
                    // Gradient arka plan
                    using var bg = new LinearGradientBrush(pnlNotify.ClientRectangle, bgFrom, bgTo, LinearGradientMode.Horizontal);
                    g.FillRectangle(bg, pnlNotify.ClientRectangle);
                    // Sol vurgu çizgisi
                    using var stripeBrush = new LinearGradientBrush(
                        new Rectangle(0, 0, 5, pnlNotify.Height), accentColor,
                        Color.FromArgb(80, accentColor), LinearGradientMode.Vertical);
                    g.FillRectangle(stripeBrush, 0, 0, 5, pnlNotify.Height);
                    // Alt ayırıcı
                    g.FillRectangle(new SolidBrush(Color.FromArgb(40, accentColor)), 0, pnlNotify.Height - 1, pnlNotify.Width, 1);
                };

                string text = urgent
                    ? string.Join("   |   ", new[] {
                        ci > 0 ? $"🏨 GİRİŞ ({ci}): " + string.Join(", ", checkIns.ConvertAll(r => r.GuestName).GetRange(0, Math.Min(ci, 4))) + (ci > 4 ? $" +{ci - 4} daha" : "") : "",
                        co > 0 ? $"🚪 ÇIKIŞ ({co}): " + string.Join(", ", checkOuts.ConvertAll(r => r.GuestName).GetRange(0, Math.Min(co, 4))) + (co > 4 ? $" +{co - 4} daha" : "") : ""
                      }.Where(x => x.Length > 0))
                    : "✅  Bugün için bekleyen giriş veya çıkış bulunmuyor. Tüm işlemler güncel.";

                pnlNotify.Controls.Add(new Label {
                    Text = urgent ? "⚠" : "ℹ",
                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                    ForeColor = accentColor, AutoSize = true,
                    Location = new Point(14, 12), BackColor = Color.Transparent
                });

                var lblText = new Label {
                    Text = text,
                    Font = new Font("Segoe UI", 9.5F, urgent ? FontStyle.Bold : FontStyle.Regular),
                    ForeColor = Color.White, AutoSize = true,
                    Location = new Point(42, 14), BackColor = Color.Transparent,
                    Cursor = urgent ? Cursors.Hand : Cursors.Default
                };
                if (urgent) lblText.Click += (s, e) => { SetActive(btnNavRezervasyon); ShowReservations(); };
                pnlNotify.Controls.Add(lblText);

                var btnClose = new Button {
                    Text = "✕", Size = new Size(36, 36),
                    FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
                    ForeColor = Color.FromArgb(140, 160, 200),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand, Dock = DockStyle.Right
                };
                btnClose.FlatAppearance.BorderSize = 0;
                btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 180, 30, 30);
                btnClose.Click += (s, e) => pnlNotify.Visible = false;
                pnlNotify.Controls.Add(btnClose);
            }
            catch (Exception ex)
            {
                pnlNotify.Controls.Add(new Label {
                    Text = $"⚠️  Bildirim yüklenemedi: {ex.Message}",
                    Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(255, 180, 80),
                    AutoSize = true, Location = new Point(10, 15), BackColor = Color.Transparent
                });
            }

            pnlNotify.Visible = true;
            pnlNotify.BringToFront();
        }

        private void SetupNav()
        {
            pnlMainContent.BackColor = cBg;

            // ── LEFT SIDEBAR ─────────────────────────────────────────────────
            pnlHeader.BackColor = cSidebar;
            pnlHeader.Width = 250;
            pnlHeader.Controls.Clear();
            // Sağ kenar ince altın çizgi
            pnlHeader.Paint += (s, e) => {
                e.Graphics.FillRectangle(
                    new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(248, 0, 2, pnlHeader.Height),
                        Color.FromArgb(120, 218, 165, 32), Color.FromArgb(0, 218, 165, 32),
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical),
                    248, 0, 2, pnlHeader.Height);
            };

            // ── LOGO ALANI ────────────────────────────────────────────────────
            var pnlLogo = new Panel { Location = new Point(0, 0), Size = new Size(250, 78), BackColor = Color.FromArgb(7, 10, 20) };
            pnlLogo.Paint += (s, e) => {
                var g = e.Graphics;
                using var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(pnlLogo.ClientRectangle,
                    Color.FromArgb(30, 218, 165, 32), Color.Transparent, System.Drawing.Drawing2D.LinearGradientMode.Vertical);
                g.FillRectangle(gradBrush, pnlLogo.ClientRectangle);
                g.FillRectangle(new SolidBrush(Color.FromArgb(60, 218, 165, 32)), 0, pnlLogo.Height - 1, pnlLogo.Width, 1);
            };
            var picLogo = new PictureBox { Location = new Point(12, 12), Size = new Size(54, 54), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            try { picLogo.Image = Image.FromFile(System.IO.Path.Combine(Application.StartupPath, "Resources", "logo.png")); } catch { }
            var lblHotelName = new Label { Text = "AFM GRAND", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = cGold, Location = new Point(72, 13), AutoSize = true, BackColor = Color.Transparent };
            var lblHotelSub  = new Label { Text = "HOTEL MANAGEMENT", Font = new Font("Segoe UI", 6.5F, FontStyle.Bold), ForeColor = cSubText, Location = new Point(73, 40), AutoSize = true, BackColor = Color.Transparent };
            var lblStars     = new Label { Text = "★★★★★", Font = new Font("Segoe UI", 7F), ForeColor = cGold, Location = new Point(73, 54), AutoSize = true, BackColor = Color.Transparent };
            pnlLogo.Controls.AddRange(new Control[] { picLogo, lblHotelName, lblHotelSub, lblStars });
            pnlHeader.Controls.Add(pnlLogo);

            // ── KULLANICI PROFİLİ ─────────────────────────────────────────────
            var avatarInit = (AuthHelper.CurrentUser?.Username ?? "U").Substring(0, 1).ToUpper();
            var pnlUser = new Panel { Location = new Point(0, 78), Size = new Size(250, 72), BackColor = Color.FromArgb(12, 18, 36), Cursor = Cursors.Hand };
            pnlUser.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var br = new System.Drawing.Drawing2D.LinearGradientBrush(pnlUser.ClientRectangle,
                    Color.FromArgb(25, 218, 165, 32), Color.Transparent, System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
                g.FillRectangle(br, pnlUser.ClientRectangle);
                g.FillRectangle(new SolidBrush(Color.FromArgb(40, 218, 165, 32)), 0, pnlUser.Height - 1, pnlUser.Width, 1);
            };
            // Avatar dairesi
            var pnlAv = new Panel { Location = new Point(14, 16), Size = new Size(40, 40), BackColor = Color.Transparent };
            pnlAv.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var br = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, 40, 40),
                    Color.FromArgb(80, 110, 220), Color.FromArgb(50, 70, 180), System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                g.FillEllipse(br, 0, 0, 39, 39);
                g.DrawString(avatarInit, new Font("Segoe UI", 16F, FontStyle.Bold),
                    Brushes.White, new RectangleF(0, 0, 39, 39),
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            };
            // Online nokta
            var pnlDot = new Panel { Location = new Point(42, 44), Size = new Size(12, 12), BackColor = Color.Transparent };
            pnlDot.Paint += (s, e) => { var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias; g.FillEllipse(new SolidBrush(Color.FromArgb(34, 197, 94)), 0, 0, 11, 11); };
            var lblUsername = new Label { Text = AuthHelper.CurrentUser?.Username ?? "Kullanıcı", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(62, 16), AutoSize = true, BackColor = Color.Transparent };
            var lblRole     = new Label { Text = "● " + Role, Font = new Font("Segoe UI", 8F), ForeColor = Color.FromArgb(34, 197, 94), Location = new Point(62, 36), AutoSize = true, BackColor = Color.Transparent };
            pnlUser.Controls.AddRange(new Control[] { pnlAv, pnlDot, lblUsername, lblRole });
            EventHandler logoutHandler = (s, e) => {
                if (MessageBox.Show("Oturumu kapatmak istiyor musunuz?", "Çıkış", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    this.Close();
            };
            pnlUser.Click += logoutHandler;
            foreach (Control c in pnlUser.Controls) c.Click += logoutHandler;
            pnlHeader.Controls.Add(pnlUser);

            // ── BİLDİRİM BUTONU ───────────────────────────────────────────────
            btnNotifications = new Button {
                Text = "🔔", Font = new Font("Segoe UI Emoji", 11F),
                Size = new Size(36, 36), Location = new Point(206, 84),
                FlatStyle = FlatStyle.Flat, ForeColor = cText,
                BackColor = Color.FromArgb(22, 32, 58), Cursor = Cursors.Hand
            };
            btnNotifications.FlatAppearance.BorderSize = 0;
            btnNotifications.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 50, 85);
            btnNotifications.Click += (s, e) => ToggleNotificationCenter();
            lblNotifyBadge = new Label {
                Text = "0", Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                BackColor = cRed, ForeColor = Color.White,
                Size = new Size(16, 16), Location = new Point(226, 82),
                TextAlign = ContentAlignment.MiddleCenter, Visible = false
            };
            pnlHeader.Controls.AddRange(new Control[] { btnNotifications, lblNotifyBadge });

            // ── NAV BÖLÜM BAŞLIĞI ─────────────────────────────────────────────
            var lblNavSection = new Label {
                Text = "MENÜ",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 90, 130),
                Location = new Point(20, 158), AutoSize = true, BackColor = Color.Transparent
            };
            pnlHeader.Controls.Add(lblNavSection);

            // ── NAV BUTONLARI ─────────────────────────────────────────────────
            pnlNavbar.Location = new Point(0, 174);
            pnlNavbar.Size = new Size(250, 620);
            pnlNavbar.BackColor = Color.Transparent;
            pnlNavbar.Controls.Clear();
            pnlHeader.Controls.Add(pnlNavbar);

            int navY = 4;
            btnNavDashboard   = AddSideNavBtn("📊", "Dashboard",        ref navY);
            btnOnline         = AddSideNavBtn("🌐", "Online Talepler",  ref navY);
            btnNavMisafirler  = AddSideNavBtn("👥", "Misafirler",       ref navY);
            btnNavOdalar      = AddSideNavBtn("🛏", "Odalar",            ref navY);
            btnNavRezervasyon = AddSideNavBtn("📅", "Rezervasyon",      ref navY);
            btnNavOdeme       = AddSideNavBtn("💳", "Ödeme",             ref navY);
            btnNavMonitoring  = AddSideNavBtn("👁️", "Misafir İzleme",     ref navY);

            // ── Dropdown Grup: OPERASYON ─────────────────────────────────────
            var sep1 = new Panel { Location = new Point(16, navY + 4), Size = new Size(218, 1), BackColor = Color.FromArgb(28, 40, 70) };
            pnlNavbar.Controls.Add(sep1);
            navY += 12;

            var opsButtons = new List<Button>();
            int opsBtnY = navY;
            // Placeholder buttons — geçici y; gerçek y AddDropdownGroup ile atanır
            btnNavPersonel    = CreateNavButton("🧑", "Personel");
            btnNavRaporlar    = CreateNavButton("📈", "Raporlar");
            btnNavMaintenance = CreateNavButton("🔧", "Bakım & Teknik");
            opsButtons.AddRange(new[] { btnNavPersonel, btnNavRaporlar, btnNavMaintenance });
            AddDropdownGroup("⚙️", "Operasyon & Yönetim", opsButtons, ref navY, startOpen: false);

            // ── Dropdown Grup: HİZMETLER ────────────────────────────────────
            var sep2 = new Panel { Location = new Point(16, navY + 4), Size = new Size(218, 1), BackColor = Color.FromArgb(28, 40, 70) };
            pnlNavbar.Controls.Add(sep2);
            navY += 12;

            var svcButtons = new List<Button>();
            btnNavHousekeeping = CreateNavButton("🧹", "Kat Hizmetleri");
            var btnNavRestoran  = CreateNavButton("🍽", "Restoran");
            svcButtons.AddRange(new[] { btnNavHousekeeping, btnNavRestoran });
            AddDropdownGroup("🛎", "Hizmetler", svcButtons, ref navY, startOpen: false);

            // Rozetler
            lblOnlineBadge      = AddSideBadge(btnOnline);
            lblMaintenanceBadge = AddSideBadge(btnNavMaintenance);

            // Versiyon + çıkış
            var pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 52, BackColor = Color.FromArgb(7, 10, 20) };
            pnlBottom.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(30, 218, 165, 32)), 0, 0, pnlBottom.Width, 1);
            var btnLogout = new Button {
                Text = "⏻  Oturumu Kapat",
                Font = new Font("Segoe UI", 9F),
                Size = new Size(250, 34), Location = new Point(0, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(120, 140, 175),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 30, 30);
            btnLogout.MouseEnter += (s, e) => btnLogout.ForeColor = Color.FromArgb(239, 68, 68);
            btnLogout.MouseLeave += (s, e) => btnLogout.ForeColor = Color.FromArgb(120, 140, 175);
            btnLogout.Click += logoutHandler;
            pnlBottom.Controls.Add(btnLogout);
            pnlHeader.Controls.Add(pnlBottom);

            // ── Event Atamaları ───────────────────────────────────────────────
            btnNavDashboard.Click   += (s, e) => { SetActive(btnNavDashboard);   ShowDashboard(); };
            btnOnline.Click         += (s, e) => { SetActive(btnOnline);         ShowOnlineReservations(); };
            btnNavMisafirler.Click  += (s, e) => { SetActive(btnNavMisafirler);  ShowGuests(); };
            btnNavOdalar.Click      += (s, e) => { SetActive(btnNavOdalar);      ShowRooms(); };
            btnNavRezervasyon.Click += (s, e) => { SetActive(btnNavRezervasyon); ShowReservations(); };
            btnNavOdeme.Click       += (s, e) => { SetActive(btnNavOdeme);       ShowPayments(); };
            btnNavMonitoring.Click  += (s, e) => { SetActive(btnNavMonitoring);  ShowGuestMonitoring(); };
            btnNavPersonel.Click    += (s, e) => { SetActive(btnNavPersonel);    ShowPersonel(); };
            btnNavRaporlar.Click    += (s, e) => { SetActive(btnNavRaporlar);    ShowReports(); };
            btnNavMaintenance.Click += (s, e) => { SetActive(btnNavMaintenance); ShowMaintenance(); };
            btnNavHousekeeping.Click+= (s, e) => { SetActive(btnNavHousekeeping); ShowHousekeeping(); };
            btnNavRestoran.Click    += (s, e) => { SetActive(btnNavRestoran);    ShowRestaurant(); };

            SetActive(btnNavDashboard);
        }

        private Label AddSideBadge(Button parent)
        {
            var lbl = new Label {
                Text = "!", Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                BackColor = cRed, ForeColor = Color.White,
                Size = new Size(17, 17), Location = new Point(parent.Width - 24, 8),
                TextAlign = ContentAlignment.MiddleCenter, Visible = false
            };
            parent.Controls.Add(lbl);
            return lbl;
        }

        /// <summary>Konumu yönetmek için eskiden kullanılan yardımcı — hâlâ üst menü butonları için geçerli.</summary>
        private Button AddSideNavBtn(string icon, string label, ref int y)
        {
            var b = CreateNavButton(icon, label);
            b.Location = new Point(0, y);
            pnlNavbar.Controls.Add(b);
            y += 48;
            return b;
        }

        /// <summary>Konum atamadan sadece stil oluşturur.</summary>
        private Button CreateNavButton(string icon, string label)
        {
            var b = new Button {
                Text = icon + "  " + label,
                Font = new Font("Segoe UI", 9.5F),
                Size = new Size(250, 46),
                FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(148, 165, 200), Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(18, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 28, 55);
            b.MouseEnter += (s, e) => { if (b != activeNav) b.ForeColor = Color.White; };
            b.MouseLeave += (s, e) => { if (b != activeNav) b.ForeColor = Color.FromArgb(148, 165, 200); };
            return b;
        }

        /// <summary>
        /// Genişleyip daralan bir dropdown nav grubu oluşturur.
        /// </summary>
        private void AddDropdownGroup(string icon, string groupLabel, List<Button> children, ref int y, bool startOpen = false)
        {
            bool isOpen = startOpen;
            int childrenH = children.Count * 44;

            // ── Grup başlığı (tıklanabilir) ──────────────────────────────────
            var btnGroup = new Button {
                Text = icon + "  " + groupLabel,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(250, 46), Location = new Point(0, y),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(14, 22, 44),
                ForeColor = Color.FromArgb(200, 215, 240),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            btnGroup.FlatAppearance.BorderSize = 0;
            btnGroup.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 34, 65);
            pnlNavbar.Controls.Add(btnGroup);

            // Sağdaki ok etiketi
            var lblArrow = new Label {
                Text = isOpen ? "▼" : "▶",
                Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                ForeColor = cGold,
                AutoSize = true,
                BackColor = Color.Transparent,
                Location = new Point(222, 17),
                Cursor = Cursors.Hand
            };
            btnGroup.Controls.Add(lblArrow);

            // Alt çizgi (grup başlığının altı)
            var groupLine = new Panel { Location = new Point(0, y + 46), Size = new Size(250, 2), BackColor = Color.FromArgb(30, 218, 165, 32) };
            pnlNavbar.Controls.Add(groupLine);

            y += 48;

            // ── Çocuk Panel (açılıp kapanır) ─────────────────────────────────
            var pnlChildren = new Panel {
                Location = new Point(0, y),
                Size = new Size(250, isOpen ? childrenH : 0),
                BackColor = Color.FromArgb(9, 14, 28),
                Visible = isOpen
            };
            pnlNavbar.Controls.Add(pnlChildren);

            // Çocuk butonları panele ekle
            int cy = 0;
            foreach (var child in children)
            {
                child.Location = new Point(0, cy);
                child.Size = new Size(250, 44);
                // Hafif girinti
                child.Padding = new Padding(30, 0, 0, 0);
                // Arka plan sol çizgisi (ince altın)
                child.Paint += (s, pe) => {
                    pe.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 218, 165, 32)), 16, 8, 2, 28);
                };
                pnlChildren.Controls.Add(child);
                cy += 44;
            }

            // ── Toggle Animasyonu ─────────────────────────────────────────────
            var animTimer = new System.Windows.Forms.Timer { Interval = 12 };
            int targetH = 0;
            var controlsBelow = new List<Control>();

            animTimer.Tick += (s, e) => {
                int cur = pnlChildren.Height;
                int step = Math.Max(1, Math.Abs(targetH - cur) / 3);
                int actualStep = targetH > cur ? step : -step;

                if (Math.Abs(cur - targetH) <= step) {
                    actualStep = targetH - cur;
                    pnlChildren.Height = targetH;
                    pnlChildren.Visible = targetH > 0;
                    animTimer.Stop();
                } else {
                    pnlChildren.Height += actualStep;
                    pnlChildren.Visible = true;
                }

                foreach (Control c in controlsBelow) {
                    c.Top += actualStep;
                }
            };

            Action toggle = () => {
                if (animTimer.Enabled) return; // Spam tıklamayı engelle

                isOpen = !isOpen;
                lblArrow.Text = isOpen ? "▼" : "▶";
                targetH = isOpen ? childrenH : 0;
                
                controlsBelow.Clear();
                foreach (Control c in pnlNavbar.Controls) {
                    // PnlChildren'ın altındaki veya onunla aynı hizada başlayan diğer kontrolleri bul
                    if (c != pnlChildren && c.Top >= pnlChildren.Top) {
                        controlsBelow.Add(c);
                    }
                }
                
                animTimer.Start();
            };

            btnGroup.Click += (s, e) => toggle();
            lblArrow.Click += (s, e) => toggle();

            // Başlangıçta açıksa placeholder y'yi güncelle
            if (isOpen) y += childrenH;
        }

        // Eski imza uyumu
        private Button AddSideNavBtn(string text, ref int y) => AddSideNavBtn("", text, ref y);
        private Button AddNavBtn(string text, ref int x) => AddSideNavBtn("", text, ref x);

        private void SetActive(Button b)
        {
            if (activeNav != null)
            {
                activeNav.ForeColor = Color.FromArgb(148, 165, 200);
                activeNav.Font = new Font("Segoe UI", 9.5F);
                activeNav.BackColor = Color.Transparent;
                activeNav.Paint -= NavBtn_Paint;
                activeNav.Invalidate();
            }
            activeNav = b;
            activeNav.ForeColor = Color.White;
            activeNav.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            activeNav.BackColor = Color.FromArgb(16, 26, 52);
            activeNav.Paint += NavBtn_Paint;
            activeNav.Invalidate();
        }

        private void NavBtn_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Button b)
            {
                // Sol altın vurgu çubuğu
                using var br = new LinearGradientBrush(
                    new Rectangle(0, 6, 4, b.Height - 12),
                    cGold, cGoldHover, LinearGradientMode.Vertical);
                e.Graphics.FillRectangle(br, 0, 6, 4, b.Height - 12);
                // Hafif sağ parlama
                using var glow = new LinearGradientBrush(
                    new Rectangle(0, 0, b.Width, b.Height),
                    Color.FromArgb(18, 218, 165, 32), Color.Transparent,
                    LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(glow, 0, 0, b.Width, b.Height);
            }
        }

        /// <summary>
        /// Bekleyen online talep sayısını rozette günceller.
        /// </summary>
        private void UpdateOnlineBadge()
        {
            try
            {
                int count = OnlineReservationHelper.GetPendingCount();
                if (lblOnlineBadge != null)
                {
                    lblOnlineBadge.Text = count.ToString();
                    lblOnlineBadge.Visible = count > 0;
                }

                // Yeni talep varsa bildirim gönder
                if (count > lastPendingCount)
                {
                    notifyIcon.ShowBalloonTip(3000, "Yeni Online Rezervasyon", $"Şu an bekleyen {count} adet online rezervasyon talebi bulunmaktadır.", ToolTipIcon.Info);
                }
                lastPendingCount = count;
            }
            catch { }
        }

        private void UpdateDirtyBadge()
        {
            try
            {
                int count = HousekeepingHelper.GetDirtyRoomCount();
                if (lblDirtyBadge != null)
                {
                    lblDirtyBadge.Text = count.ToString();
                    lblDirtyBadge.Visible = count > 0;
                }
                lastDirtyCount = count;
            }
            catch { }
        }

        private void UpdateMaintenanceBadge()
        {
            try
            {
                int count = MaintenanceHelper.GetPendingCount();
                if (lblMaintenanceBadge != null)
                {
                    lblMaintenanceBadge.Text = count.ToString();
                    lblMaintenanceBadge.Visible = count > 0;
                }
            }
            catch { }
        }

        private void ToggleNotificationCenter()
        {
            if (pnlNotificationCenter != null && pnlNotificationCenter.Visible)
            {
                pnlNotificationCenter.Visible = false;
                return;
            }

            if (pnlNotificationCenter == null)
            {
                pnlNotificationCenter = new Panel
                {
                    Size = new Size(350, 450),
                    BackColor = Color.FromArgb(25, 35, 60),
                    BorderStyle = BorderStyle.FixedSingle,
                    Visible = false
                };
                this.Controls.Add(pnlNotificationCenter);
                pnlNotificationCenter.BringToFront();
            }

            // Pozisyonu butonun altına ayarla
            var btnPoint = btnNotifications!.PointToScreen(Point.Empty);
            var formPoint = this.PointToClient(btnPoint);
            pnlNotificationCenter.Location = new Point(formPoint.X - pnlNotificationCenter.Width + btnNotifications.Width, formPoint.Y + btnNotifications.Height + 5);

            RefreshNotificationCenter();
            pnlNotificationCenter.Visible = true;
        }

        private void RefreshNotificationCenter()
        {
            if (pnlNotificationCenter == null) return;
            pnlNotificationCenter.Controls.Clear();

            // Başlık
            var lblTitle = new Label { Text = "🔔 Bildirimler", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = cGold, Location = new Point(10, 10), AutoSize = true };
            pnlNotificationCenter.Controls.Add(lblTitle);

            var btnClear = new Button { Text = "Temizle", Size = new Size(70, 25), Location = new Point(pnlNotificationCenter.Width - 80, 10), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Segoe UI", 8F) };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => { notificationList.Clear(); RefreshNotificationCenter(); UpdateNotifyBadge(); };
            pnlNotificationCenter.Controls.Add(btnClear);

            var pnlList = new Panel { Location = new Point(0, 45), Size = new Size(pnlNotificationCenter.Width, pnlNotificationCenter.Height - 45), AutoScroll = true };
            pnlNotificationCenter.Controls.Add(pnlList);

            if (notificationList.Count == 0)
            {
                pnlList.Controls.Add(new Label { Text = "Henüz bildirim yok.", ForeColor = Color.Gray, Location = new Point(10, 20), AutoSize = true });
            }
            else
            {
                int yy = 0;
                foreach (var note in notificationList)
                {
                    var pNote = new Panel { Size = new Size(pnlList.Width - 25, 60), Location = new Point(5, yy), BackColor = Color.FromArgb(35, 45, 75) };
                    pNote.Controls.Add(new Label { Text = note, ForeColor = Color.White, Font = new Font("Segoe UI", 9F), Dock = DockStyle.Fill, Padding = new Padding(5), TextAlign = ContentAlignment.MiddleLeft });
                    pnlList.Controls.Add(pNote);
                    yy += 65;
                }
            }
        }

        private void SwitchPanel(string txt)
        {
            if (txt.Contains("Dashboard")) ShowDashboard();
            else if (txt.Contains("Misafir")) ShowGuests();
            else if (txt.Contains("Online")) ShowOnlineReservations();
            else if (txt.Contains("Kat Hizmet") || txt.Contains("Housekeeping")) ShowHousekeeping();
            else if (txt.Contains("Restoran") || txt.Contains("Restaurant")) ShowRestaurant();
            else if (txt.Contains("Teknik") || txt.Contains("Maintenance") || txt.Contains("Servis")) ShowMaintenance();
            else if (txt.Contains("Stok") || txt.Contains("Inventory") || txt.Contains("Envanter")) ShowInventory();
            else if (txt.Contains("Oda")) ShowRooms();
            else if (txt.Contains("Rezerv")) ShowReservations();
            else if (txt.Contains("Ödeme") || txt.Contains("deme")) ShowPayments();
            else if (txt.Contains("Personel")) ShowPersonel();
            else if (txt.Contains("Rapor")) ShowReports();
        }

        private void ClearContent() { pnlMainContent.Controls.Clear(); }

        private Panel MakeTitle(string t)
        {
            int w = pnlMainContent.ClientSize.Width - 40;
            var pnl = new Panel { Location = new Point(0, 0), Size = new Size(w + 40, 58), BackColor = Color.Transparent };

            // Başlık metni
            var lbl = new Label {
                Text = t, Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true,
                Location = new Point(0, 8), BackColor = Color.Transparent
            };
            pnl.Controls.Add(lbl);

            // Canlı saat (sağ üst)
            var lblClock = new Label {
                Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm"),
                Font = new Font("Segoe UI", 9.5F), ForeColor = cSubText,
                AutoSize = true, BackColor = Color.Transparent,
                Location = new Point(w - 160, 16)
            };
            pnl.Controls.Add(lblClock);
            var clockTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            clockTimer.Tick += (s, e) => lblClock.Text = DateTime.Now.ToString("dd MMM yyyy  HH:mm");
            clockTimer.Start();

            // Alt altın çizgi
            pnl.Paint += (s, e) => {
                using var br = new LinearGradientBrush(
                    new Rectangle(0, 52, w + 40, 2),
                    cGold, Color.Transparent, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(br, 0, 52, w + 40, 2);
            };

            return pnl;
        }
        private DataGridView MakeGrid(int y, int h) {
            var g = new DataGridView {
                Location = new Point(0, y),
                BackgroundColor = Color.FromArgb(14, 20, 38),
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(24, 36, 62),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ScrollBars = ScrollBars.Both
            };
            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(16, 24, 48),
                ForeColor = cGold,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                SelectionBackColor = Color.FromArgb(16, 24, 48),
                Padding = new Padding(6, 0, 0, 0)
            };
            g.ColumnHeadersHeight = 38;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.DefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(14, 20, 38),
                ForeColor = cText,
                SelectionBackColor = Color.FromArgb(25, 40, 75),
                SelectionForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F),
                Padding = new Padding(4, 0, 0, 0)
            };
            g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle {
                BackColor = Color.FromArgb(16, 23, 44),
                ForeColor = cText,
                SelectionBackColor = Color.FromArgb(25, 40, 75)
            };
            g.RowTemplate.Height = 40;
            return g;
        }
        private Button MakeBtn(string t, Color bg, int x, int y)
        {
            var b = new Button {
                Text = t, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Size = new Size(150, 38), Location = new Point(x, y),
                FlatStyle = FlatStyle.Flat, BackColor = bg,
                ForeColor = Color.White, Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            b.MouseEnter += (s, e) => b.BackColor = ControlPaint.Light(bg, 0.15f);
            b.MouseLeave += (s, e) => b.BackColor = bg;
            return b;
        }
        private Panel MakeStatCard(string icon, string title, string val, int x, Color tintColor) {
            var p = new Panel { Size = new Size(220, 118), Location = new Point(x, 62), BackColor = Color.FromArgb(16, 22, 40) };

            p.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // Gradient arka plan
                using var bgBrush = new LinearGradientBrush(new Rectangle(0, 0, p.Width, p.Height),
                    Color.FromArgb(38, tintColor), Color.FromArgb(8, tintColor),
                    LinearGradientMode.ForwardDiagonal);
                g.FillRectangle(bgBrush, 0, 0, p.Width, p.Height);
                // Üst renkli şerit
                using var topBrush = new LinearGradientBrush(new Rectangle(0, 0, p.Width, 3),
                    tintColor, Color.FromArgb(0, tintColor), LinearGradientMode.Horizontal);
                g.FillRectangle(topBrush, 0, 0, p.Width, 3);
                // Çerçeve
                using var borderPen = new Pen(Color.FromArgb(55, tintColor), 1);
                g.DrawRectangle(borderPen, 0, 0, p.Width - 1, p.Height - 1);
            };

            // İkon arka plan dairesi
            var pnlIconBg = new Panel { Size = new Size(52, 52), Location = new Point(14, 28), BackColor = Color.Transparent };
            pnlIconBg.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(Color.FromArgb(45, tintColor));
                g.FillEllipse(br, 0, 0, 51, 51);
            };
            var lblIcon = new Label {
                Text = icon, Font = new Font("Segoe UI Emoji", 20F),
                Size = new Size(52, 52), Location = new Point(0, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent, ForeColor = tintColor
            };
            pnlIconBg.Controls.Add(lblIcon);
            p.Controls.Add(pnlIconBg);

            // Değer
            var lblVal = new Label {
                Text = val, Font = new Font("Segoe UI", 24F, FontStyle.Bold),
                ForeColor = Color.White, AutoSize = true,
                Location = new Point(78, 18), BackColor = Color.Transparent, Name = "val"
            };
            p.Controls.Add(lblVal);

            // Başlık
            var lblTitle = new Label {
                Text = title, Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(160, 175, 205),
                AutoSize = true, Location = new Point(80, 60),
                BackColor = Color.Transparent
            };
            p.Controls.Add(lblTitle);

            // Alt renkli çizgi
            var pnlBar = new Panel { Size = new Size(p.Width, 3), Location = new Point(0, p.Height - 3), BackColor = Color.Transparent };
            pnlBar.Paint += (s, e) => {
                using var br = new LinearGradientBrush(new Rectangle(0, 0, pnlBar.Width, 3),
                    Color.FromArgb(100, tintColor), Color.Transparent, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(br, 0, 0, pnlBar.Width, 3);
            };
            p.Controls.Add(pnlBar);

            // Hover efekti
            p.MouseEnter += (s, e) => p.Invalidate();
            p.Cursor = Cursors.Hand;

            return p;
        }

        private Panel MakeRoomCard(Room r, int x, int y, int w) {
            Color statColor; string iconStr;
            if (r.CleaningStatus == "Dirty") {
                statColor = Color.FromArgb(239, 68, 68); iconStr = "🧹";
            } else {
                switch (r.Status) {
                    case "Available":   statColor = cGreen;  iconStr = "✓"; break;
                    case "Occupied":    statColor = cRed;    iconStr = "●"; break;
                    case "Maintenance": statColor = cYellow; iconStr = "⚙"; break;
                    case "Reserved":    statColor = cBlue;   iconStr = "◆"; break;
                    default:            statColor = cGreen;  iconStr = "✓"; break;
                }
            }

            var p = new Panel { Size = new Size(w, 68), Location = new Point(x, y), Cursor = Cursors.Hand, BackColor = Color.FromArgb(16, 22, 40) };

            bool hovered = false;
            p.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                // Arka plan
                g.FillRectangle(new SolidBrush(hovered ? Color.FromArgb(22, 32, 58) : Color.FromArgb(16, 22, 40)), 0, 0, w, 68);
                // Sol renkli şerit
                using var stripeBrush = new LinearGradientBrush(
                    new Rectangle(0, 8, 4, 52), statColor, Color.FromArgb(120, statColor), LinearGradientMode.Vertical);
                g.FillRectangle(stripeBrush, 0, 8, 4, 52);
                // Hafif arka plan rengi
                g.FillRectangle(new SolidBrush(Color.FromArgb(hovered ? 30 : 15, statColor)), 0, 0, w, 68);
                // Çerçeve
                using var pen = new Pen(hovered ? Color.FromArgb(80, statColor) : Color.FromArgb(30, 42, 65), 1);
                g.DrawRectangle(pen, 0, 0, w - 1, 67);
            };
            p.MouseEnter += (s, e) => { hovered = true; p.Invalidate(); };
            p.MouseLeave += (s, e) => { hovered = false; p.Invalidate(); };

            // Oda numarası
            var lblNo = new Label {
                Text = r.RoomNumber,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 8), AutoSize = true, BackColor = Color.Transparent
            };
            p.Controls.Add(lblNo);

            // Durum ikonu
            var lblIcon = new Label {
                Text = iconStr, Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = statColor, Location = new Point(w - 20, 6),
                AutoSize = true, BackColor = Color.Transparent
            };
            p.Controls.Add(lblIcon);

            // Durum metni
            string statText = r.CleaningStatus == "Dirty" ? "Kirli" :
                r.Status switch { "Available" => "Müsait", "Occupied" => "Dolu", "Maintenance" => "Bakımda", "Reserved" => "Rezerve", _ => "Müsait" };
            var lblStat = new Label {
                Text = statText, Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, statColor), Location = new Point(12, 38),
                AutoSize = true, BackColor = Color.Transparent
            };
            p.Controls.Add(lblStat);

            // Tooltip
            if (r.Status == "Occupied" || r.Status == "Reserved") {
                try {
                    var allRes = ReservationHelper.GetActiveReservations();
                    var res = allRes.FirstOrDefault(rv => rv.RoomId == r.Id);
                    if (res != null) {
                        var tip = new ToolTip { OwnerDraw = true, UseAnimation = true, UseFading = true };
                        tip.Draw += (s, e) => {
                            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(18, 26, 50)), e.Bounds);
                            e.Graphics.DrawRectangle(new Pen(Color.FromArgb(55, 75, 120), 1), e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                            var fT = new Font("Segoe UI", 9F, FontStyle.Bold); var fB = new Font("Segoe UI", 8.5F);
                            int ty = e.Bounds.Y + 8;
                            e.Graphics.DrawString(r.RoomNumber, fT, new SolidBrush(cGold), e.Bounds.X + 10, ty); ty += 20;
                            e.Graphics.DrawString($"👤 {res.GuestName}", fB, Brushes.White, e.Bounds.X + 10, ty); ty += 17;
                            e.Graphics.DrawString($"🚪 Çıkış: {res.CheckOutDate:dd.MM.yyyy}", fB, Brushes.LightGray, e.Bounds.X + 10, ty);
                            fT.Dispose(); fB.Dispose();
                        };
                        tip.Popup += (s, e) => { e.ToolTipSize = new Size(200, 80); };
                        tip.SetToolTip(p, "info"); tip.SetToolTip(lblNo, "info"); tip.SetToolTip(lblStat, "info");
                    }
                } catch { }
            }
            p.Click += (s, e) => ShowRoomDetails(r);
            foreach (Control c in p.Controls) c.Click += (s, e) => ShowRoomDetails(r);
            return p;
        }

        private void ShowRoomDetails(Room r)
        {
            var f = new Form { Text = $"Oda {r.RoomNumber} Detayı", Size = new Size(400, 300), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog };
            f.Controls.Add(new Label { Text = $"Oda No: {r.RoomNumber}", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) });
            f.Controls.Add(new Label { Text = $"Tip: {r.RoomTypeName}", Location = new Point(20, 55), AutoSize = true });
            f.Controls.Add(new Label { Text = $"Durum: {r.Status}", Location = new Point(20, 85), AutoSize = true });
            f.Controls.Add(new Label { Text = $"Temizlik: {r.CleaningStatus}", Location = new Point(20, 115), AutoSize = true });
            
            var btnGoToRooms = MakeBtn("Odalar Sayfasına Git", cBlue, 20, 180);
            btnGoToRooms.Size = new Size(200, 40);
            btnGoToRooms.Click += (s, e) => { f.Close(); SetActive(btnNavOdalar); ShowRooms(); };
            f.Controls.Add(btnGoToRooms);
            
            f.ShowDialog();
        }

        // ========== DASHBOARD ==========
        private void ShowDashboard()
        {
            try { RoomHelper.SyncRoomStatuses(); } catch { } // Veri tutarlılığını otomatik onar

            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📊 Dashboard"));

            // Veri Senkronizasyon Butonu (Onarıcı)
            var btnSync = new Button { 
                Text = "🔄 Verileri Onar", 
                Location = new Point(230, 20), 
                Size = new Size(130, 32), 
                FlatStyle = FlatStyle.Flat, 
                BackColor = Color.FromArgb(45, 55, 85), 
                ForeColor = cGold, 
                Font = new Font("Segoe UI", 9F, FontStyle.Bold), 
                Cursor = Cursors.Hand 
            };
            btnSync.FlatAppearance.BorderSize = 0;
            btnSync.Click += (s, e) => {
                RoomHelper.SyncRoomStatuses();
                MessageBox.Show("Oda durumları rezervasyon verileriyle karşılaştırıldı ve tüm tutarsızlıklar başarıyla onarıldı.", "Veri Senkronizasyonu Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowDashboard();
            };
            pnlMainContent.Controls.Add(btnSync);
            int w = pnlMainContent.ClientSize.Width - 40;

            // === İSTATİSTİK KARTLARI ===
            int totalRooms = RoomHelper.GetRoomCount();
            int occupied = RoomHelper.GetRoomCount("Occupied");
            int available = RoomHelper.GetRoomCount("Available");
            decimal revenue = PaymentHelper.GetDailyRevenue(DateTime.Today);
            int onlinePending = OnlineReservationHelper.GetPendingCount();

            int cardW = (w - 40) / 5;
            pnlMainContent.Controls.Add(MakeStatCard("🛏️", "Toplam Oda", totalRooms.ToString(), 20, Color.FromArgb(100, 149, 237)));
            pnlMainContent.Controls.Add(MakeStatCard("🛌", "Dolu Oda", occupied.ToString(), 20 + cardW + 10, Color.FromArgb(239, 83, 80)));
            pnlMainContent.Controls.Add(MakeStatCard("🔑", "Müsait Oda", available.ToString(), 20 + (cardW + 10) * 2, Color.FromArgb(129, 199, 132)));
            pnlMainContent.Controls.Add(MakeStatCard("💰", "Günlük Gelir", $"₺{revenue:N0}", 20 + (cardW + 10) * 3, Color.FromArgb(255, 183, 77)));
            
            var onlineCard = MakeStatCard("🌐", "Online Talep", onlinePending.ToString(), 20 + (cardW + 10) * 4, Color.FromArgb(158, 158, 158));
            onlineCard.Cursor = Cursors.Hand;
            if (onlinePending > 0) onlineCard.BackColor = Color.FromArgb(60, 30, 30);
            onlineCard.Click += (s, e) => { ShowOnlineReservations(); };
            foreach (Control c in onlineCard.Controls) c.Click += (s2, e2) => { ShowOnlineReservations(); };
            pnlMainContent.Controls.Add(onlineCard);

            // === KİRLİ ODA UYARISI (Varsa) ===
            int dirtyCount = HousekeepingHelper.GetDirtyRoomCount();
            if (dirtyCount > 0)
            {
                var pnlDirtyAlert = new Panel { Size = new Size(w, 40), Location = new Point(20, 165), BackColor = Color.FromArgb(80, 229, 115, 115) };
                var lblDirty = new Label { Text = $"⚠️ DİKKAT: Şu an temizlenmesi gereken {dirtyCount} adet kirli oda bulunuyor!", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(10, 10), BackColor = Color.Transparent };
                pnlDirtyAlert.Controls.Add(lblDirty);
                var btnGoHk = new Button { Text = "Temizliğe Git →", Size = new Size(120, 26), Location = new Point(w - 130, 7), FlatStyle = FlatStyle.Flat, BackColor = cGold, ForeColor = Color.Black, Font = new Font("Segoe UI", 8F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnGoHk.FlatAppearance.BorderSize = 0;
                btnGoHk.Click += (s, e) => { SetActive(btnNavHousekeeping!); ShowHousekeeping(); };
                pnlDirtyAlert.Controls.Add(btnGoHk);
                pnlMainContent.Controls.Add(pnlDirtyAlert);
            }

            // === ODA DURUMU GRID (KAT BAZINDA GRUPLANMIŞ) ===
            int roomGridBottom = 420;
            try
            {
                var rooms = RoomHelper.GetAllRooms();
                var groupedRooms = rooms.GroupBy(r => r.Floor).OrderBy(g => g.Key).ToList();
                
                int perRow = 11; 
                int cardW2 = (w - (perRow - 1) * 6 - 10) / perRow;
                
                // Toplam yükseklik tahmini: her kat için bir başlık + kart satırları
                int gridH = 40;
                foreach (var g in groupedRooms)
                {
                    int roomsInGroup = g.Count();
                    int rowsInGroup = (int)Math.Ceiling(roomsInGroup / (double)perRow);
                    gridH += 30 + rowsInGroup * 62 + 15;
                }

                var pnlRooms = new Panel { Location = new Point(20, dirtyCount > 0 ? 215 : 175), Size = new Size(w, gridH), BackColor = Color.Transparent };
                pnlRooms.Controls.Add(new Label { Text = "Kat Bazında Oda Görünümü", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(200, 210, 230), AutoSize = true, Location = new Point(0, 0), BackColor = Color.Transparent });
                
                int currentY = 35;
                foreach (var g in groupedRooms)
                {
                    int floorNum = g.Key;
                    var lblFloor = new Label { 
                        Text = $"🏢 {floorNum}. Kat", 
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold), 
                        ForeColor = cGold, 
                        AutoSize = true, 
                        Location = new Point(5, currentY), 
                        BackColor = Color.Transparent 
                    };
                    pnlRooms.Controls.Add(lblFloor);
                    currentY += 24;
                    
                    int rx = 5;
                    int count = 0;
                    var sortedRooms = g.OrderBy(r => r.RoomNumber);
                    foreach (var room in sortedRooms)
                    {
                        pnlRooms.Controls.Add(MakeRoomCard(room, rx, currentY, cardW2));
                        count++;
                        rx += cardW2 + 6;
                        if (count % perRow == 0 && count < g.Count())
                        {
                            rx = 5;
                            currentY += 62;
                        }
                    }
                    currentY += 62 + 15; // Bir sonraki kat için boşluk
                }
                
                pnlMainContent.Controls.Add(pnlRooms);
                roomGridBottom = (dirtyCount > 0 ? 215 : 175) + gridH + 20;
            }
            catch { }

            // === BUGÜN GİRİŞ & ÇIKIŞ LİSTELERİ ===
            int halfW = (w - 15) / 2;
            
            // Giriş Listesi (Beklenen Girişler)
            var ciPanel = new Panel { Location = new Point(20, roomGridBottom), Size = new Size(halfW, 300), BackColor = Color.Transparent };
            ciPanel.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 75), 1), 0,0, ciPanel.Width-1, ciPanel.Height-1); };
            ciPanel.Controls.Add(new Label { Text = "BU GÜNÜN GİRİŞ LİSTESİ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(170,180,200), AutoSize = true, Location = new Point(10, 10), BackColor = Color.Transparent });

            try {
                var arrivals = ReservationHelper.GetFilteredCheckIns(DateTime.Today, DateTime.Today, null);
                if (arrivals.Count == 0) {
                    var lblEmpty = new Label { Text = "🚪🧳\n\nBeklenen Giriş Listesi Boş", Font = new Font("Segoe UI", 11F), ForeColor = Color.Gray, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(halfW, 250), Location = new Point(0, 40) };
                    ciPanel.Controls.Add(lblEmpty);
                } else {
                    var dgCI = MakeGrid(35, 260); dgCI.Size = new Size(halfW - 2, 260); dgCI.Location = new Point(1, 35);
                    dgCI.BackgroundColor = Color.Transparent; dgCI.RowTemplate.Height = 40;
                    dgCI.Columns.AddRange(new DataGridViewColumn[] { new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=50}, new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir Adı"}, new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Giriş Tarihi",Width=100}, new DataGridViewTextBoxColumn{Name="CTarih",HeaderText="Çıkış Tarihi",Width=100} });
                    var btnColCI = new DataGridViewButtonColumn { Name = "Islem", HeaderText = "Aksiyon", Width = 90, FlatStyle = FlatStyle.Flat };
                    dgCI.Columns.Add(btnColCI);
                    foreach (var r in arrivals) {
                        int idx = dgCI.Rows.Add(r.RoomNumber, r.GuestName, $"{r.CheckInDate:dd.MM.yyyy}", $"{r.CheckOutDate:dd.MM.yyyy}");
                        dgCI.Rows[idx].Cells["Islem"].Value = "Giriş Yap";
                        dgCI.Rows[idx].Cells["Islem"].Style.BackColor = Color.FromArgb(161, 110, 80); // Brownish matching the UI
                        dgCI.Rows[idx].Cells["Islem"].Style.ForeColor = Color.White;
                    }
                    dgCI.CellClick += (s, e) => {
                        if (e.ColumnIndex >= 0 && dgCI.Columns[e.ColumnIndex].Name == "Islem" && e.RowIndex >= 0 && e.RowIndex < arrivals.Count) {
                            if (MessageBox.Show($"{arrivals[e.RowIndex].GuestName} için giriş yapılsın mı?", "Giriş", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                                ReservationHelper.CheckIn(arrivals[e.RowIndex].Id, arrivals[e.RowIndex].RoomId); ShowDashboard();
                            }
                        }
                    };
                    ciPanel.Controls.Add(dgCI);
                }
            } catch { }
            pnlMainContent.Controls.Add(ciPanel);

            // Çıkış Listesi (Beklenen Çıkışlar)
            var coPanel = new Panel { Location = new Point(35 + halfW, roomGridBottom), Size = new Size(halfW, 300), BackColor = Color.Transparent };
            coPanel.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 75), 1), 0,0, coPanel.Width-1, coPanel.Height-1); };
            coPanel.Controls.Add(new Label { Text = "BU GÜNÜN ÇIKIŞ LİSTESİ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(170,180,200), AutoSize = true, Location = new Point(10, 10), BackColor = Color.Transparent });
            try {
                var departures = ReservationHelper.GetFilteredCheckOuts(DateTime.Today, DateTime.Today, null);
                if (departures.Count == 0) {
                    var lblEmpty = new Label { Text = "🛫\n\nBeklenen Çıkış Listesi Boş", Font = new Font("Segoe UI", 11F), ForeColor = Color.Gray, AutoSize = false, TextAlign = ContentAlignment.MiddleCenter, Size = new Size(halfW, 250), Location = new Point(0, 40) };
                    coPanel.Controls.Add(lblEmpty);
                } else {
                    var dgCO = MakeGrid(35, 260); dgCO.Size = new Size(halfW - 2, 260); dgCO.Location = new Point(1, 35);
                    dgCO.BackgroundColor = Color.Transparent; dgCO.RowTemplate.Height = 40;
                    dgCO.Columns.AddRange(new DataGridViewColumn[] { new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=50}, new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir Adı"}, new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Giriş Tarihi",Width=100}, new DataGridViewTextBoxColumn{Name="CTarih",HeaderText="Çıkış Tarihi",Width=100} });
                    var btnColCO = new DataGridViewButtonColumn { Name = "Islem", HeaderText = "Aksiyon", Width = 90, FlatStyle = FlatStyle.Flat };
                    dgCO.Columns.Add(btnColCO);
                    foreach (var r in departures) {
                        int idx = dgCO.Rows.Add(r.RoomNumber, r.GuestName, $"{r.CheckInDate:dd.MM.yyyy}", $"{r.CheckOutDate:dd.MM.yyyy}");
                        dgCO.Rows[idx].Cells["Islem"].Value = "Çıkış Yap";
                        dgCO.Rows[idx].Cells["Islem"].Style.BackColor = Color.FromArgb(161, 110, 80);
                        dgCO.Rows[idx].Cells["Islem"].Style.ForeColor = Color.White;
                    }
                    dgCO.CellClick += (s, e) => {
                        if (e.ColumnIndex >= 0 && dgCO.Columns[e.ColumnIndex].Name == "Islem" && e.RowIndex >= 0 && e.RowIndex < departures.Count) {
                            SetActive(btnNavOdeme); ShowPayments();
                            if (ShowPaymentDialog(departures[e.RowIndex].Id)) ShowDashboard();
                        }
                    };
                    coPanel.Controls.Add(dgCO);
                }
            } catch { }
            pnlMainContent.Controls.Add(coPanel);
        }

        // ========== MİSAFİRLER ==========
        private void ShowGuests()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("👥 Misafir Yönetimi"));

            int w = pnlMainContent.ClientSize.Width;
            int h = pnlMainContent.ClientSize.Height;

            // --- ÜST PANEL (Arama ve Ana Butonlar) ---
            var pnlTop = new Panel { Location = new Point(20, 55), Size = new Size(w - 40, 50), BackColor = Color.Transparent };
            var txtSearch = new TextBox { 
                Size = new Size(350, 32), Location = new Point(0, 10), 
                Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(25, 40, 75), 
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, 
                PlaceholderText = "🔍 Misafir ara (ad, TC, telefon)..." 
            };
            pnlTop.Controls.Add(txtSearch);

            if (!IsMuhasebe) {
                var btnAdd = MakeBtn("➕ Yeni Misafir", cGreen, 420, 10);
                btnAdd.Size = new Size(130, 32);
                btnAdd.Click += (s, e) => { if (ShowGuestDialog(null)) loadGuests_ref?.Invoke(); };
                pnlTop.Controls.Add(btnAdd);
            }

            if (IsAdmin) {
                var btnDel = MakeBtn("🗑️ Sil", cRed, 560, 10);
                btnDel.Size = new Size(80, 32);
                pnlTop.Controls.Add(btnDel);
            }
            pnlMainContent.Controls.Add(pnlTop);

            // --- ANA İÇERİK (SOL: Liste, SAĞ: Detay) ---
            var split = new SplitContainer {
                Location = new Point(20, 115),
                Size = new Size(w - 40, h - 135),
                SplitterDistance = (int)((w - 40) * 0.65),
                BorderStyle = BorderStyle.None,
                IsSplitterFixed = false
            };
            pnlMainContent.Controls.Add(split);

            // --- SOL PANEL: GRID ---
            var dg = MakeGrid(0, 0);
            dg.Dock = DockStyle.Fill;
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=45},
                new DataGridViewTextBoxColumn{Name="Ad",HeaderText="Ad Soyad", Width=180},
                new DataGridViewTextBoxColumn{Name="TC",HeaderText="TC No",Width=110},
                new DataGridViewTextBoxColumn{Name="Pasaport",HeaderText="Pasaport No",Width=110},
                new DataGridViewTextBoxColumn{Name="Tel",HeaderText="Telefon",Width=110},
                new DataGridViewTextBoxColumn{Name="Email",HeaderText="Email",Width=140},
                new DataGridViewTextBoxColumn{Name="Ulke",HeaderText="Uyruk",Width=80},
            });
            split.Panel1.Controls.Add(dg);

            // --- SAĞ PANEL: DETAY ---
            var pnlDetail = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(22, 30, 50), Padding = new Padding(15) };
            pnlDetail.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 80), 1), 0, 0, pnlDetail.Width - 1, pnlDetail.Height - 1); };
            split.Panel2.Controls.Add(pnlDetail);

            // Detay içeriği temizleme ve güncelleme fonksiyonu
            Action<Guest?> updateDetail = (g) => {
                pnlDetail.Controls.Clear();
                if (g == null) {
                    var lblNone = new Label { Text = "BİR MİSAFİR SEÇİN", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                    pnlDetail.Controls.Add(lblNone);
                    return;
                }

                int dy = 20;
                // Avatar
                var pbAvatar = new PictureBox { Size = new Size(100, 100), Location = new Point((pnlDetail.Width - 100) / 2, dy), SizeMode = PictureBoxSizeMode.StretchImage, BackColor = Color.FromArgb(45, 55, 80) };
                // Placeholder avatar (harf ile)
                var avatarImg = new Bitmap(100, 100);
                using (var gfx = Graphics.FromImage(avatarImg)) {
                    gfx.Clear(Color.FromArgb(60, 70, 100));
                    var initial = g.FullName.Length > 0 ? g.FullName[0].ToString().ToUpper() : "?";
                    gfx.DrawString(initial, new Font("Segoe UI", 40, FontStyle.Bold), Brushes.White, new RectangleF(0, 0, 100, 100), new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
                pbAvatar.Image = avatarImg;
                pnlDetail.Controls.Add(pbAvatar);
                dy += 110;

                var lblName = new Label { Text = g.FullName.ToUpper(), Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, AutoSize = false, Size = new Size(pnlDetail.Width - 30, 30), Location = new Point(15, dy), TextAlign = ContentAlignment.TopCenter };
                pnlDetail.Controls.Add(lblName);
                dy += 45;

                // Bölüm: Kişisel
                var lblH1 = new Label { Text = "Kişisel Bilgiler", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(120, 140, 180), AutoSize = true, Location = new Point(15, dy) };
                pnlDetail.Controls.Add(lblH1); dy += 22;
                
                Action<string, string> addRow = (label, val) => {
                    var l = new Label { Text = label + ":", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9), AutoSize = true, Location = new Point(15, dy) };
                    var v = new Label { Text = string.IsNullOrEmpty(val) ? "-" : val, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold), AutoSize = true, Location = new Point(110, dy) };
                    pnlDetail.Controls.Add(l); pnlDetail.Controls.Add(v);
                    dy += 22;
                };

                addRow("TC", g.TcNo ?? "");
                addRow("Pasaport", g.PassportNo ?? "");
                addRow("Uyruk", g.Nationality ?? "");
                dy += 15;

                // Bölüm: İletişim
                var lblH2 = new Label { Text = "İletişim Bilgileri", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(120, 140, 180), AutoSize = true, Location = new Point(15, dy) };
                pnlDetail.Controls.Add(lblH2); dy += 22;
                addRow("Telefon", g.Phone ?? "");
                addRow("Email", g.Email ?? "");
                dy += 25;

                // İşlem Butonları
                if (!IsMuhasebe) {
                    var btnEdit = MakeBtn("📝 Müşteri Bilgilerini Düzenle", Color.FromArgb(50, 100, 200), 15, dy);
                    btnEdit.Size = new Size(pnlDetail.Width - 30, 38);
                    btnEdit.Click += (s, e) => { if (ShowGuestDialog(g)) loadGuests_ref?.Invoke(); };
                    pnlDetail.Controls.Add(btnEdit);
                    dy += 45;
                }

                if (IsAdmin) {
                    var btnDelQuick = MakeBtn("Kaydı Sil", Color.FromArgb(120, 40, 40), 15, dy);
                    btnDelQuick.Size = new Size(pnlDetail.Width - 30, 32);
                    btnDelQuick.Click += (s, e) => {
                        if (MessageBox.Show($"{g.FullName} kaydını silmek istediğinize emin misiniz?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                            GuestHelper.DeleteGuest(g.Id);
                            loadGuests_ref?.Invoke();
                        }
                    };
                    pnlDetail.Controls.Add(btnDelQuick);
                }
            };

            // Yükleme ve Arama Mantığı
            Action loadGuests = () => {
                dg.Rows.Clear();
                var guests = string.IsNullOrWhiteSpace(txtSearch.Text) ? GuestHelper.GetAllGuests() : GuestHelper.SearchGuests(txtSearch.Text);
                foreach (var g in guests) {
                    dg.Rows.Add(g.Id, g.FullName, g.TcNo ?? "", g.PassportNo ?? "", g.Phone ?? "", g.Email ?? "", g.Nationality);
                }
                updateDetail(null);
            };

            loadGuests_ref = loadGuests;
            loadGuests();

            txtSearch.TextChanged += (s, e) => loadGuests();
            dg.SelectionChanged += (s, e) => {
                if (dg.SelectedRows.Count > 0) {
                    int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    var guest = GuestHelper.GetGuestById(id);
                    updateDetail(guest);
                }
            };

            updateDetail(null);
        }
        private Action? loadGuests_ref;

        private bool ShowGuestDialog(Guest? guest)
        {
            // Mevcut adres parse: "İlçe, Şehir" veya serbest metin
            string existingCity = "";
            string existingDistrict = "";
            string existingFreeAddr = "";
            if (!string.IsNullOrWhiteSpace(guest?.Address))
            {
                var parts = guest.Address.Split(',');
                if (parts.Length >= 2)
                {
                    existingDistrict = parts[0].Trim();
                    existingCity = parts[1].Trim();
                }
                else
                {
                    existingFreeAddr = guest.Address.Trim();
                }
            }

            // ── FORM ──────────────────────────────────────────────────────
            var f = new Form
            {
                Text = guest == null ? "Yeni Misafir" : "Misafir Düzenle",
                Size = new Size(500, 700), MinimumSize = new Size(480, 560),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = cCard, ForeColor = Color.White,
                FormBorderStyle = FormBorderStyle.Sizable, MaximizeBox = false
            };

            // ── SABİT FOOTER (Her zaman görünür kaydet/iptal butonları) ────
            var pnlFoot = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.FromArgb(10, 15, 30) };
            pnlFoot.Paint += (s, e) => { using var pen = new Pen(cGold, 1); e.Graphics.DrawLine(pen, 0, 0, pnlFoot.Width, 0); };
            var btnSave = new Button { Text = "💾  Kaydet", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Size = new Size(160, 40), Location = new Point(15, 10), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 139, 34), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.MouseEnter += (s, e) => btnSave.BackColor = Color.FromArgb(55, 180, 55);
            btnSave.MouseLeave += (s, e) => btnSave.BackColor = Color.FromArgb(34, 139, 34);
            var btnCancel = new Button { Text = "✖  İptal", Font = new Font("Segoe UI", 10F, FontStyle.Bold), Size = new Size(130, 40), Location = new Point(185, 10), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(160, 30, 30), ForeColor = Color.White, Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = Color.FromArgb(210, 55, 55);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.FromArgb(160, 30, 30);
            var lblStatus = new Label { Text = "", AutoSize = false, Location = new Point(330, 16), Size = new Size(145, 22), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.LightGray, BackColor = Color.Transparent, TextAlign = ContentAlignment.MiddleLeft };
            pnlFoot.Controls.AddRange(new Control[] { btnSave, btnCancel, lblStatus });
            f.Controls.Add(pnlFoot);

            // ── SABİT HEADER ──────────────────────────────────────────────
            var pnlHead = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(10, 15, 30) };
            pnlHead.Paint += (s, e) => { using var pen = new Pen(cGold, 2); e.Graphics.DrawLine(pen, 0, pnlHead.Height - 1, pnlHead.Width, pnlHead.Height - 1); };
            pnlHead.Controls.Add(new Label { Text = guest == null ? "👤 Yeni Misafir Ekle" : "👤 Misafir Bilgilerini Düzenle", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = cGold, Location = new Point(14, 12), AutoSize = true, BackColor = Color.Transparent });
            f.Controls.Add(pnlHead);

            // ── KAYDIRILAN İÇERİK PANELİ ─────────────────────────────────
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = cCard, Padding = new Padding(0, 8, 0, 8) };
            f.Controls.Add(scroll);
            f.Controls.SetChildIndex(scroll, 0);

            int y = 12;
            int fieldW = 440;

            void AddSectionHeader(string title)
            {
                scroll.Controls.Add(new Label { Text = title, Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(120, 145, 185), BackColor = Color.Transparent });
                scroll.Controls.Add(new Panel { Location = new Point(14, y + 19), Size = new Size(fieldW, 1), BackColor = Color.FromArgb(38, 52, 80) });
                y += 28;
            }

            TextBox MakeField(string label, string val = "", bool req = false)
            {
                scroll.Controls.Add(new Label { Text = label + (req ? " *" : ""), Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F, req ? FontStyle.Bold : FontStyle.Regular), ForeColor = req ? cGold : Color.FromArgb(175, 192, 218), BackColor = Color.Transparent });
                y += 20;
                var t = new TextBox { Text = val, Location = new Point(14, y), Size = new Size(fieldW, 30), Font = new Font("Segoe UI", 10.5F), BackColor = Color.FromArgb(20, 32, 58), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                scroll.Controls.Add(t); y += 38; return t;
            }

            // Kişisel
            AddSectionHeader("🧑  KİŞİSEL BİLGİLER");
            var tName     = MakeField("Ad Soyad",    guest?.FullName    ?? "", req: true);
            var tTc       = MakeField("TC Kimlik No",guest?.TcNo        ?? "");
            var tPassport = MakeField("Pasaport No",  guest?.PassportNo ?? "");
            var tNat      = MakeField("Uyruk",        guest?.Nationality ?? "Türkiye");

            // İletişim
            AddSectionHeader("📞  İLETİŞİM");
            var tPhone = MakeField("Telefon",        guest?.Phone ?? "");
            var tEmail = MakeField("E-posta Adresi", guest?.Email ?? "");

            // Adres
            AddSectionHeader("📍  ADRES");
            scroll.Controls.Add(new Label { Text = "Şehir", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(175, 192, 218), BackColor = Color.Transparent }); y += 20;
            var cmbCity = new ComboBox { Location = new Point(14, y), Size = new Size(fieldW, 30), Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(20, 32, 58), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            scroll.Controls.Add(cmbCity); y += 38;
            scroll.Controls.Add(new Label { Text = "İlçe", Location = new Point(14, y), AutoSize = true, Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(175, 192, 218), BackColor = Color.Transparent }); y += 20;
            var cmbDistrict = new ComboBox { Location = new Point(14, y), Size = new Size(fieldW, 30), Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(20, 32, 58), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Enabled = false };
            scroll.Controls.Add(cmbDistrict); y += 38;
            var tAddrExtra = MakeField("Adres Detayı (sokak, bina, daire...)", existingFreeAddr);
            y += 5;

            // ── Şehir listesini arka planda yükle ───────────────────────────
            // Şehir yüklenene kadar devre dışı bırak
            List<CityInfo> cities = new();
            cmbCity.Items.Add("⏳ Yükleniyor..."); cmbCity.SelectedIndex = 0; cmbCity.Enabled = false;
            lblStatus.Text = "";

            var bgCity = new System.ComponentModel.BackgroundWorker();
            bgCity.DoWork += (s, e) => { e.Result = LocationHelper.GetProvinces(); };
            bgCity.RunWorkerCompleted += (s, e) =>
            {
                cities = (List<CityInfo>)(e.Result ?? new List<CityInfo>());
                cmbCity.Items.Clear();
                cmbCity.Items.Add("-- Şehir Seçin --");
                foreach (var c in cities) cmbCity.Items.Add(c.Name);
                cmbCity.SelectedIndex = 0; cmbCity.Enabled = true;
                lblStatus.Text = "";

                if (!string.IsNullOrEmpty(existingCity))
                {
                    int idx = cities.FindIndex(c => c.Name.Equals(existingCity, StringComparison.OrdinalIgnoreCase));
                    if (idx >= 0) cmbCity.SelectedIndex = idx + 1;
                }
            };
            bgCity.RunWorkerAsync();

            // ── İlçe yükleme ────────────────────────────────────────────────
            List<DistrictInfo> districts = new();
            cmbCity.SelectedIndexChanged += (s, e) =>
            {
                cmbDistrict.Items.Clear();
                cmbDistrict.Enabled = false;
                int selIdx = cmbCity.SelectedIndex - 1;
                if (selIdx < 0 || selIdx >= cities.Count) return;

                lblStatus.Text = "⏳ İlçeler yükleniyor...";
                cmbDistrict.Items.Add("⏳ Yükleniyor..."); cmbDistrict.SelectedIndex = 0;

                int provinceId = cities[selIdx].Id;
                var bgDist = new System.ComponentModel.BackgroundWorker();
                bgDist.DoWork += (s2, e2) => { e2.Result = LocationHelper.GetDistricts((int)e2.Argument!); };
                bgDist.RunWorkerCompleted += (s2, e2) =>
                {
                    districts = (List<DistrictInfo>)(e2.Result ?? new List<DistrictInfo>());
                    cmbDistrict.Items.Clear();
                    cmbDistrict.Items.Add("-- İlçe Seçin --");
                    foreach (var d in districts) cmbDistrict.Items.Add(d.Name);
                    cmbDistrict.SelectedIndex = 0;
                    cmbDistrict.Enabled = true;
                    lblStatus.Text = "";

                    if (!string.IsNullOrEmpty(existingDistrict))
                    {
                        int didx = districts.FindIndex(d => d.Name.Equals(existingDistrict, StringComparison.OrdinalIgnoreCase));
                        if (didx >= 0) cmbDistrict.SelectedIndex = didx + 1;
                        existingDistrict = "";
                    }
                };
                bgDist.RunWorkerAsync(provinceId);
            };

            // ── Kaydet ──────────────────────────────────────────────────────
            bool saved = false;
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(tName.Text))
                { MessageBox.Show("Ad Soyad zorunludur!", "Eksik Alan", MessageBoxButtons.OK, MessageBoxIcon.Warning); tName.Focus(); return; }

                string tc = tTc.Text.Trim();
                if (!string.IsNullOrWhiteSpace(tc) && !System.Text.RegularExpressions.Regex.IsMatch(tc, @"^\d{11}$"))
                { MessageBox.Show("TC Kimlik Numarası 11 haneli ve sadece rakamlardan oluşmalıdır!", "Geçersiz TC", MessageBoxButtons.OK, MessageBoxIcon.Warning); tTc.Focus(); return; }

                string em = tEmail.Text.Trim();
                if (!string.IsNullOrWhiteSpace(em) && !System.Text.RegularExpressions.Regex.IsMatch(em, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                { MessageBox.Show("Lütfen geçerli bir e-posta adresi girin!", "Geçersiz E-posta", MessageBoxButtons.OK, MessageBoxIcon.Warning); tEmail.Focus(); return; }

                string ph = tPhone.Text.Trim();
                if (!string.IsNullOrWhiteSpace(ph) && ph.Length < 10)
                { MessageBox.Show("Telefon numarası çok kısa, alan kodu ile birlikte giriniz.", "Geçersiz Telefon", MessageBoxButtons.OK, MessageBoxIcon.Warning); tPhone.Focus(); return; }

                string selCity     = cmbCity.SelectedIndex     > 0 ? cmbCity.SelectedItem!.ToString()!     : "";
                string selDistrict = cmbDistrict.SelectedIndex > 0 ? cmbDistrict.SelectedItem!.ToString()! : "";
                string extraAddr   = tAddrExtra.Text.Trim();
                string address = "";
                if (!string.IsNullOrEmpty(selDistrict) && !string.IsNullOrEmpty(selCity))
                    address = $"{selDistrict}, {selCity}";
                else if (!string.IsNullOrEmpty(selCity))
                    address = selCity;
                if (!string.IsNullOrEmpty(extraAddr))
                    address = string.IsNullOrEmpty(address) ? extraAddr : $"{address} | {extraAddr}";

                try
                {
                    lblStatus.Text = "⏳ Kaydediliyor...";
                    var g2         = guest ?? new Guest();
                    g2.FullName    = tName.Text.Trim();
                    g2.TcNo        = tc;
                    g2.PassportNo  = tPassport.Text.Trim();
                    g2.Phone       = ph;
                    g2.Email       = em;
                    g2.Nationality = tNat.Text.Trim();
                    g2.Address     = address;
                    if (guest == null) GuestHelper.AddGuest(g2);
                    else               GuestHelper.UpdateGuest(g2);
                    saved = true;
                    f.Close();
                }
                catch (Exception ex) { MessageBox.Show($"Kayıt hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); lblStatus.Text = "❌ Hata!"; }
            };
            btnCancel.Click += (s, e) => f.Close();
            f.ShowDialog();
            return saved;
        }

        // ========== ODALAR ==========
        private void ShowRooms()
        {
            try { RoomHelper.SyncRoomStatuses(); } catch { }
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("Oda Yönetimi"));

            int topY = 60;
            // ── ÜST BUTONLAR ──
            var btnAdd = MakeBtn("➕ Yeni Oda", Color.FromArgb(76, 175, 80), 20, topY);
            btnAdd.Size = new Size(130, 40);
            if (IsAdmin) { 
                pnlMainContent.Controls.Add(btnAdd);
                btnAdd.Click += (s, e) => { if (ShowRoomDialog(null)) loadRooms_ref?.Invoke(); }; 
            }

            var btnStatus = MakeBtn("🔄 Durum Değiştir", Color.FromArgb(33, 150, 243), IsAdmin ? 160 : 20, topY);
            btnStatus.Size = new Size(160, 40);
            pnlMainContent.Controls.Add(btnStatus);

            var btnUpdatePrice = MakeBtn("💰 Fiyatları Güncelle", Color.FromArgb(41, 128, 185), IsAdmin ? 330 : 190, topY);
            btnUpdatePrice.Size = new Size(180, 40);
            if (IsAdmin) { pnlMainContent.Controls.Add(btnUpdatePrice); }

            var btnDel = MakeBtn("🗑️ Sil", Color.FromArgb(244, 67, 54), IsAdmin ? 520 : 380, topY);
            btnDel.Size = new Size(90, 40);
            if (IsAdmin) { pnlMainContent.Controls.Add(btnDel); }

            // ── DATA GRID ──
            var dg = MakeGrid(120, 500); 
            dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 150);
            dg.BackgroundColor = Color.FromArgb(30, 35, 50); // Darker sleek bg
            dg.BorderStyle = BorderStyle.None;
            dg.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dg.GridColor = Color.FromArgb(45, 55, 75);
            dg.RowTemplate.Height = 45; // Taller rows for modern look

            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=50},
                new DataGridViewTextBoxColumn{Name="No",HeaderText="Room No.",Width=80},
                new DataGridViewTextBoxColumn{Name="Tip",HeaderText="Tip",Width=140},
                new DataGridViewTextBoxColumn{Name="Kat",HeaderText="Floor",Width=60},
                new DataGridViewTextBoxColumn{Name="Kap",HeaderText="Kapasity",Width=80},
                new DataGridViewTextBoxColumn{Name="Fiyat",HeaderText="Gecelik ₺",Width=120},
                new DataGridViewTextBoxColumn{Name="Durum",HeaderText="Durum",Width=100},
            });

            // "Durum" ve "Fiyat" kolonlarının hizalaması
            dg.Columns["Durum"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dg.Columns["Fiyat"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dg.Columns["Fiyat"].DefaultCellStyle.ForeColor = Color.FromArgb(200, 220, 255); // Biraz açık mavi

            Action loadRooms = () => { 
                try { 
                    dg.Rows.Clear(); 
                    var rooms = RoomHelper.GetAllRooms();
                    foreach (var r in rooms) { 
                        string sDisp = r.Status switch { "Available" => "Müsait", "Occupied" => "Dolu", "Reserved" => "Rezerve", "Maintenance" => "Bakımda", _ => "Bilinmiyor" };
                        // Fiyat kolonuna edit kalemi ikonu "✎"
                        int idx = dg.Rows.Add(r.Id, r.RoomNumber, r.RoomTypeName, r.Floor, r.Capacity, $"{r.PricePerNight:N0} ₺   ✎", sDisp);
                        
                        // Durum renkleri (Rounded badge tarzı soft renkler)
                        Color scBg = r.Status switch { "Available" => Color.FromArgb(141, 110, 99), "Occupied" => Color.FromArgb(161, 110, 80), "Reserved" => cYellow, "Maintenance" => cBlue, _ => Color.Gray };
                        dg.Rows[idx].Cells["Durum"].Style.BackColor = scBg; 
                        dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.White;
                        dg.Rows[idx].Cells["Durum"].Style.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        dg.Rows[idx].Cells["Durum"].Style.Padding = new Padding(10, 5, 10, 5); 
                    } 
                } catch (Exception ex) { MessageBox.Show(ex.Message); } 
            };
            loadRooms_ref = loadRooms;
            loadRooms();

            // ── HÜCRE TIKLAMA MANTIĞI (Fiyat Düzenleme) ──
            if (IsAdmin) {
                dg.CellClick += (s, e) => {
                    // Kalem ikonlu fiyat hücresine tıklandığında popup açılır
                    if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name == "Fiyat") {
                        int id = Convert.ToInt32(dg.Rows[e.RowIndex].Cells["Id"].Value);
                        string no = dg.Rows[e.RowIndex].Cells["No"].Value.ToString() ?? "";
                        var rooms = RoomHelper.GetAllRooms(); 
                        var room = rooms.Find(r => r.Id == id); 
                        if (room != null) {
                            var inputForm = new Form { Text = $"Fiyat Güncelle", Size = new Size(300, 180), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox=false };
                            inputForm.Controls.Add(new Label { Text = $"Oda {no} Yeni Fiyat (₺):", Location = new Point(20, 20), AutoSize = true, Font=new Font("Segoe UI", 10F) });
                            var txtPrice = new NumericUpDown { Minimum = 0, Maximum = 1000000, Value = room.PricePerNight, DecimalPlaces = 0, Location = new Point(20, 50), Size = new Size(240, 28), Font = new Font("Segoe UI", 12F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
                            inputForm.Controls.Add(txtPrice);
                            var btnOk = MakeBtn("💾 Kaydet", cGreen, 20, 95);
                            inputForm.Controls.Add(btnOk);
                            btnOk.Click += (s2, e2) => {
                                room.PricePerNight = txtPrice.Value;
                                RoomHelper.UpdateRoom(room); // VERİTABANINA KAYDET
                                inputForm.Close();
                                loadRooms(); // Listeyi yenile
                            };
                            inputForm.ShowDialog();
                        }
                    }
                };

                // "Fiyatları Güncelle" butonu ile toplu güncelleme
                btnUpdatePrice.Click += (s, e) => {
                    var inputForm = new Form { Text = $"Toplu Fiyat Güncelleme", Size = new Size(340, 200), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox=false };
                    inputForm.Controls.Add(new Label { Text = "Tüm odalara Yüzde (%) kaç zam/indirim yapılsın?", Location = new Point(20, 20), AutoSize = true, Font = new Font("Segoe UI", 9F) });
                    var txtPercent = new NumericUpDown { Minimum = -100, Maximum = 500, Value = 10, Location = new Point(20, 50), Size = new Size(280, 28), Font = new Font("Segoe UI", 12F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
                    inputForm.Controls.Add(txtPercent);
                    var btnOk = MakeBtn("🚀 Hepsini Güncelle", Color.FromArgb(41, 128, 185), 20, 95);
                    btnOk.Size = new Size(280, 40);
                    inputForm.Controls.Add(btnOk);
                    btnOk.Click += (s2, e2) => {
                        if (MessageBox.Show($"Tüm oda fiyatları %{txtPercent.Value} değişecek. Emin misiniz?", "Toplu Fiyat Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                            var allRooms = RoomHelper.GetAllRooms();
                            decimal multiplier = 1 + (txtPercent.Value / 100m);
                            foreach(var r in allRooms) {
                                r.PricePerNight = r.PricePerNight * multiplier;
                                RoomHelper.UpdateRoom(r);
                            }
                            inputForm.Close();
                            loadRooms();
                        }
                    };
                    inputForm.ShowDialog();
                };

                // Diğer hücrelere çift tıklayınca standart odayı düzenle açılır
                dg.CellDoubleClick += (s, e) => { 
                    if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name != "Fiyat") { 
                        int id = Convert.ToInt32(dg.Rows[e.RowIndex].Cells["Id"].Value);
                        var rooms = RoomHelper.GetAllRooms(); var room = rooms.Find(r => r.Id == id); 
                        if (room != null && ShowRoomDialog(room)) loadRooms(); 
                    } 
                };
            }

            btnStatus.Click += (s, e) => { 
                if (dg.SelectedRows.Count > 0) { 
                    int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    var sf = new Form { Text = "Durum Değiştir", Size = new Size(300, 200), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog };
                    var cmb = new ComboBox { Location = new Point(20, 30), Size = new Size(240, 30), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
                    var secenekler = new Dictionary<string, string> { {"Müsait", "Available"}, {"Dolu", "Occupied"}, {"Rezerve", "Reserved"}, {"Bakımda", "Maintenance"} };
                    foreach (var kb in secenekler.Keys) cmb.Items.Add(kb);
                    cmb.SelectedIndex = 0;
                    var btnOk = MakeBtn("✅ Uygula", cGreen, 20, 80); sf.Controls.AddRange(new Control[] { cmb, btnOk });
                    btnOk.Click += (s2, e2) => { try { string targetStatus = secenekler[cmb.SelectedItem!.ToString()!]; RoomHelper.UpdateRoomStatus(id, targetStatus); sf.Close(); loadRooms(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
                    sf.ShowDialog(); 
                } 
            };

            if (IsAdmin) { 
                btnDel.Click += (s, e) => { 
                    if (dg.SelectedRows.Count > 0) { 
                        int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                        if (MessageBox.Show("Bu odayı silmek istiyor musunuz?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes) { 
                            try { RoomHelper.DeleteRoom(id); loadRooms(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } 
                        } 
                    } 
                }; 
            }
            
            pnlMainContent.Controls.Add(dg);
        }
        private Action? loadRooms_ref;

        private bool ShowRoomDialog(Room? room)
        {
            var f = new Form { Text = room == null ? "Yeni Oda" : "Oda Düzenle", Size = new Size(420, 450), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 20;
            var types = new List<(int Id, string Name, decimal BasePrice, int Capacity)>();
            try { types = RoomHelper.GetRoomTypes(); } catch { types.Add((1, "Standart", 1500, 2)); }
            f.Controls.Add(new Label { Text = "Oda No *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tNo = new TextBox { Text = room?.RoomNumber ?? "", Location = new Point(20, y), Size = new Size(360, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; f.Controls.Add(tNo); y += 38;
            f.Controls.Add(new Label { Text = "Oda Tipi", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbType = new ComboBox { Location = new Point(20, y), Size = new Size(360, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            foreach (var t in types) cmbType.Items.Add(t.Name); cmbType.SelectedIndex = room != null ? Math.Max(0, types.FindIndex(t => t.Id == room.RoomTypeId)) : 0;
            f.Controls.Add(cmbType); y += 38;
            f.Controls.Add(new Label { Text = "Kat", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tFloor = new NumericUpDown { Value = room?.Floor ?? 1, Minimum = 0, Maximum = 50, Location = new Point(20, y), Size = new Size(360, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White }; f.Controls.Add(tFloor); y += 38;
            f.Controls.Add(new Label { Text = "Kapasite", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tCap = new NumericUpDown { Value = room?.Capacity ?? 2, Minimum = 1, Maximum = 10, Location = new Point(20, y), Size = new Size(360, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White }; f.Controls.Add(tCap); y += 38;
            f.Controls.Add(new Label { Text = "Gecelik Fiyat ₺", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tPrice = new NumericUpDown { Minimum = 0, Maximum = 100000, DecimalPlaces = 0, Location = new Point(20, y), Size = new Size(360, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            tPrice.Value = room?.PricePerNight ?? 1500;
            f.Controls.Add(tPrice); y += 45;
            var btnSave = MakeBtn("💾 Kaydet", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCancel = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCancel);
            bool saved = false;
            btnSave.Click += (s, e) => { if (string.IsNullOrWhiteSpace(tNo.Text)) { MessageBox.Show("Oda No zorunlu!"); return; }
                try { var r = room ?? new Room(); r.RoomNumber = tNo.Text.Trim(); r.RoomTypeId = types[cmbType.SelectedIndex].Id; r.Floor = (int)tFloor.Value; r.Capacity = (int)tCap.Value; r.PricePerNight = tPrice.Value;
                    if (room == null) { r.Status = "Available"; RoomHelper.AddRoom(r); } else RoomHelper.UpdateRoom(r); saved = true; f.Close(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } };
            btnCancel.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        // ========== REZERVASYON ==========
        private void ShowReservations()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📅 Rezervasyon Yönetimi"));
            int w = pnlMainContent.ClientSize.Width - 40;
            int h = pnlMainContent.ClientSize.Height - 65;

            // === SOL PANEL (FORM) ===
            var pnlLeft = new Panel { Location = new Point(20, 65), Size = new Size((int)(w * 0.52), h), BackColor = Color.FromArgb(26, 32, 45) };
            pnlLeft.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 75), 1), 0,0, pnlLeft.Width-1, pnlLeft.Height-1); };
            pnlLeft.AutoScroll = true;
            pnlMainContent.Controls.Add(pnlLeft);

            int y = 20;
            pnlLeft.Controls.Add(new Label { Text = "Yeni Rezervasyon Formu", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, y), AutoSize = true });
            y += 40;

            // Misafir Arama / Seçimi
            pnlLeft.Controls.Add(new Label { Text = "Misafir Ara / Seç", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20, y), AutoSize = true }); y += 22;
            var txtGuestSearch = new TextBox { Location = new Point(20, y), Size = new Size((int)(pnlLeft.Width * 0.65) - 30, 32), Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            pnlLeft.Controls.Add(txtGuestSearch);
            
            var btnNewGuest = MakeBtn("Yeni Misafir Ekle", Color.FromArgb(45, 55, 85), txtGuestSearch.Right + 10, y);
            btnNewGuest.Size = new Size(pnlLeft.Width - txtGuestSearch.Right - 30, 28);
            btnNewGuest.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnNewGuest.Click += (s, e) => { if (ShowGuestDialog(null)) { /* refresh logic if needed */ } };
            pnlLeft.Controls.Add(btnNewGuest);
            y += 45;

            // Dinamik Misafir Listesi (Dropdown gibi)
            var lstGuests = new ListBox { Location = new Point(20, y - 10), Size = txtGuestSearch.Size, Visible = false, BackColor = Color.FromArgb(35, 50, 90), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 10F) };
            pnlLeft.Controls.Add(lstGuests);
            lstGuests.BringToFront();

            txtGuestSearch.TextChanged += (s, e) => {
                string term = txtGuestSearch.Text.Trim().ToLower();
                if (term.Length < 2) { lstGuests.Visible = false; return; }
                var matches = GuestHelper.GetAllGuests().FindAll(g => g.FullName.ToLower().Contains(term));
                if (matches.Count > 0) {
                    lstGuests.Items.Clear();
                    foreach (var m in matches) lstGuests.Items.Add(m.FullName);
                    lstGuests.Visible = true;
                    lstGuests.BringToFront();
                } else { lstGuests.Visible = false; }
            };

            lstGuests.Click += (s, e) => {
                if (lstGuests.SelectedItem != null) {
                    txtGuestSearch.Text = lstGuests.SelectedItem.ToString();
                    lstGuests.Visible = false;
                }
            };

            // Giriş / Çıkış Tarihleri
            int halfW = (pnlLeft.Width - 50) / 2;
            pnlLeft.Controls.Add(new Label { Text = "Giriş Tarihi", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20, y), AutoSize = true });
            pnlLeft.Controls.Add(new Label { Text = "Çıkış Tarihi", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(30 + halfW, y), AutoSize = true });
            y += 22;
            var dtIn = new DateTimePicker { Location = new Point(20, y), Size = new Size(halfW, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var dtOut = new DateTimePicker { Location = new Point(30 + halfW, y), Size = new Size(halfW, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(1) };
            pnlLeft.Controls.Add(dtIn); pnlLeft.Controls.Add(dtOut);
            y += 45;

            // Oda Tipi ve Kişi Sayısı
            pnlLeft.Controls.Add(new Label { Text = "Oda Tipi", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20, y), AutoSize = true });
            pnlLeft.Controls.Add(new Label { Text = "Yetişkin", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20 + halfW, y), AutoSize = true });
            pnlLeft.Controls.Add(new Label { Text = "Çocuk", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20 + halfW + 80, y), AutoSize = true });
            y += 22;
            var cmbType = new ComboBox { Location = new Point(20, y), Size = new Size(halfW - 20, 30), Font=new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White };
            var types = RoomHelper.GetRoomTypes();
            foreach(var t in types) cmbType.Items.Add(t.Name);
            if (cmbType.Items.Count > 0) cmbType.SelectedIndex = 0;
            pnlLeft.Controls.Add(cmbType);

            var nAdult = new NumericUpDown { Location = new Point(20 + halfW, y), Size = new Size(60, 28), Font=new Font("Segoe UI", 10F), Minimum = 1, Value = 1, BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White };
            pnlLeft.Controls.Add(nAdult);
            var nChild = new NumericUpDown { Location = new Point(20 + halfW + 80, y), Size = new Size(60, 28), Font=new Font("Segoe UI", 10F), Minimum = 0, Value = 0, BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White };
            pnlLeft.Controls.Add(nChild);
            y += 45;

            // Oda Seçimi
            var cmbRoom = new ComboBox { Location = new Point(20, y), Size = new Size(pnlLeft.Width - 40, 30), Font=new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            pnlLeft.Controls.Add(cmbRoom);
            y += 45;

            // Logik: Odaları listele
            Action loadAvailRooms = () => {
                cmbRoom.Items.Clear();
                var avail = RoomHelper.GetAvailableRooms(dtIn.Value, dtOut.Value);
                var filtered = avail.FindAll(x => cmbType.SelectedIndex < 0 || x.RoomTypeName == cmbType.Text);
                foreach(var r in filtered) cmbRoom.Items.Add($"Oda {r.RoomNumber} ({r.RoomTypeName})");
                if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
            };
            dtIn.ValueChanged += (s, e) => loadAvailRooms();
            dtOut.ValueChanged += (s, e) => loadAvailRooms();
            cmbType.SelectedIndexChanged += (s, e) => loadAvailRooms();
            loadAvailRooms();

            // Ekstralar
            pnlLeft.Controls.Add(new Label { Text = "Ekstra Hizmetler", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20, y), AutoSize = true }); y+=22;
            var clbExtras = new CheckedListBox { Location = new Point(20, y), Size = new Size(pnlLeft.Width - 40, 100), Font=new Font("Segoe UI", 9F), BackColor = Color.FromArgb(26, 32, 45), ForeColor = Color.FromArgb(180, 190, 210), BorderStyle = BorderStyle.None };
            clbExtras.Items.AddRange(new[] { "Kahvaltı", "Havalimanı Transferi", "Mini Bar Paketi", "SPA Girişi", "Geç Çıkış (Late Check-out)" });
            pnlLeft.Controls.Add(clbExtras);
            y += 110;

            // Fiyat Bilgileri
            int thirdW = (pnlLeft.Width - 60) / 3;
            pnlLeft.Controls.Add(new Label { Text = "Toplam Tutar", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(20, y), AutoSize = true });
            pnlLeft.Controls.Add(new Label { Text = "Depozito", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(30 + thirdW, y), AutoSize = true });
            pnlLeft.Controls.Add(new Label { Text = "Ödeme Yöntemi", Font = new Font("Segoe UI", 9F), ForeColor = cGold, Location = new Point(40 + thirdW * 2, y), AutoSize = true });
            y += 22;

            var tTotal = new TextBox { Location = new Point(20, y), Size = new Size(thirdW, 30), Font = new Font("Segoe UI", 11F, FontStyle.Bold), BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, ReadOnly = true };
            var tDep = new TextBox { Location = new Point(30 + thirdW, y), Size = new Size(thirdW, 30), Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var cmbPay = new ComboBox { Location = new Point(40 + thirdW * 2, y), Size = new Size(thirdW, 30), Font=new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(33, 43, 63), ForeColor = Color.White };
            cmbPay.Items.AddRange(new[] { "Nakit", "Kredi Kartı", "Havale" }); cmbPay.SelectedIndex = 0;
            pnlLeft.Controls.AddRange(new Control[]{tTotal, tDep, cmbPay});
            y += 35;
            
            var lblDemand = new Label { Text = "", Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(129, 199, 132), Location = new Point(20, y), AutoSize = true };
            pnlLeft.Controls.Add(lblDemand);
            y += 30;

            // Butonlar
            var btnCreate = MakeBtn("Rezervasyon Oluştur", Color.FromArgb(184, 150, 70), 20, y);
            btnCreate.Size = new Size(160, 40);
            
            var btnUpdate = MakeBtn("Rezervasyon Düzenle", Color.Transparent, 190, y);
            btnUpdate.Size = new Size(160, 40);
            btnUpdate.FlatAppearance.BorderSize = 1; btnUpdate.FlatAppearance.BorderColor = Color.FromArgb(100, 110, 130);

            var btnCancelRez = MakeBtn("İptal Et", Color.Transparent, 360, y);
            btnCancelRez.Size = new Size(100, 40);
            btnCancelRez.ForeColor = Color.FromArgb(239, 83, 80);
            btnCancelRez.FlatAppearance.BorderSize = 1; btnCancelRez.FlatAppearance.BorderColor = Color.FromArgb(239, 83, 80);

            pnlLeft.Controls.AddRange(new Control[]{btnCreate, btnUpdate, btnCancelRez});

            btnCreate.Click += (s, e) => {
                try {
                    var guestList = GuestHelper.GetAllGuests();
                    var guest = guestList.FirstOrDefault(g => g.FullName.Equals(txtGuestSearch.Text.Trim(), StringComparison.OrdinalIgnoreCase));
                    if (guest == null) { MessageBox.Show("Lütfen geçerli bir misafir seçin veya yeni misafir ekleyin."); return; }
                    if (string.IsNullOrEmpty(cmbRoom.Text)) { MessageBox.Show("Lütfen bir oda seçin."); return; }

                    string roomNo = cmbRoom.Text.Split(' ')[1];
                    var room = RoomHelper.GetAllRooms().FirstOrDefault(r => r.RoomNumber == roomNo);
                    if (room == null) return;

                    var res = new Reservation {
                        GuestId = guest.Id, RoomId = room.Id, CheckInDate = dtIn.Value, CheckOutDate = dtOut.Value,
                        Adults = (int)nAdult.Value, Children = (int)nChild.Value, Status = "Onaylandi",
                        TotalPrice = decimal.Parse(tTotal.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.GetCultureInfo("tr-TR")), CreatedBy = AuthHelper.CurrentUser?.Id, Notes = "Hızlı Rezervasyon"
                    };
                    ReservationHelper.AddReservation(res); RoomHelper.UpdateRoomStatus(room.Id, "Reserved");
                    MessageBox.Show("✅ Rezervasyon başarıyla oluşturuldu."); ShowReservations();
                } catch (InvalidOperationException ex) { MessageBox.Show("⚠️ " + ex.Message, "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
                catch (Exception ex) { MessageBox.Show("Hata: " + ex.Message); }
            };

            // === SAĞ PANEL (DURUM) ===
            int rightX = 20 + pnlLeft.Width + 20;
            int rightW = w - pnlLeft.Width - 20;

            var pnlRightTop = new Panel { Location = new Point(rightX, 65), Size = new Size(rightW, h / 2 - 10), BackColor = Color.FromArgb(26, 32, 45) };
            pnlRightTop.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 75), 1), 0,0, pnlRightTop.Width-1, pnlRightTop.Height-1); };
            pnlMainContent.Controls.Add(pnlRightTop);

            // Müsait Odalar ve Durum
            pnlRightTop.Controls.Add(new Label { Text = "Müsait Odalar ve Durum", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGold, Location = new Point(15, 15), AutoSize = true });
            
            int total = RoomHelper.GetRoomCount();
            int m_avail = RoomHelper.GetRoomCount("Available");
            int m_occ = RoomHelper.GetRoomCount("Occupied");
            
            pnlRightTop.Controls.Add(new Label { Text = $"🛏️ {total} Toplam  |  👤 {m_occ} Dolu  |  🔑 {m_avail} Müsait", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(170, 180, 200), Location = new Point(15, 45), AutoSize = true });

            // Mini Oda Grid (Küçük Kareler)
            var pnlGrid = new Panel { Location = new Point(15, 75), Size = new Size(rightW - 30, pnlRightTop.Height - 90), BackColor = Color.Transparent, AutoScroll = true };
            var all_R = RoomHelper.GetAllRooms();
            int gx = 0, gy = 0;
            foreach(var r in all_R) {
                var btnR = new Button { Text = r.RoomNumber, Size = new Size(50, 45), Location = new Point(gx, gy), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.White, Cursor = Cursors.Hand };
                btnR.FlatAppearance.BorderSize = 0;
                btnR.BackColor = r.Status switch { "Available" => Color.FromArgb(76, 175, 80), "Occupied" => Color.FromArgb(239, 83, 80), "Reserved" => Color.FromArgb(33, 150, 243), "Maintenance" => Color.FromArgb(255, 183, 77), _ => Color.Gray };
                btnR.Click += (s, e) => {
                    if (r.Status == "Occupied") { MessageBox.Show($"🚫 Oda {r.RoomNumber} şu an DOLU!", "Oda Dolu", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    if (r.Status == "Maintenance") { MessageBox.Show($"🔧 Oda {r.RoomNumber} BAKIMDA!", "Bakımda", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    cmbType.Text = r.RoomTypeName; loadAvailRooms();
                    for (int i = 0; i < cmbRoom.Items.Count; i++) {
                        if (cmbRoom.Items[i].ToString()!.Contains($"Oda {r.RoomNumber}")) { cmbRoom.SelectedIndex = i; break; }
                    }
                };
                pnlGrid.Controls.Add(btnR);
                gx += 55; if (gx > pnlGrid.Width - 55) { gx = 0; gy += 50; }
            }
            pnlRightTop.Controls.Add(pnlGrid);

            // Son Rezervasyonlar Listesi (SAĞ ALT PANEL)
            var pnlRightBottom = new Panel { Location = new Point(rightX, 65 + h / 2), Size = new Size(rightW, h / 2), BackColor = Color.FromArgb(26, 32, 45) };
            pnlRightBottom.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(45, 55, 75), 1), 0,0, pnlRightBottom.Width-1, pnlRightBottom.Height-1); };
            pnlMainContent.Controls.Add(pnlRightBottom);

            pnlRightBottom.Controls.Add(new Label { Text = "MİSAFİR LİSTESİ", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = cGold, Location = new Point(15, 15), AutoSize = true });
            
            var dgRecent = MakeGrid(15, 45);
            dgRecent.Size = new Size(rightW - 30, pnlRightBottom.Height - 60);
            dgRecent.BackgroundColor = Color.FromArgb(26, 32, 45);
            dgRecent.RowTemplate.Height = 35;
            dgRecent.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { Name = "Oda", HeaderText = "Oda", Width = 40 },
                new DataGridViewTextBoxColumn { Name = "Misafir", HeaderText = "Misafir Adı" },
                new DataGridViewTextBoxColumn { Name = "Tarih", HeaderText = "Giriş/Çıkış", Width = 90 }
            });
            
            var btnCol = new DataGridViewButtonColumn { 
                Name = "Action", HeaderText = "İşlem", Width = 90, 
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = { ForeColor = Color.White, SelectionForeColor = Color.White }
            };
            dgRecent.Columns.Add(btnCol);

            var recentRes = ReservationHelper.GetAllReservations().OrderByDescending(x => x.Id).Take(20).ToList();
            foreach(var r in recentRes) {
                int idx = dgRecent.Rows.Add(r.RoomNumber, r.GuestName, $"{r.CheckInDate:dd.MM}-{r.CheckOutDate:dd.MM}");
                if (r.Status == "Onaylandi" || r.Status == "Bekliyor") {
                    dgRecent.Rows[idx].Cells["Action"].Value = "Giriş Yap";
                    dgRecent.Rows[idx].Cells["Action"].Style.BackColor = Color.FromArgb(76, 175, 80);
                } else if (r.Status == "GirisYapildi") {
                    dgRecent.Rows[idx].Cells["Action"].Value = "Ödeme/Çıkış";
                    dgRecent.Rows[idx].Cells["Action"].Style.BackColor = Color.FromArgb(41, 128, 185);
                } else {
                    dgRecent.Rows[idx].Cells["Action"].Value = r.Status;
                    dgRecent.Rows[idx].Cells["Action"].Style.BackColor = Color.Gray;
                }
            }

            dgRecent.CellClick += (s, e) => {
                if (e.RowIndex >= 0 && dgRecent.Columns[e.ColumnIndex].Name == "Action") {
                    var r = recentRes[e.RowIndex];
                    string action = dgRecent.Rows[e.RowIndex].Cells["Action"].Value?.ToString() ?? "";
                    
                    if (action == "Giriş Yap") {
                        if (MessageBox.Show($"{r.GuestName} için giriş yapılsın mı?", "Giriş Onayı", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                            try { ReservationHelper.CheckIn(r.Id, r.RoomId); ShowReservations(); } catch (Exception ex) { MessageBox.Show(ex.Message); }
                        }
                    } else if (action == "Ödeme/Çıkış") {
                        SetActive(btnNavOdeme); ShowPayments();
                        if (ShowPaymentDialog(r.Id)) ShowReservations();
                    }
                }
            };
            pnlRightBottom.Controls.Add(dgRecent);

            // Logik: Toplam Tutar Hesaplama (Akıllı Fiyatlandırma)
            Action calcPrice = () => {
                if (cmbType.SelectedIndex >= 0) {
                    var selectedType = types[cmbType.SelectedIndex];
                    int nights = (int)(dtOut.Value.Date - dtIn.Value.Date).TotalDays;
                    if (nights < 1) nights = 1;
                    
                    var pricingInfo = PricingHelper.GetDynamicPricingInfo(dtIn.Value, dtOut.Value);
                    decimal baseTotal = selectedType.BasePrice * nights;
                    decimal totalPrice = PricingHelper.CalculateSmartPrice(baseTotal, pricingInfo.Multiplier);
                    
                    tTotal.Text = totalPrice.ToString("N0");
                    lblDemand.Text = PricingHelper.GetDemandMessage(pricingInfo.Level, pricingInfo.OccupancyRate);
                    lblDemand.ForeColor = pricingInfo.Level == PricingHelper.DemandLevel.Low || pricingInfo.Level == PricingHelper.DemandLevel.Normal ? cGreen : (pricingInfo.Level == PricingHelper.DemandLevel.High ? cYellow : cRed);
                }
            };
            dtIn.ValueChanged += (s, e) => calcPrice();
            dtOut.ValueChanged += (s, e) => calcPrice();
            cmbType.SelectedIndexChanged += (s, e) => calcPrice();
            calcPrice();
        }

        private bool ShowExtraChargeDialog(Reservation res)
        {
            var f = new Form { Text = "Ekstra Hizmet Ekle", Size = new Size(400, 380), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 15;
            
            f.Controls.Add(new Label { Text = $"Misafir: {res.GuestName} (Oda {res.RoomNumber})", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGreen }); y += 40;
            
            f.Controls.Add(new Label { Text = "Hizmet Türü *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbService = new ComboBox { Location = new Point(20, y), Size = new Size(340, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDown, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbService.Items.AddRange(new[] { "Havalimanı Transferi", "Şehir Turu", "Balon Turu", "Spa & Masaj", "Restoran/Oda Servisi", "Mini Bar" }); cmbService.SelectedIndex = 0;
            f.Controls.Add(cmbService); y += 38;

            f.Controls.Add(new Label { Text = "Tutar ₺ *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var nAmt = new NumericUpDown { Minimum = 1, Maximum = 999999, DecimalPlaces = 0, Location = new Point(20, y), Size = new Size(340, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            f.Controls.Add(nAmt); y += 38;

            var btnSave = MakeBtn("💾 Ekstra Ekle", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCnl = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCnl);
            
            bool saved = false;
            btnSave.Click += (s, e) => { 
                if (string.IsNullOrWhiteSpace(cmbService.Text)) { MessageBox.Show("Lütfen hizmet türü seçin veya girin!"); return; }
                try { 
                    ReservationHelper.AddExtraCharge(res.Id, nAmt.Value, cmbService.Text.Trim());
                    MessageBox.Show($"{cmbService.Text} ücreti başarıyla hesaba yansıtıldı!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    saved = true; f.Close(); 
                } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } 
            };
            btnCnl.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        private bool ShowReservationDialog(Reservation? currentRes)
        {
            // ── FORM ──
            var f = new Form
            {
                Text = currentRes == null ? "➕ Yeni Rezervasyon" : "✏️ Rezervasyon Düzenle",
                Size = new Size(640, 700),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(14, 20, 40),
                ForeColor = Color.White,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                Font = new Font("Segoe UI", 10F)
            };

            // ── HEADER BAR ──
            var pnlHead = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.FromArgb(18, 28, 55) };
            pnlHead.Paint += (s, e) => e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(218, 165, 32)), 0, pnlHead.Height - 3, pnlHead.Width, 3);
            pnlHead.Controls.Add(new Label
            {
                Text = currentRes == null ? "🏨  Yeni Rezervasyon Oluştur" : "✏️  Rezervasyon Güncelle",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(218, 165, 32),
                AutoSize = true,
                Location = new Point(16, 14),
                BackColor = Color.Transparent
            });
            f.Controls.Add(pnlHead);

            // ── FIELDS PANEL (sol) ──
            int y = 70; // header'dan sonra
            Label MkLbl(string t, int yy) => new Label { Text = t, Location = new Point(20, yy), AutoSize = true, ForeColor = Color.FromArgb(180, 195, 220), Font = new Font("Segoe UI", 9F) };
            Control MkField(Control c, int yy) { c.Location = new Point(20, yy); return c; }

            // Misafir
            f.Controls.Add(MkLbl("👤  Misafir Seç *", y)); y += 22;
            var cmbGuest = new ComboBox { Size = new Size(590, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            f.Controls.Add(MkField(cmbGuest, y));
            List<Guest> guests = new(); try { guests = GuestHelper.GetAllGuests(); } catch { }
            foreach (var g in guests) cmbGuest.Items.Add($"{g.Id} — {g.FullName}");
            if (cmbGuest.Items.Count > 0) cmbGuest.SelectedIndex = 0;
            if (currentRes != null && guests.Exists(g => g.Id == currentRes.GuestId))
                cmbGuest.SelectedIndex = guests.FindIndex(g => g.Id == currentRes.GuestId);
            y += 40;

            // Tarihler yan yana
            f.Controls.Add(MkLbl("📅  Giriş Tarihi *", y));
            f.Controls.Add(new Label { Text = "📅  Çıkış Tarihi *", Location = new Point(320, y), AutoSize = true, ForeColor = Color.FromArgb(180, 195, 220), Font = new Font("Segoe UI", 9F) });
            y += 22;
            var dtIn  = new DateTimePicker { Size = new Size(280, 30), Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Value = currentRes?.CheckInDate  ?? DateTime.Today, CalendarMonthBackground = Color.FromArgb(22,35,65) };
            var dtOut = new DateTimePicker { Size = new Size(280, 30), Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Value = currentRes?.CheckOutDate ?? DateTime.Today.AddDays(3), CalendarMonthBackground = Color.FromArgb(22,35,65) };
            dtIn.Location  = new Point(20, y);
            dtOut.Location = new Point(320, y);
            f.Controls.AddRange(new Control[] { dtIn, dtOut });
            y += 42;

            // Oda
            f.Controls.Add(MkLbl("🛏️  Oda Seç *", y)); y += 22;
            var cmbRoom = new ComboBox { Size = new Size(590, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            f.Controls.Add(MkField(cmbRoom, y));
            List<Room> availRooms = new();
            Action loadAvail = () =>
            {
                cmbRoom.Items.Clear();
                try { availRooms = RoomHelper.GetAvailableRooms(dtIn.Value, dtOut.Value, currentRes?.Id ?? 0); }
                catch { }
                foreach (var r in availRooms) cmbRoom.Items.Add($"Oda {r.RoomNumber}  —  {r.RoomTypeName}  —  ₺{r.PricePerNight:N0}/gece");
                if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0;
                if (currentRes != null && availRooms.Exists(x => x.Id == currentRes.RoomId))
                    cmbRoom.SelectedIndex = availRooms.FindIndex(x => x.Id == currentRes.RoomId);
            };
            loadAvail();
            y += 42;

            // Kişi sayısı
            f.Controls.Add(MkLbl("👨‍👩‍👧  Yetişkin Sayısı", y));
            f.Controls.Add(new Label { Text = "👶  Çocuk Sayısı", Location = new Point(320, y), AutoSize = true, ForeColor = Color.FromArgb(180, 195, 220), Font = new Font("Segoe UI", 9F) });
            y += 22;
            var nAdult = new NumericUpDown { Value = currentRes?.Adults ?? 1, Minimum = 1, Maximum = 10, Size = new Size(280, 30), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            var nChild = new NumericUpDown { Value = currentRes?.Children ?? 0, Minimum = 0, Maximum = 10, Location = new Point(320, y), Size = new Size(280, 30), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            nAdult.Location = new Point(20, y);
            f.Controls.AddRange(new Control[] { nAdult, nChild });
            y += 42;

            // Notlar
            f.Controls.Add(MkLbl("📝  Notlar", y)); y += 22;
            var tNotes = new TextBox { Text = currentRes?.Notes ?? "", Size = new Size(590, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            f.Controls.Add(MkField(tNotes, y));
            y += 48;

            // ── BİLGİ PANELİ (anlık hesaplama kartı) ──
            var pnlInfo = new Panel
            {
                Location = new Point(16, y),
                Size = new Size(594, 120),
                BackColor = Color.FromArgb(10, 22, 48),
            };
            pnlInfo.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.FillRectangle(new SolidBrush(Color.FromArgb(218, 165, 32)), 0, 0, 4, pnlInfo.Height);
                using var pen = new Pen(Color.FromArgb(40, 218, 165, 32), 1);
                g.DrawRectangle(pen, 0, 0, pnlInfo.Width - 1, pnlInfo.Height - 1);
            };

            // Bilgi satırları
            var lblGiris    = new Label { AutoSize = true, Location = new Point(14, 12), ForeColor = Color.FromArgb(130, 200, 255), Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.Transparent };
            var lblCikis    = new Label { AutoSize = true, Location = new Point(220, 12), ForeColor = Color.FromArgb(255, 160, 100), Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.Transparent };
            var lblGece     = new Label { AutoSize = true, Location = new Point(420, 12), ForeColor = Color.FromArgb(180, 255, 180), Font = new Font("Segoe UI", 9F, FontStyle.Bold), BackColor = Color.Transparent };
            var lblFiyat    = new Label { AutoSize = true, Location = new Point(14, 48), ForeColor = Color.FromArgb(218, 165, 32), Font = new Font("Segoe UI", 12F, FontStyle.Bold), BackColor = Color.Transparent };
            var lblKisi     = new Label { AutoSize = true, Location = new Point(14, 82), ForeColor = Color.FromArgb(200, 200, 200), Font = new Font("Segoe UI", 9F), BackColor = Color.Transparent };
            var lblOdaDetay = new Label { AutoSize = true, Location = new Point(280, 48), ForeColor = Color.FromArgb(160, 190, 220), Font = new Font("Segoe UI", 9F), BackColor = Color.Transparent };
            var lblDemandDialog = new Label { AutoSize = true, Location = new Point(280, 82), ForeColor = Color.FromArgb(129, 199, 132), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), BackColor = Color.Transparent };

            pnlInfo.Controls.AddRange(new Control[] { lblGiris, lblCikis, lblGece, lblFiyat, lblKisi, lblOdaDetay, lblDemandDialog });
            f.Controls.Add(pnlInfo);

            // Anlık güncelleme fonksiyonu
            Action updateInfo = () =>
            {
                var ci = dtIn.Value;
                var co = dtOut.Value;
                int gece = (co - ci).Days;
                if (gece <= 0) { gece = 0; }

                int today = (ci.Date - DateTime.Today).Days;
                string kalanText = today <= 0
                    ? (ci.Date == DateTime.Today ? "Bugün giriş!" : "Giriş tarihi geçmiş")
                    : $"{today} gün kaldı";

                lblGiris.Text    = $"📅 Giriş: {ci:dd MMM yyyy}";
                lblCikis.Text    = $"🚪 Çıkış: {co:dd MMM yyyy}";
                lblGece.Text     = $"🌙 {gece} Gece";

                decimal fiyat = 0;
                string odaInfo = "";
                if (cmbRoom.SelectedIndex >= 0 && cmbRoom.SelectedIndex < availRooms.Count)
                {
                    var r = availRooms[cmbRoom.SelectedIndex];
                    var pricingInfo = PricingHelper.GetDynamicPricingInfo(ci, co);
                    decimal baseNightly = r.PricePerNight;
                    decimal smartNightly = PricingHelper.CalculateSmartPrice(baseNightly, pricingInfo.Multiplier);
                    fiyat = smartNightly * gece;
                    odaInfo = $"Oda {r.RoomNumber} — {r.RoomTypeName}\n₺{smartNightly:N0}/gece × {gece} gece";
                    if (pricingInfo.Multiplier > 1.0m) odaInfo += $" (Baz: ₺{baseNightly:N0})";
                    lblDemandDialog.Text = PricingHelper.GetDemandMessage(pricingInfo.Level, pricingInfo.OccupancyRate);
                    lblDemandDialog.ForeColor = pricingInfo.Level == PricingHelper.DemandLevel.Critical ? Color.FromArgb(229, 115, 115) : (pricingInfo.Level == PricingHelper.DemandLevel.High ? Color.FromArgb(255, 183, 77) : Color.FromArgb(129, 199, 132));
                }
                else { lblDemandDialog.Text = ""; }

                lblFiyat.Text    = fiyat > 0 ? $"💰 Toplam: ₺{fiyat:N0}" : "💰 Toplam: —";
                lblKisi.Text     = $"👤 {(int)nAdult.Value} Yetişkin  +  👶 {(int)nChild.Value} Çocuk     ⏳ {kalanText}";
                lblOdaDetay.Text = odaInfo;
            };

            // Her değişiklikte güncelle
            dtIn.ValueChanged   += (s, e) => { loadAvail(); updateInfo(); };
            dtOut.ValueChanged  += (s, e) => { loadAvail(); updateInfo(); };
            cmbRoom.SelectedIndexChanged += (s, e) => updateInfo();
            nAdult.ValueChanged += (s, e) => updateInfo();
            nChild.ValueChanged += (s, e) => updateInfo();
            updateInfo(); // ilk yükleme
            y += 130;

            // ── BUTONLAR ──
            var btnSave = new Button
            {
                Text = "💾  KAYDET", Size = new Size(180, 42), Location = new Point(20, y),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(34, 120, 60), ForeColor = Color.White,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold), Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            var btnCnl = new Button
            {
                Text = "✕  İptal", Size = new Size(110, 42), Location = new Point(210, y),
                FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(100, 30, 30), ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand
            };
            btnCnl.FlatAppearance.BorderSize = 0;
            f.Controls.AddRange(new Control[] { btnSave, btnCnl });

            // ── KAYDET MANTIĞI ──
            bool saved = false;
            btnSave.Click += (s, e) =>
            {
                if (cmbGuest.SelectedIndex < 0 || cmbRoom.SelectedIndex < 0) { MessageBox.Show("Misafir ve oda seçimi zorunlu!"); return; }
                if (dtOut.Value <= dtIn.Value) { MessageBox.Show("Çıkış tarihi giriş tarihinden sonra olmalı!"); return; }
                
                var selectedRoom = availRooms[cmbRoom.SelectedIndex];
                if (selectedRoom.Status == "Occupied" && dtIn.Value.Date <= DateTime.Today)
                {
                    MessageBox.Show("Bu oda şu an dolu, içeride müşteri var! Lütfen başka bir oda seçiniz.", "Oda Dolu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    var res = new Reservation
                    {
                        GuestId = guests[cmbGuest.SelectedIndex].Id,
                        RoomId = selectedRoom.Id,
                        CheckInDate = dtIn.Value,
                        CheckOutDate = dtOut.Value,
                        Adults = (int)nAdult.Value,
                        Children = (int)nChild.Value,
                        Status = currentRes?.Status ?? "Onaylandi",
                        TotalPrice = PricingHelper.CalculateSmartPrice(availRooms[cmbRoom.SelectedIndex].PricePerNight, PricingHelper.GetDynamicPricingInfo(dtIn.Value, dtOut.Value).Multiplier) * Math.Max(1, (dtOut.Value - dtIn.Value).Days),
                        Notes = tNotes.Text,
                        CreatedBy = AuthHelper.CurrentUser?.Id,
                        Id = currentRes?.Id ?? 0
                    };
                    if (currentRes == null) { ReservationHelper.AddReservation(res); RoomHelper.UpdateRoomStatus(res.RoomId, "Reserved"); }
                    else { ReservationHelper.UpdateReservation(res); if (currentRes.RoomId != res.RoomId) { RoomHelper.UpdateRoomStatus(currentRes.RoomId, "Available"); RoomHelper.UpdateRoomStatus(res.RoomId, "Reserved"); } }
                    saved = true;
                    f.Close();
                }
                catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
            };
            btnCnl.Click += (s, e) => f.Close();
            f.ShowDialog();
            return saved;
        }


        // ========== ÖDEME ==========
        private void ShowPayments()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("💳 Finansal İşlemler & Tahsilat"));
            
            // Üst Panel (Filtreler ve Arama)
            var pnlTop = new Panel { Location = new Point(20, 65), Size = new Size(pnlMainContent.ClientSize.Width - 40, 80), BackColor = cCard };
            pnlMainContent.Controls.Add(pnlTop);

            var btnNew = MakeBtn("➕ Yeni Tahsilat", cGreen, 15, 20);
            btnNew.Size = new Size(160, 40);
            btnNew.Click += (s, e) => { if (ShowPaymentDialog()) ShowPayments(); };
            pnlTop.Controls.Add(btnNew);

            var txtSearch = new TextBox { 
                PlaceholderText = "🔍 Misafir, Oda veya Not ara...", 
                Size = new Size(220, 30), Location = new Point(190, 25), 
                Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(30, 45, 80), 
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle 
            };
            pnlTop.Controls.Add(txtSearch);

            var cmbMethodFilter = new ComboBox { 
                Location = new Point(420, 25), Size = new Size(130, 32), 
                Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, 
                BackColor = Color.FromArgb(30, 45, 80), ForeColor = Color.White 
            };
            cmbMethodFilter.Items.AddRange(new[] { "Tüm Yöntemler", "Nakit", "Kredi Karti", "Havale", "Diger" });
            cmbMethodFilter.SelectedIndex = 0;
            pnlTop.Controls.Add(cmbMethodFilter);

            var dtStart = new DateTimePicker { Location = new Point(pnlTop.Width - 360, 28), Size = new Size(100, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var dtEnd = new DateTimePicker { Location = new Point(pnlTop.Width - 250, 28), Size = new Size(100, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var btnFilter = MakeBtn("Filtrele", Color.FromArgb(45, 55, 85), pnlTop.Width - 140, 20);
            btnFilter.Size = new Size(120, 40);
            pnlTop.Controls.AddRange(new Control[] { dtStart, dtEnd, btnFilter });

            // Ana Grid
            var dg = MakeGrid(160, 500); 
            dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 180);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=50},
                new DataGridViewTextBoxColumn{Name="Rez",HeaderText="Rez.No",Width=70},
                new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir"},
                new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=70},
                new DataGridViewTextBoxColumn{Name="Tutar",HeaderText="Tutar",Width=100},
                new DataGridViewTextBoxColumn{Name="Yontem",HeaderText="Yöntem",Width=110},
                new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Tarih",Width=140},
                new DataGridViewTextBoxColumn{Name="Not",HeaderText="Notlar"},
            });

            Action loadPay = () => { 
                try { 
                    dg.Rows.Clear(); 
                    var list = PaymentHelper.GetAllPayments();
                    DateTime sDate = dtStart.Value.Date;
                    DateTime eDate = dtEnd.Value.Date.AddDays(1).AddTicks(-1);
                    list = list.FindAll(p => p.PaymentDate >= sDate && p.PaymentDate <= eDate);
                    string sTerm = txtSearch.Text.Trim().ToLower();
                    if (!string.IsNullOrEmpty(sTerm))
                        list = list.FindAll(p => p.GuestName.ToLower().Contains(sTerm) || p.RoomNumber.ToLower().Contains(sTerm) || (p.Notes != null && p.Notes.ToLower().Contains(sTerm)));
                    if (cmbMethodFilter.SelectedIndex > 0)
                        list = list.FindAll(p => p.PaymentMethod == cmbMethodFilter.SelectedItem.ToString());

                    foreach (var p in list) 
                        dg.Rows.Add(p.Id, p.ReservationId, p.GuestName, p.RoomNumber, $"₺{p.Amount:N0}", p.PaymentMethodDisplay, p.PaymentDate.ToString("dd.MM.yyyy HH:mm"), p.Notes ?? ""); 
                } catch { } 
            };

            loadPay();
            btnFilter.Click += (s, e) => loadPay();
            txtSearch.TextChanged += (s, e) => loadPay();
            cmbMethodFilter.SelectedIndexChanged += (s, e) => loadPay();
            pnlMainContent.Controls.Add(dg);
        }

        private bool ShowPaymentDialog(int preSelectedResId = 0)
        {
            // Ekran boyutuna göre dinamik pencere boyutu
            var screen = Screen.FromPoint(Cursor.Position);
            int frmH = Math.Min((int)(screen.WorkingArea.Height * 0.92), 980);
            var f = new Form {
                Text = "Ödeme ve Tahsilat İşlemi",
                Size = new Size(660, frmH),
                MinimumSize = new Size(620, 650),
                StartPosition = FormStartPosition.CenterScreen,
                BackColor = Color.FromArgb(18, 22, 38),
                ForeColor = Color.White,
                FormBorderStyle = FormBorderStyle.Sizable,
                MaximizeBox = true,
                Font = new Font("Segoe UI", 10F)
            };

            // ── SCROLLABLE CONTAINER (önce eklenmeli - WinForms dock sırası) ──
            var pnlScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(18, 22, 38), Padding = new Padding(0, 15, 0, 20) };
            f.Controls.Add(pnlScroll);  // Fill önce

            // ── HEADER (sonra eklenmeli - Top panel Fill'in üstüne gelir) ──
            var pnlHead = new Panel { Dock = DockStyle.Top, Height = 75, BackColor = Color.FromArgb(24, 30, 50) };
            pnlHead.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(cGold), 0, pnlHead.Height - 4, pnlHead.Width, 4);
                e.Graphics.DrawString("AFM", new Font("Arial", 14, FontStyle.Bold), new SolidBrush(cGold), pnlHead.Width - 110, 15);
                e.Graphics.DrawString("GRAND RMS", new Font("Arial", 8, FontStyle.Bold), new SolidBrush(Color.White), pnlHead.Width - 110, 40);
            };
            var lblTitle = new Label { Text = "💳 Yeni Tahsilat İşlemi", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(20, 18), BackColor = Color.Transparent };
            pnlHead.Controls.Add(lblTitle);
            f.Controls.Add(pnlHead);   // Top sonra

            // Form açıldığında scroll'u en üste al
            f.Shown += (s, e) => pnlScroll.AutoScrollPosition = new Point(0, 0);

            int y = 20;
            Label MkLbl(string t, int yy) => new Label { Text = t, Location = new Point(30, yy), AutoSize = true, ForeColor = Color.FromArgb(160, 180, 210), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };

            // 1. Rezervasyon Seçim & Filtreleme (Dinamik Arama)
            pnlScroll.Controls.Add(MkLbl("🔍 Misafir Adı ile Ara", y)); y += 26;
            var txtGuestSearch = new TextBox { Location = new Point(30, y), Size = new Size(545, 36), Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Misafir adı giriniz..." };
            pnlScroll.Controls.Add(txtGuestSearch); y += 45;

            pnlScroll.Controls.Add(new Label { Text = "🏢 Kat Seçimi", Location = new Point(30, y), AutoSize = true, ForeColor = Color.FromArgb(160, 180, 210), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });
            pnlScroll.Controls.Add(new Label { Text = "🛏️ Oda Seçimi", Location = new Point(310, y), AutoSize = true, ForeColor = Color.FromArgb(160, 180, 210), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });
            y += 26;

            var cmbFloorSelect = new ComboBox { Location = new Point(30, y), Size = new Size(260, 36), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbFloorSelect.Items.Add("Tüm Katlar");
            cmbFloorSelect.Items.AddRange(new[] { "1. Kat", "2. Kat", "3. Kat", "4. Kat" });
            cmbFloorSelect.SelectedIndex = 0;
            pnlScroll.Controls.Add(cmbFloorSelect);

            var cmbRoomSelect = new ComboBox { Location = new Point(310, y), Size = new Size(265, 36), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            pnlScroll.Controls.Add(cmbRoomSelect);
            y += 48;

            pnlScroll.Controls.Add(MkLbl("📋 Eşleşen Rezervasyon Seç *", y)); y += 26;
            var cmbRes = new ComboBox { Location = new Point(30, y), Size = new Size(545, 40), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            pnlScroll.Controls.Add(cmbRes);
            y += 60;

            // 2. KONAKLAMA BİLGİLERİ (YENİ ŞIK PANEL)
            var pnlAcc = new Panel { Location = new Point(30, y), Size = new Size(545, 80), BackColor = Color.FromArgb(26, 36, 60) };
            pnlAcc.Paint += (s, e) => {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(100, 140, 255)), 0, 0, 5, pnlAcc.Height);
            };
            var lblRoomInfo = new Label { Text = "🛏️ Oda: -", Location = new Point(15, 12), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White };
            var lblDates = new Label { Text = "📅 Giriş: -   🚪 Çıkış: -", Location = new Point(15, 45), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(180, 200, 230) };
            var lblOverstay = new Label { Text = "", Location = new Point(280, 12), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.FromArgb(255, 100, 100) };
            pnlAcc.Controls.AddRange(new Control[] { lblRoomInfo, lblDates, lblOverstay });
            pnlScroll.Controls.Add(pnlAcc);
            y += 100;

            // 3. BAKİYE KARTI
            var pnlBalance = new Panel { Location = new Point(30, y), Size = new Size(545, 165), BackColor = Color.FromArgb(28, 38, 65) };
            pnlBalance.Paint += (s, e) => {
                using var pen = new Pen(Color.FromArgb(80, 218, 165, 32), 2);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlBalance.Width - 1, pnlBalance.Height - 1);
            };
            var lblTotalCost = new Label { Text = "Orijinal Rezervasyon: ₺0", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(200, 210, 230) };
            var lblExtraDaysPrice = new Label { Text = "Ekstra Gün Ücreti: ₺0", Location = new Point(20, 45), AutoSize = true, Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(250, 160, 90) };
            var lblTotalPaid = new Label { Text = "Daha Önce Ödenen: ₺0", Location = new Point(20, 75), AutoSize = true, Font = new Font("Segoe UI", 11F), ForeColor = Color.FromArgb(200, 210, 230) };
            var lblRemaining = new Label { Text = "KALAN BORÇ: ₺0", Location = new Point(20, 115), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.FromArgb(248, 113, 113) };
            var lblExtras = new Label { Text = "", Location = new Point(280, 15), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.FromArgb(218, 165, 32) };
            pnlBalance.Controls.AddRange(new Control[] { lblTotalCost, lblExtraDaysPrice, lblTotalPaid, lblRemaining, lblExtras });
            var pnlBag = new Panel { Location = new Point(445, 42), Size = new Size(80, 80), BackColor = Color.FromArgb(60, 218, 165, 32) };
            pnlBag.Controls.Add(new Label { Text = "💰", Font = new Font("Segoe UI", 30F), Location = new Point(12, 12), AutoSize = true, ForeColor = Color.White });
            pnlBalance.Controls.Add(pnlBag);
            pnlScroll.Controls.Add(pnlBalance);
            y += 190;

            // 3.5 RESTORAN HARCAMALARI PANELİ (YENİ)
            var pnlRestaurant = new Panel { Location = new Point(30, y), Size = new Size(545, 150), BackColor = Color.FromArgb(28, 38, 65), AutoScroll = true };
            pnlRestaurant.Paint += (s, e) => {
                using var pen = new Pen(Color.FromArgb(218, 100, 32), 2);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlRestaurant.Width - 1, pnlRestaurant.Height - 1);
            };
            var lblRestaurantTitle = new Label { Text = "🍽️ Restoran Harcamaları", Location = new Point(20, 8), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(218, 165, 32) };
            pnlRestaurant.Controls.Add(lblRestaurantTitle);
            
            // Restoran siparişlerini listeleyecek container
            var pnlRestaurantItems = new Panel { Location = new Point(5, 35), Size = new Size(535, 110), BackColor = Color.Transparent, AutoScroll = true };
            pnlRestaurant.Controls.Add(pnlRestaurantItems);
            
            pnlScroll.Controls.Add(pnlRestaurant);
            y += 160;

            // 4. Tutar ve Döviz
            var pnlMoney = new Panel { Location = new Point(30, y), Size = new Size(545, 100), BackColor = Color.Transparent };
            pnlMoney.Controls.Add(new Label { Text = "💱 Para Birimi", Location = new Point(0, 0), AutoSize = true, ForeColor = Color.FromArgb(160, 180, 210), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });
            pnlMoney.Controls.Add(new Label { Text = "💰 Ödeme Tutarı *", Location = new Point(220, 0), AutoSize = true, ForeColor = Color.FromArgb(160, 180, 210), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) });
            var cmbCurr = new ComboBox { Location = new Point(0, 30), Size = new Size(200, 40), Font = new Font("Segoe UI", 12F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbCurr.Items.AddRange(new[] { "TRY 🇹🇷", "USD 🇺🇸", "EUR 🇪🇺", "GBP 🇬🇧" }); cmbCurr.SelectedIndex = 0;
            var nAmt = new NumericUpDown { Minimum = 0, Maximum = 999999, DecimalPlaces = 2, Location = new Point(220, 30), Size = new Size(325, 40), Font = new Font("Segoe UI", 14F, FontStyle.Bold), BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.FromArgb(74, 222, 128), BorderStyle = BorderStyle.FixedSingle };
            var lblRateInfo = new Label { Text = "Borsa Kuru: 1.00 TRY", Location = new Point(0, 75), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.Gray };
            var lblTryEquivalent = new Label { Text = "TAHSİLAT DEĞERİ: ₺0", Location = new Point(220, 75), AutoSize = true, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGold };
            pnlMoney.Controls.AddRange(new Control[] { cmbCurr, nAmt, lblRateInfo, lblTryEquivalent });
            pnlScroll.Controls.Add(pnlMoney);
            y += 115;

            // 5. Ödeme Yöntemi
            pnlScroll.Controls.Add(MkLbl("💳 Ödeme Yöntemi", y)); y += 28;
            var cmbMethod = new ComboBox { Location = new Point(30, y), Size = new Size(545, 40), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            cmbMethod.Items.AddRange(new[] { "💵 Nakit", "💳 Kredi Kartı", "🏦 Havale/EFT", "🧾 Diğer" }); cmbMethod.SelectedIndex = 0;
            pnlScroll.Controls.Add(cmbMethod);
            y += 65;

            // 6. Notlar
            pnlScroll.Controls.Add(MkLbl("📝 Notlar", y)); y += 28;
            var tNotes = new TextBox { Location = new Point(30, y), Size = new Size(545, 40), Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(32, 44, 70), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Ek Notlar Girin..." };
            pnlScroll.Controls.Add(tNotes);
            y += 75;

            // 7. İşlem Türü
            pnlScroll.Controls.Add(new Label { Text = "İŞLEM TÜRÜ", Location = new Point(30, y), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White }); y += 30;
            var rbPartial = new RadioButton { Text = "Ara Ödeme (Kısmi Ödeme, Misafir Kalmaya Devam Eder)", Location = new Point(35, y), AutoSize = true, ForeColor = Color.FromArgb(200, 210, 230), Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand }; y += 35;
            var rbCheckOut = new RadioButton { Text = "Tam Ödeme & Çıkış (Kalan Borç Kapatılır ve Oda Boşaltılır)", Location = new Point(35, y), AutoSize = true, ForeColor = Color.FromArgb(218, 165, 32), Font = new Font("Segoe UI", 10F, FontStyle.Bold), Checked = true, Cursor = Cursors.Hand };
            pnlScroll.Controls.AddRange(new Control[] { rbPartial, rbCheckOut });
            y += 70;

            // Butonlar
            var btnPay = MakeBtn("✓ TAHSİLAT YAP", cGreen, 30, y); btnPay.Size = new Size(265, 55); btnPay.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            var btnCnl = MakeBtn("✕ İptal", Color.FromArgb(180, 50, 50), 310, y); btnCnl.Size = new Size(265, 55); btnCnl.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            pnlScroll.Controls.AddRange(new Control[] { btnPay, btnCnl });
            y += 100; // Panel için biraz boşluk

            decimal currentRate = 1m;
            decimal kalanDebtTry = 0;

            Action updateCalc = () => {
                string currRaw = cmbCurr.SelectedItem?.ToString() ?? "TRY";
                string curr = currRaw.Length >= 3 ? currRaw.Substring(0, 3) : "TRY";
                currentRate = ORYS.Helpers.ExchangeRateHelper.GetRate(curr);
                lblRateInfo.Text = $"Borsa Kuru: {currentRate:N2} {curr}";
                decimal tryAmt = nAmt.Value * currentRate;
                lblTryEquivalent.Text = $"TAHSİLAT DEĞERİ: ₺{tryAmt:N0}";
            };

            List<Reservation> resList = new(); 
            try { 
                resList = ReservationHelper.GetActiveReservations().GroupBy(x => x.Id).Select(g => g.First()).ToList(); 
            } catch { }

            var allRooms = new List<Room>();
            try { allRooms = RoomHelper.GetAllRooms(); } catch { }

            // Oda seçimi combobox'ını doldurma
            Action populateRoomsFilter = () => {
                cmbRoomSelect.Items.Clear();
                cmbRoomSelect.Items.Add("Tüm Odalar");
                
                var activeRoomNumbers = resList.Select(r => r.RoomNumber).Distinct().OrderBy(n => n).ToList();
                foreach(var num in activeRoomNumbers) {
                    cmbRoomSelect.Items.Add($"Oda {num}");
                }
                cmbRoomSelect.SelectedIndex = 0;
            };
            populateRoomsFilter();

            List<Reservation> filteredResList = new();
            List<RestaurantOrder> currentRestaurantOrders = new(); // Seçilen odanın restoran siparişleri
            
            Action loadReservationDetails = () => {
                if (cmbRes.SelectedIndex >= 0 && cmbRes.SelectedIndex < filteredResList.Count) {
                    var r = filteredResList[cmbRes.SelectedIndex];
                    
                    // -- YENİ: Konaklama Bilgileri Güncellemesi --
                    lblRoomInfo.Text = $"🛏️ Oda: {r.RoomNumber}";
                    lblDates.Text = $"📅 Giriş: {r.CheckInDate:dd.MM.yyyy}   🚪 Çıkış: {r.CheckOutDate:dd.MM.yyyy}";
                    
                    int overstayDays = (DateTime.Today - r.CheckOutDate.Date).Days;
                    decimal extraPrice = 0;
                    if (overstayDays > 0) {
                        lblOverstay.Text = $"⚠️ DİKKAT: {overstayDays} gün ekstra!";
                        lblOverstay.ForeColor = Color.FromArgb(255, 100, 100);

                        // Ekstra gün ücreti otomatik hesabı
                        var room = RoomHelper.GetAllRooms().Find(rm => rm.Id == r.RoomId);
                        if (room != null) {
                            var pricing = PricingHelper.GetDynamicPricingInfo(r.CheckOutDate, DateTime.Today);
                            decimal nightlyPrice = PricingHelper.CalculateSmartPrice(room.PricePerNight, pricing.Multiplier);
                            extraPrice = nightlyPrice * overstayDays;
                        }
                    } else if (overstayDays == 0) {
                        lblOverstay.Text = "ℹ️ Çıkış Günü (Bugün)";
                        lblOverstay.ForeColor = Color.FromArgb(255, 180, 50);
                    } else {
                        lblOverstay.Text = $"ℹ️ Çıkışına {-overstayDays} gün var";
                        lblOverstay.ForeColor = Color.FromArgb(100, 200, 100);
                    }

                    // -- YENİ: Restoran Harcamaları Yükleme --
                    currentRestaurantOrders = new();
                    decimal totalRestaurantDebt = 0;
                    pnlRestaurantItems.Controls.Clear();
                    
                    try {
                        currentRestaurantOrders = RestaurantHelper.GetOrdersByRoom(r.RoomId);
                    } catch { /* Veritabanı hatası - sessizce devam et */ }
                    
                    int restY = 5;
                    if (currentRestaurantOrders.Count > 0) {
                        foreach (var order in currentRestaurantOrders) {
                            var isPaid = order.Status == "Tamamlandi";
                            
                            // Her sipariş için bir panel oluştur
                            var pnlOrder = new Panel { Location = new Point(0, restY), Size = new Size(530, 50), BackColor = Color.FromArgb(35, 45, 70), BorderStyle = BorderStyle.FixedSingle };
                            
                            // Sipariş tarihi ve ürünler
                            var itemsText = string.Join(", ", order.Items.Select(it => $"{it.ProductName}(x{it.Quantity})"));
                            var lblOrderInfo = new Label { Text = $"📌 {order.CreatedAt:HH:mm} - {itemsText}", Location = new Point(10, 5), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(200, 210, 230), MaximumSize = new Size(350, 40) };
                            pnlOrder.Controls.Add(lblOrderInfo);
                            
                            // Tutar
                            var lblAmount = new Label { Text = $"₺{order.TotalAmount:N0}", Location = new Point(400, 15), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White };
                            pnlOrder.Controls.Add(lblAmount);
                            
                            // Durum etiketi
                            var lblStatus = new Label { 
                                Text = isPaid ? "ÖDENDİ ✓" : "ÖDENMEDİ",
                                Location = new Point(460, 15), AutoSize = true, Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                                ForeColor = isPaid ? Color.FromArgb(100, 200, 100) : Color.FromArgb(255, 100, 100),
                                BackColor = isPaid ? Color.FromArgb(20, 40, 20) : Color.FromArgb(60, 20, 20),
                                Padding = new Padding(5, 3, 5, 3)
                            };
                            pnlOrder.Controls.Add(lblStatus);
                            
                            pnlRestaurantItems.Controls.Add(pnlOrder);
                            restY += 55;
                            
                            // Ödenmemiş siparişleri borca ekle
                            if (!isPaid) {
                                totalRestaurantDebt += order.TotalAmount;
                            }
                        }
                    } else {
                        var lblNoOrders = new Label { Text = "Restoran siparişi yok.", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = Color.Gray };
                        pnlRestaurantItems.Controls.Add(lblNoOrders);
                    }

                    // -- Finansal Durum Güncellemesi --
                    decimal odenilen = PaymentHelper.GetTotalPaidForReservation(r.Id);
                    
                    // Toplam tutara ekstra gün fiyatı ve restoran harcamaları otomatik olarak ekleniyor
                    decimal guncelToplam = r.TotalPrice + extraPrice + totalRestaurantDebt;
                    kalanDebtTry = guncelToplam - odenilen;

                    lblTotalCost.Text = $"Orijinal Rezervasyon: ₺{r.TotalPrice:N0}";
                    lblExtraDaysPrice.Text = (extraPrice + totalRestaurantDebt) > 0 
                        ? $"Ekstra Gün + Restoran: ₺{(extraPrice + totalRestaurantDebt):N0}"
                        : "Ekstra Gün + Restoran: ₺0";
                    lblTotalPaid.Text = $"Daha Önce Ödenen: ₺{odenilen:N0}";
                    lblRemaining.Text = $"GÜNCEL BORÇ: ₺{kalanDebtTry:N0}";

                    string extras = "";
                    if (r.IncludeBreakfast) extras += $"🍳 Kahvaltı (₺{r.BreakfastPrice:N0}) ";
                    if (r.IncludeDinner) extras += $"🍽 Akşam Yemeği (₺{r.DinnerPrice:N0})";
                    lblExtras.Text = string.IsNullOrEmpty(extras) ? "" : "➕ Ekstralar:\n" + extras;
                    
                    if (rbCheckOut.Checked) {
                        decimal foreignAmount = currentRate > 0 ? (kalanDebtTry / currentRate) : kalanDebtTry;
                        nAmt.Value = foreignAmount <= nAmt.Maximum ? Math.Round(foreignAmount, 2) : nAmt.Maximum;
                    }
                }
            };

            Action filterReservations = () => {
                string nameQuery = txtGuestSearch.Text.Trim().ToLower();
                
                int targetFloor = 0;
                if (cmbFloorSelect.SelectedIndex > 0) {
                    targetFloor = Convert.ToInt32(cmbFloorSelect.SelectedItem.ToString().Split('.')[0]);
                }
                
                string targetRoomNum = "";
                if (cmbRoomSelect.SelectedIndex > 0) {
                    targetRoomNum = cmbRoomSelect.SelectedItem.ToString().Replace("Oda ", "").Trim();
                }

                filteredResList = resList.FindAll(r => {
                    // İsim filtresi
                    if (!string.IsNullOrEmpty(nameQuery) && !r.GuestName.ToLower().Contains(nameQuery))
                        return false;
                    
                    // Kat filtresi
                    if (targetFloor > 0) {
                        var room = allRooms.Find(rm => rm.Id == r.RoomId);
                        if (room == null || room.Floor != targetFloor)
                            return false;
                    }
                    
                    // Oda filtresi
                    if (!string.IsNullOrEmpty(targetRoomNum) && r.RoomNumber != targetRoomNum)
                        return false;
                    
                    return true;
                });

                // Eşleşenleri cmbRes dropdown'ına ekleme
                cmbRes.Items.Clear();
                foreach(var r in filteredResList) {
                    cmbRes.Items.Add(r.DropdownDisplay);
                }

                if (cmbRes.Items.Count > 0) {
                    cmbRes.SelectedIndex = 0;
                    loadReservationDetails();
                } else {
                    lblRoomInfo.Text = "🛏️ Oda: -";
                    lblDates.Text = "📅 Giriş: -   🚪 Çıkış: -";
                    lblOverstay.Text = "";
                    lblTotalCost.Text = "Orijinal Rezervasyon: ₺0";
                    lblExtraDaysPrice.Text = "Ekstra Gün Ücreti: ₺0";
                    lblTotalPaid.Text = "Daha Önce Ödenen: ₺0";
                    lblRemaining.Text = "GÜNCEL BORÇ: ₺0";
                    lblExtras.Text = "";
                    nAmt.Value = 0;
                }
            };

            txtGuestSearch.TextChanged += (s, e) => filterReservations();
            cmbFloorSelect.SelectedIndexChanged += (s, e) => filterReservations();
            cmbRoomSelect.SelectedIndexChanged += (s, e) => filterReservations();

            cmbRes.SelectedIndexChanged += (s, e) => loadReservationDetails();
            cmbCurr.SelectedIndexChanged += (s, e) => { updateCalc(); loadReservationDetails(); };
            nAmt.ValueChanged += (s, e) => updateCalc();
            rbPartial.CheckedChanged += (s, e) => loadReservationDetails();
            rbCheckOut.CheckedChanged += (s, e) => loadReservationDetails();

            if (preSelectedResId > 0) {
                int idx = resList.FindIndex(x => x.Id == preSelectedResId);
                if (idx >= 0) {
                    var r = resList[idx];
                    txtGuestSearch.Text = r.GuestName;
                }
            } else {
                filterReservations();
            }

            bool result = false;
            btnPay.Click += (s, e) => {
                if (cmbRes.SelectedIndex < 0 || cmbRes.SelectedIndex >= filteredResList.Count) return;
                var r = filteredResList[cmbRes.SelectedIndex];
                decimal amtTry = nAmt.Value * currentRate;
                if (amtTry <= 0) { MessageBox.Show("Lütfen geçerli bir tutar girin."); return; }
                try {
                    // Ekstra gün kalma durumunda veritabanında rezervasyon güncelleniyor
                    int overstayDays = (DateTime.Today - r.CheckOutDate.Date).Days;
                    if (overstayDays > 0) {
                        var room = RoomHelper.GetAllRooms().Find(rm => rm.Id == r.RoomId);
                        if (room != null) {
                            var pricing = PricingHelper.GetDynamicPricingInfo(r.CheckOutDate, DateTime.Today);
                            decimal nightlyPrice = PricingHelper.CalculateSmartPrice(room.PricePerNight, pricing.Multiplier);
                            decimal extraPrice = nightlyPrice * overstayDays;
                            
                            string extNote = $"[{DateTime.Now:dd.MM HH:mm}] EKSTRA KONAKLAMA: {overstayDays} gün uzatıldı (+{extraPrice:N0}₺)";
                            ReservationHelper.ExtendReservationAndAddExtraCharge(r.Id, DateTime.Today, extraPrice, extNote);
                        }
                    }

                    // YENİ: Ödenmemiş restoran siparişlerini "Tamamlandı" olarak işaretle
                    foreach (var order in currentRestaurantOrders) {
                        if (order.Status == "OdayaYazildi") {
                            RestaurantHelper.UpdateOrderStatus(order.Id, "Tamamlandi");
                        }
                    }

                    PaymentHelper.AddPayment(new Payment {
                        ReservationId = r.Id, Amount = amtTry,
                        PaymentMethod = cmbMethod.SelectedIndex switch {
                            0 => "Nakit",
                            1 => "Kredi Karti",
                            2 => "Havale",
                            _ => "Diger"
                        },
                        PaymentDate = DateTime.Now, Notes = tNotes.Text, CreatedBy = AuthHelper.CurrentUser?.Id
                    });
                    if (rbCheckOut.Checked) { ReservationHelper.CheckOut(r.Id, r.RoomId); MessageBox.Show("✅ Tahsilat ve Check-out işlemi başarıyla tamamlandı."); }
                    else { MessageBox.Show("✅ Tahsilat başarıyla kaydedildi."); }
                    result = true; f.Close();
                } catch (Exception ex) { MessageBox.Show(ex.Message); }
            };
            btnCnl.Click += (s, e) => f.Close();

            f.ShowDialog();
            return result;
        }


        // ========== RAPORLAR ==========
        private void ShowReports()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📊 SOM-PMS RAPORLAMA VE ANALİZ MODÜLÜ - YÖNETİM DASHBOARD"));
            int w = pnlMainContent.ClientSize.Width - 40;
            int y = 65;

            // === 1. TARİH FİLTRE PANELİ ===
            var pnlFilter = new Panel { Location = new Point(20, y), Size = new Size(w, 85), BackColor = cCard };
            pnlFilter.Paint += (s, e) => { 
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(50, 218, 165, 32), 1);
                g.DrawRectangle(pen, 0, 0, pnlFilter.Width - 1, pnlFilter.Height - 1);
            };

            pnlFilter.Controls.Add(new Label { Text = "TARİH FİLTRE", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = cGold, Location = new Point(15, 10), AutoSize = true });
            
            var dtpStart = new DateTimePicker { Location = new Point(15, 40), Size = new Size(130, 28), Format = DateTimePickerFormat.Short, Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            var lblTo = new Label { Text = "→", Location = new Point(150, 43), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 12F) };
            var dtpEnd = new DateTimePicker { Location = new Point(180, 40), Size = new Size(130, 28), Format = DateTimePickerFormat.Short, Value = DateTime.Today, Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(22, 35, 65), ForeColor = Color.White };
            pnlFilter.Controls.AddRange(new Control[] { dtpStart, lblTo, dtpEnd });

            var btnRefresh = MakeBtn("🔄 Verileri Güncelle", cBlue, 330, 38);
            btnRefresh.Size = new Size(160, 32);
            pnlFilter.Controls.Add(btnRefresh);

            // PDF Butonları
            var btnFinPdf = MakeBtn("💰 Finansal PDF", Color.FromArgb(180, 50, 50), 500, 38);
            btnFinPdf.Size = new Size(130, 32);
            btnFinPdf.Click += (s, e) => ExportToPdf(dtpStart.Value, dtpEnd.Value, "Tüm Odalar", false);
            pnlFilter.Controls.Add(btnFinPdf);

            var btnMgmtPdf = MakeBtn("📋 Yönetim PDF", Color.FromArgb(50, 100, 180), 640, 38);
            btnMgmtPdf.Size = new Size(130, 32);
            btnMgmtPdf.Click += (s, e) => ExportToPdf(dtpStart.Value, dtpEnd.Value, "Tüm Odalar", true);
            pnlFilter.Controls.Add(btnMgmtPdf);

            pnlMainContent.Controls.Add(pnlFilter);
            y += 100;

            // === 2. ANA İÇERİK (SOL: Tablo/Grafik, SAĞ: Kartlar) ===
            int colLeftW = (int)(w * 0.68);
            int colRightW = w - colLeftW - 15;

            // --- SOL: ODA BAZLI PERFORMANS ---
            var pnlLeft = new Panel { Location = new Point(20, y), Size = new Size(colLeftW, pnlMainContent.ClientSize.Height - y - 20), BackColor = Color.Transparent };
            pnlMainContent.Controls.Add(pnlLeft);

            var pnlPerfHeader = new Panel { Location = new Point(0, 0), Size = new Size(colLeftW, 40), BackColor = Color.Transparent };
            pnlPerfHeader.Controls.Add(new Label { Text = "ODA BAZLI KAZANÇ VE PERFORMANS (TARİH ARALIKLI)", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(0, 10) });
            pnlLeft.Controls.Add(pnlPerfHeader);

            // Grafik Paneli (TOP 5)
            var pnlChart = new Panel { Location = new Point(0, 45), Size = new Size(colLeftW, 220), BackColor = cCard };
            pnlLeft.Controls.Add(pnlChart);

            // Tablo Paneli
            var dgPerf = MakeGrid(275, pnlLeft.Height - 275);
            dgPerf.Size = new Size(colLeftW, pnlLeft.Height - 275);
            dgPerf.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="No", HeaderText="Oda No", Width=60},
                new DataGridViewTextBoxColumn{Name="Tip", HeaderText="Oda Tipi", Width=100},
                new DataGridViewTextBoxColumn{Name="Doluluk", HeaderText="Doluluk Gün", Width=90},
                new DataGridViewTextBoxColumn{Name="Gelir", HeaderText="Toplam Gelir (TL)", Width=120},
                new DataGridViewTextBoxColumn{Name="Kar", HeaderText="Net Kâr (TL)", Width=100}
            });
            pnlLeft.Controls.Add(dgPerf);

            // --- SAĞ: ÖZET KARTLAR VE METRİKLER ---
            var pnlRight = new Panel { Location = new Point(20 + colLeftW + 15, y), Size = new Size(colRightW, pnlMainContent.ClientSize.Height - y - 20), BackColor = Color.Transparent };
            pnlMainContent.Controls.Add(pnlRight);

            int ry = 0;

            // Otomatik Rapor Durumu (Image'daki yeşil bar)
            var pnlPdfStatus = new Panel { Location = new Point(0, ry), Size = new Size(colRightW, 70), BackColor = Color.FromArgb(20, 45, 40) };
            pnlPdfStatus.Paint += (s, e) => { using var p = new Pen(Color.FromArgb(40, 200, 100), 1); e.Graphics.DrawRectangle(p, 0, 0, pnlPdfStatus.Width - 1, pnlPdfStatus.Height - 1); };
            var lblPdfStatusIcon = new Label { Text = "✔️", Font = new Font("Segoe UI", 14F), ForeColor = Color.FromArgb(40, 200, 100), Location = new Point(10, 20), AutoSize = true };
            var lblPdfStatusText = new Label { Text = $"Bugünkü Rapor ({DateTime.Now:dd/MM/yyyy}) Başarıyla\nGönderildi: [Yönetici Mail]", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.White, Location = new Point(45, 18), AutoSize = true };
            pnlPdfStatus.Controls.AddRange(new Control[] { lblPdfStatusIcon, lblPdfStatusText });
            pnlRight.Controls.Add(pnlPdfStatus);
            ry += 85;

            // Kartlar (Grid gibi alt alta)
            var mkCard = (string icon, string title, string val, Color c, string sub = "") => {
                var p = new Panel { Location = new Point(0, ry), Size = new Size(colRightW, 90), BackColor = cCard };
                p.Paint += (s, e) => { e.Graphics.FillRectangle(new SolidBrush(c), 0, 0, 5, p.Height); };
                p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.FromArgb(180, 190, 210), Location = new Point(15, 12), AutoSize = true });
                p.Controls.Add(new Label { Text = val, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 35), AutoSize = true });
                if (!string.IsNullOrEmpty(sub))
                    p.Controls.Add(new Label { Text = sub, Font = new Font("Segoe UI", 8.5F), ForeColor = c, Location = new Point(15, 68), AutoSize = true });
                pnlRight.Controls.Add(p);
                ry += 100;
            };

            // Dinamik verileri tutacak değişkenler
            List<PaymentHelper.RoomPerformance> stats = new();

            Action updateData = () => {
                DateTime start = dtpStart.Value.Date;
                DateTime end = dtpEnd.Value.Date;
                stats = PaymentHelper.GetRoomPerformanceStats(start, end);
                
                decimal totalGross = stats.Sum(s => s.TotalRevenue);
                decimal totalExp = totalGross * 0.22m; // %22 gider simülasyonu
                decimal totalNet = totalGross - totalExp;
                int totalDays = (int)(end - start).TotalDays + 1;
                int roomCount = RoomHelper.GetRoomCount();
                int totalOccDays = stats.Sum(s => s.OccupancyDays);
                
                decimal adr = totalOccDays > 0 ? totalGross / totalOccDays : 0;
                decimal revPar = (roomCount * totalDays) > 0 ? totalGross / (roomCount * totalDays) : 0;
                double gop = totalGross > 0 ? (double)(totalNet / totalGross * 100) : 0;

                // Sağ Paneli Yenile
                pnlRight.Controls.Clear(); ry = 0;
                mkCard("💰", "GÜNLÜK BRÜT GELİR (Dönem Ort.)", $"₺{(totalGross/(totalDays > 0 ? totalDays : 1)):N0} TL", cGold, "↑ 4.50% vs geçen ay");
                mkCard("📉", "GÜNLÜK GİDERLER (Tahmini)", $"₺{(totalExp/(totalDays > 0 ? totalDays : 1)):N0} TL", cRed, "↓ 2.10% tasarruf");
                mkCard("📈", "NET GÜNLÜK KÂR", $"₺{(totalNet/(totalDays > 0 ? totalDays : 1)):N0} TL", cGreen, "↑ 9.7% artış");

                // ADR / RevPAR / GOP Paneli
                var pnlMetrics = new Panel { Location = new Point(0, ry), Size = new Size(colRightW, 110), BackColor = cCard };
                pnlMetrics.Controls.Add(new Label { Text = "ADR:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(15, 15), AutoSize = true });
                pnlMetrics.Controls.Add(new Label { Text = $"₺{adr:N2}", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(15, 35), AutoSize = true });
                
                pnlMetrics.Controls.Add(new Label { Text = "RevPAR:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(110, 15), AutoSize = true });
                pnlMetrics.Controls.Add(new Label { Text = $"₺{revPar:N2}", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(110, 35), AutoSize = true });
                
                pnlMetrics.Controls.Add(new Label { Text = "GOP %:", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(205, 15), AutoSize = true });
                pnlMetrics.Controls.Add(new Label { Text = $"%{gop:N1}", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(205, 35), AutoSize = true });
                pnlRight.Controls.Add(pnlMetrics);

                // Tabloyu Güncelle
                dgPerf.Rows.Clear();
                foreach(var s in stats) {
                    int idx = dgPerf.Rows.Add(s.RoomNumber, s.RoomType, s.OccupancyDays, $"₺{s.TotalRevenue:N0}", $"₺{s.NetProfit:N0}");
                    dgPerf.Rows[idx].DefaultCellStyle.BackColor = Color.FromArgb(24, 34, 58);
                }

                // Grafiği Yenile
                pnlChart.Invalidate();
            };

            // Grafik Çizimi (Paint Event)
            pnlChart.Paint += (s, e) => {
                var g = e.Graphics; g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var top5 = stats.OrderByDescending(x => x.TotalRevenue).Take(5).ToList();
                if (top5.Count == 0) {
                    g.DrawString("Veri bulunamadı.", new Font("Segoe UI", 10), Brushes.Gray, 50, 100);
                    return;
                }

                float max = (float)top5[0].TotalRevenue; if (max <= 0) max = 1;
                int startX = 60, startY = 170;
                int barW = 35, gap = 60;
                
                // Legend
                g.FillRectangle(new SolidBrush(cGreen), 15, 15, 12, 12);
                g.DrawString("TOPLAM GELİR (TL)", new Font("Segoe UI", 8), Brushes.White, 32, 14);
                g.FillRectangle(new SolidBrush(cBlue), 160, 15, 12, 12);
                g.DrawString("NET KÂR (TL)", new Font("Segoe UI", 8), Brushes.White, 177, 14);

                for (int i = 0; i < top5.Count; i++) {
                    float hRev = (float)top5[i].TotalRevenue / max * 120;
                    float hNet = (float)top5[i].NetProfit / max * 120;
                    
                    int x = startX + (i * (barW * 2 + gap / 2));
                    // Gelir Barı
                    g.FillRectangle(new SolidBrush(cGreen), x, startY - hRev, barW, hRev);
                    // Kâr Barı
                    g.FillRectangle(new SolidBrush(cBlue), x + barW + 2, startY - hNet, barW, hNet);
                    
                    // Oda No
                    g.DrawString(top5[i].RoomNumber, new Font("Segoe UI", 8, FontStyle.Bold), Brushes.White, x + barW/2, startY + 5);
                }
                // Eksen Çizgisi
                g.DrawLine(Pens.Gray, startX - 10, startY, pnlChart.Width - 20, startY);
            };

            btnRefresh.Click += (s, e) => updateData();
            updateData();
        }

        private void ShowHousekeeping()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("🧹 Kat Hizmetleri Takip Paneli"));
            int w = pnlMainContent.ClientSize.Width - 40;

            // === ÖZET ===
            int dirtyCount = HousekeepingHelper.GetDirtyRoomCount();
            var pnlSummary = new Panel { Location = new Point(20, 65), Size = new Size(w, 80), BackColor = cCard };
            pnlSummary.Controls.Add(new Label { Text = "🧹", Font = new Font("Segoe UI Emoji", 24F), Location = new Point(15, 15), AutoSize = true });
            pnlSummary.Controls.Add(new Label { Text = "Kirli Odalar", Font = new Font("Segoe UI", 10F), ForeColor = cText, Location = new Point(70, 15), AutoSize = true });
            pnlSummary.Controls.Add(new Label { Text = dirtyCount.ToString(), Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = cRed, Location = new Point(70, 35), AutoSize = true });
            
            var btnRefreshHk = MakeBtn("🔄 Yenile", Color.FromArgb(40, 60, 90), w - 120, 20);
            btnRefreshHk.Size = new Size(100, 34);
            btnRefreshHk.Click += (s, e) => ShowHousekeeping();
            pnlSummary.Controls.Add(btnRefreshHk);
            pnlMainContent.Controls.Add(pnlSummary);

            // === ODA LİSTESİ (Temizlik Durumlu) ===
            var dg = MakeGrid(155, 300);
            dg.Size = new Size(w, 300);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Oda", HeaderText="Oda No", Width=80},
                new DataGridViewTextBoxColumn{Name="Tip", HeaderText="Oda Tipi"},
                new DataGridViewTextBoxColumn{Name="OdaDurum", HeaderText="Oda Durumu", Width=120},
                new DataGridViewTextBoxColumn{Name="Temizlik", HeaderText="Temizlik Durumu", Width=150}
            });
            var btnColClean = new DataGridViewButtonColumn { Name = "ActionClean", HeaderText = "İşlem", Width = 110 };
            dg.Columns.Add(btnColClean);
            pnlMainContent.Controls.Add(dg);

            var rooms = RoomHelper.GetAllRooms();
            foreach (var r in rooms)
            {
                int idx = dg.Rows.Add(r.RoomNumber, r.RoomTypeName, r.Status, r.CleaningStatus);
                var cell = dg.Rows[idx].Cells["Temizlik"];
                cell.Style.ForeColor = Color.White;
                cell.Style.BackColor = r.CleaningStatus switch {
                    "Clean" => cGreen,
                    "Dirty" => cRed,
                    "Cleaning" => cBlue,
                    _ => Color.Gray
                };
                dg.Rows[idx].Cells["ActionClean"].Value = r.CleaningStatus switch {
                    "Dirty" => "🚿 Temizle",
                    "Cleaning" => "✅ Bitir",
                    "Clean" => "🧹 Kirlet (Log)",
                    _ => "-"
                };
            }

            dg.CellContentClick += (s, e) => {
                if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name == "ActionClean") {
                    var r = rooms[e.RowIndex];
                    string nextStatus = r.CleaningStatus switch {
                        "Dirty" => "Cleaning",
                        "Cleaning" => "Clean",
                        "Clean" => "Dirty",
                        _ => r.CleaningStatus
                    };
                    
                    HousekeepingHelper.ChangeCleaningStatus(r.Id, nextStatus, nextStatus == "Clean" ? "Temizlik tamamlandı." : (nextStatus == "Cleaning" ? "Temizliğe başlandı." : "Oda kirli olarak işaretlendi."));
                    ShowHousekeeping();
                    UpdateDirtyBadge();
                }
            };

            // === SON LOGLAR ===
            pnlMainContent.Controls.Add(new Label { Text = "🕒 Son Temizlik Aktiviteleri", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 470), AutoSize = true });
            var dgLogs = MakeGrid(505, pnlMainContent.Height - 520);
            dgLogs.Size = new Size(w, pnlMainContent.ClientSize.Height - 520);
            dgLogs.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Zaman", HeaderText="Zaman", Width=130},
                new DataGridViewTextBoxColumn{Name="OdaLog", HeaderText="Oda", Width=70},
                new DataGridViewTextBoxColumn{Name="Personel", HeaderText="Personel", Width=120},
                new DataGridViewTextBoxColumn{Name="Islem", HeaderText="İşlem"},
                new DataGridViewTextBoxColumn{Name="Notlar", HeaderText="Notlar"}
            });
            pnlMainContent.Controls.Add(dgLogs);

            var logs = HousekeepingHelper.GetAllLogs();
            foreach (var l in logs) {
                dgLogs.Rows.Add(l.CreatedAt.ToString("dd.MM HH:mm"), l.RoomNumber, l.StaffName, $"{l.StatusFrom} ➜ {l.StatusTo}", l.Notes);
            }
        }

        private void ShowRestaurant()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("🍽️ Restoran / POS Yönetimi"));
            int w = pnlMainContent.ClientSize.Width - 40;

            // === ÜST PANEL (Yeni Sipariş & İstatistik) ===
            var pnlTop = new Panel { Location = new Point(20, 65), Size = new Size(w, 80), BackColor = cCard };
            pnlTop.Controls.Add(new Label { Text = "🍔", Font = new Font("Segoe UI Emoji", 24F), Location = new Point(15, 15), AutoSize = true });
            var btnNewOrder = MakeBtn("➕ Yeni Sipariş Al", cGreen, 70, 22);
            btnNewOrder.Size = new Size(160, 36);
            btnNewOrder.Click += (s, e) => ShowNewOrderDialog();
            pnlTop.Controls.Add(btnNewOrder);
            pnlMainContent.Controls.Add(pnlTop);

            // === AKTİF SİPARİŞLER (Kartlar) ===
            pnlMainContent.Controls.Add(new Label { Text = "🔔 Aktif Siparişler", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 160), AutoSize = true });
            var flpOrders = new FlowLayoutPanel { Location = new Point(20, 195), Size = new Size(w, 250), AutoScroll = true, BackColor = Color.Transparent };
            pnlMainContent.Controls.Add(flpOrders);

            Action loadOrders = () => {
                flpOrders.Controls.Clear();
                var activeOrders = RestaurantHelper.GetActiveOrders();
                foreach (var o in activeOrders)
                {
                    var card = new Panel { Size = new Size(200, 110), BackColor = cCard, Margin = new Padding(0, 0, 15, 15) };
                    card.Controls.Add(new Label { Text = string.IsNullOrEmpty(o.RoomNumber) ? $"Masa: {o.TableNumber}" : $"Oda: {o.RoomNumber}", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGold, Location = new Point(10, 10), AutoSize = true });
                    card.Controls.Add(new Label { Text = $"Tutar: ₺{o.TotalAmount:N0}", ForeColor = Color.White, Location = new Point(10, 35), AutoSize = true });
                    card.Controls.Add(new Label { Text = o.CreatedAt.ToString("HH:mm"), ForeColor = Color.Gray, Location = new Point(10, 55), AutoSize = true });
                    
                    var btnComplete = new Button { Text = "Tamamla", Size = new Size(85, 25), Location = new Point(10, 75), FlatStyle = FlatStyle.Flat, BackColor = cGreen, ForeColor = Color.White, Font = new Font("Segoe UI", 8F) };
                    btnComplete.FlatAppearance.BorderSize = 0;
                    btnComplete.Click += (s, e) => { RestaurantHelper.UpdateOrderStatus(o.Id, "Tamamlandi"); ShowRestaurant(); };
                    card.Controls.Add(btnComplete);

                    var btnChargeToRoom = new Button { Text = "Odaya Yaz", Size = new Size(85, 25), Location = new Point(100, 75), FlatStyle = FlatStyle.Flat, BackColor = cBlue, ForeColor = Color.White, Font = new Font("Segoe UI", 8F), Visible = o.RoomId != null };
                    btnChargeToRoom.FlatAppearance.BorderSize = 0;
                    btnChargeToRoom.Click += (s, e) => { 
                        var activeRes = ReservationHelper.GetActiveReservations().FirstOrDefault(r => r.RoomId == o.RoomId);
                        if (activeRes == null) {
                            MessageBox.Show("Bu odaya ait aktif bir rezervasyon bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        // Sadece statüyü OdayaYazildi yapıyoruz. Tahsilat, Ödeme ekranında borca eklenecek.
                        RestaurantHelper.UpdateOrderStatus(o.Id, "OdayaYazildi"); 
                        ShowRestaurant(); 
                    };
                    card.Controls.Add(btnChargeToRoom);
                    flpOrders.Controls.Add(card);
                }
                if (activeOrders.Count == 0) flpOrders.Controls.Add(new Label { Text = "Şu an aktif sipariş yok.", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10F), AutoSize = true, Margin = new Padding(10) });
            };
            loadOrders();

            // === ÜRÜN LİSTESİ (Tablo) ===
            pnlMainContent.Controls.Add(new Label { Text = "📋 Ürün Kataloğu & Fiyatlar", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 460), AutoSize = true });
            var dgProd = MakeGrid(495, pnlMainContent.Height - 510);
            dgProd.Size = new Size(w, pnlMainContent.ClientSize.Height - 510);
            dgProd.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Kategori", HeaderText="Kategori", Width=150},
                new DataGridViewTextBoxColumn{Name="Urun", HeaderText="Ürün Adı"},
                new DataGridViewTextBoxColumn{Name="Fiyat", HeaderText="Fiyat ₺", Width=100}
            });
            pnlMainContent.Controls.Add(dgProd);
            var products = RestaurantHelper.GetAllProducts();
            foreach (var p in products) dgProd.Rows.Add(p.CategoryName, p.Name, $"₺{p.Price:N2}");
        }

        private void ShowNewOrderDialog()
        {
            var f = new Form { Text = "Yeni Restoran Siparişi", Size = new Size(800, 600), StartPosition = FormStartPosition.CenterParent, BackColor = cBg, ForeColor = Color.White };
            var pnlLeft = new Panel { Dock = DockStyle.Left, Width = 450, Padding = new Padding(10) };
            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = cCard, Padding = new Padding(10) };
            f.Controls.Add(pnlRight); f.Controls.Add(pnlLeft);

            // Sol: Ürün seçimi
            pnlLeft.Controls.Add(new Label { Text = "Ürün Seçin", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
            var flpProds = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlLeft.Controls.Add(flpProds);

            // Sağ: Sepet
            pnlRight.Controls.Add(new Label { Text = "Sipariş Detayı", Font = new Font("Segoe UI", 12F, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 });
            var dgCart = new DataGridView { Dock = DockStyle.Top, Height = 300, BackgroundColor = cCard, ForeColor = Color.Black, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
            dgCart.Columns.AddRange(new DataGridViewColumn[] { new DataGridViewTextBoxColumn{Name="Ad",HeaderText="Ürün"}, new DataGridViewTextBoxColumn{Name="Adet",HeaderText="Adet",Width=50}, new DataGridViewTextBoxColumn{Name="Tutar",HeaderText="Tutar",Width=80} });
            pnlRight.Controls.Add(dgCart);

            var lblTotal = new Label { Text = "Toplam: ₺0", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = cGold, Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleRight };
            pnlRight.Controls.Add(lblTotal);

            // Oda/Masa seçimi
            var pnlTarget = new Panel { Dock = DockStyle.Top, Height = 100 };
            pnlTarget.Controls.Add(new Label { Text = "Oda Seç (İsteğe Bağlı):", Location = new Point(10, 10), AutoSize = true });
            var cmbRoom = new ComboBox { Location = new Point(10, 30), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoom.Items.Add("-- Masa Satışı --");
            var rooms = RoomHelper.GetAllRooms().FindAll(x => x.Status == "Occupied");
            foreach (var r in rooms) cmbRoom.Items.Add($"Oda {r.RoomNumber}");
            cmbRoom.SelectedIndex = 0;
            pnlTarget.Controls.Add(cmbRoom);

            pnlTarget.Controls.Add(new Label { Text = "Masa No:", Location = new Point(180, 10), AutoSize = true });
            var txtTable = new TextBox { Location = new Point(180, 30), Size = new Size(100, 25) };
            pnlTarget.Controls.Add(txtTable);
            pnlRight.Controls.Add(pnlTarget);

            var btnSave = new Button { Text = "Siparişi Onayla", Dock = DockStyle.Bottom, Height = 50, BackColor = cGreen, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            f.Controls.Add(btnSave);

            List<RestaurantOrderItem> cartItems = new List<RestaurantOrderItem>();
            Action updateCart = () => {
                dgCart.Rows.Clear();
                decimal total = 0;
                foreach (var item in cartItems) {
                    dgCart.Rows.Add(item.ProductName, item.Quantity, $"₺{item.Total:N0}");
                    total += item.Total;
                }
                lblTotal.Text = $"Toplam: ₺{total:N0}";
            };

            var products = RestaurantHelper.GetAllProducts();
            foreach (var p in products) {
                var btn = new Button { Text = $"{p.Name}\n₺{p.Price:N0}", Size = new Size(130, 60), Margin = new Padding(5), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(40, 55, 80), Font = new Font("Segoe UI", 9F) };
                btn.Click += (s, e) => {
                    var existing = cartItems.Find(x => x.ProductId == p.Id);
                    if (existing != null) existing.Quantity++;
                    else cartItems.Add(new RestaurantOrderItem { ProductId = p.Id, ProductName = p.Name, Quantity = 1, UnitPrice = p.Price });
                    updateCart();
                };
                flpProds.Controls.Add(btn);
            }

            btnSave.Click += (s, e) => {
                if (cartItems.Count == 0) return;
                var order = new RestaurantOrder {
                    RoomId = cmbRoom.SelectedIndex > 0 ? rooms[cmbRoom.SelectedIndex - 1].Id : (int?)null,
                    TableNumber = txtTable.Text,
                    TotalAmount = cartItems.ConvertAll(x => x.Total).Sum(),
                    Items = cartItems
                };
                RestaurantHelper.CreateOrder(order);
                f.Close();
                ShowRestaurant();
            };

            f.ShowDialog();
        }

        private void ShowMaintenance()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("🔧 Teknik Servis & Bakım Yönetimi"));
            int w = pnlMainContent.ClientSize.Width - 40;

            // === ÜST PANEL (Yeni Kayıt & Filtre) ===
            var pnlTop = new Panel { Location = new Point(20, 65), Size = new Size(w, 80), BackColor = cCard };
            pnlTop.Controls.Add(new Label { Text = "🛠️", Font = new Font("Segoe UI Emoji", 24F), Location = new Point(15, 15), AutoSize = true });
            var btnNewReq = MakeBtn("➕ Yeni Arıza Kaydı", cBlue, 70, 22);
            btnNewReq.Size = new Size(180, 36);
            btnNewReq.Click += (s, e) => ShowNewMaintenanceDialog();
            pnlTop.Controls.Add(btnNewReq);

            var btnRefreshM = MakeBtn("🔄 Yenile", Color.FromArgb(40, 60, 90), w - 120, 22);
            btnRefreshM.Size = new Size(100, 36);
            btnRefreshM.Click += (s, e) => ShowMaintenance();
            pnlTop.Controls.Add(btnRefreshM);
            pnlMainContent.Controls.Add(pnlTop);

            // === ARIZA LİSTESİ (Tablo) ===
            var dg = MakeGrid(155, pnlMainContent.Height - 180);
            dg.Size = new Size(w, pnlMainContent.ClientSize.Height - 180);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="ID", HeaderText="#", Width=40},
                new DataGridViewTextBoxColumn{Name="Oda", HeaderText="Oda", Width=70},
                new DataGridViewTextBoxColumn{Name="Kategori", HeaderText="Kategori", Width=110},
                new DataGridViewTextBoxColumn{Name="Aciklama", HeaderText="Açıklama"},
                new DataGridViewTextBoxColumn{Name="Oncelik", HeaderText="Öncelik", Width=90},
                new DataGridViewTextBoxColumn{Name="Durum", HeaderText="Durum", Width=120},
                new DataGridViewTextBoxColumn{Name="Tarih", HeaderText="Tarih", Width=110}
            });
            var btnColAction = new DataGridViewButtonColumn { Name = "Action", HeaderText = "İşlem", Width = 110 };
            dg.Columns.Add(btnColAction);
            pnlMainContent.Controls.Add(dg);

            var requests = MaintenanceHelper.GetAllRequests();
            foreach (var r in requests)
            {
                int idx = dg.Rows.Add(r.Id, r.RoomNumber, r.Category, r.Description, r.Priority, r.Status, r.CreatedAt.ToString("dd.MM HH:mm"));
                
                // Öncelik renklendirme
                var cellPrio = dg.Rows[idx].Cells["Oncelik"];
                cellPrio.Style.ForeColor = r.Priority switch {
                    "Acil" => Color.White,
                    "Yüksek" => Color.White,
                    _ => Color.White
                };
                cellPrio.Style.BackColor = r.Priority switch {
                    "Acil" => Color.FromArgb(220, 20, 60),
                    "Yüksek" => Color.FromArgb(255, 69, 0),
                    "Orta" => Color.FromArgb(218, 165, 32),
                    _ => Color.FromArgb(34, 139, 34)
                };

                // Durum renklendirme
                var cellStatus = dg.Rows[idx].Cells["Durum"];
                cellStatus.Style.BackColor = r.Status switch {
                    "Bekliyor" => Color.FromArgb(60, 60, 60),
                    "Devam Ediyor" => Color.FromArgb(0, 120, 215),
                    "Tamamlandi" => Color.FromArgb(34, 139, 34),
                    "Iptal" => Color.FromArgb(120, 0, 0),
                    _ => Color.Gray
                };

                dg.Rows[idx].Cells["Action"].Value = r.Status switch {
                    "Bekliyor" => "▶️ Başlat",
                    "Devam Ediyor" => "✅ Bitir",
                    "Tamamlandi" => "🔍 Detay",
                    _ => "🔍 Detay"
                };
            }

            dg.CellContentClick += (s, e) => {
                if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name == "Action") {
                    var r = requests[e.RowIndex];
                    if (r.Status == "Bekliyor") {
                        MaintenanceHelper.UpdateStatus(r.Id, "Devam Ediyor");
                        ShowMaintenance();
                    } else if (r.Status == "Devam Ediyor") {
                        MaintenanceHelper.UpdateStatus(r.Id, "Tamamlandi");
                        ShowMaintenance();
                    } else {
                        MessageBox.Show($"Arıza Detayı:\n\nKategori: {r.Category}\nBildiren: {r.ReportedByName}\nAçıklama: {r.Description}\nAtanan: {r.AssignedTo ?? "-"}", "Kayıt Detayı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };
        }

        private void ShowNewMaintenanceDialog()
        {
            var f = new Form { Text = "Yeni Teknik Arıza Kaydı", Size = new Size(500, 550), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog };
            int y = 20;

            f.Controls.Add(new Label { Text = "Oda (Seçilmezse Genel Alan):", Location = new Point(20, y), AutoSize = true });
            var cmbRoom = new ComboBox { Location = new Point(20, y + 20), Size = new Size(440, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRoom.Items.Add("-- Genel Alan --");
            var rooms = RoomHelper.GetAllRooms();
            foreach (var r in rooms) cmbRoom.Items.Add($"Oda {r.RoomNumber}");
            cmbRoom.SelectedIndex = 0;
            f.Controls.Add(cmbRoom);
            y += 60;

            f.Controls.Add(new Label { Text = "Kategori:", Location = new Point(20, y), AutoSize = true });
            var cmbCat = new ComboBox { Location = new Point(20, y + 20), Size = new Size(440, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCat.Items.AddRange(new string[] { "Elektrik", "Su Tesisatı", "Klima / Isıtma", "Mobilya", "Elektronik / TV", "İnternet", "Diğer" });
            cmbCat.SelectedIndex = 0;
            f.Controls.Add(cmbCat);
            y += 60;

            f.Controls.Add(new Label { Text = "Öncelik:", Location = new Point(20, y), AutoSize = true });
            var cmbPrio = new ComboBox { Location = new Point(20, y + 20), Size = new Size(440, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPrio.Items.AddRange(new string[] { "Düşük", "Orta", "Yüksek", "Acil" });
            cmbPrio.SelectedIndex = 1;
            f.Controls.Add(cmbPrio);
            y += 60;

            f.Controls.Add(new Label { Text = "Açıklama:", Location = new Point(20, y), AutoSize = true });
            var txtDesc = new TextBox { Location = new Point(20, y + 20), Size = new Size(440, 100), Multiline = true };
            f.Controls.Add(txtDesc);
            y += 130;

            f.Controls.Add(new Label { Text = "Atanan Personel / Firma:", Location = new Point(20, y), AutoSize = true });
            var txtAsg = new TextBox { Location = new Point(20, y + 20), Size = new Size(440, 28) };
            f.Controls.Add(txtAsg);
            y += 60;

            var btnSave = MakeBtn("💾 KAYDET", cGreen, 20, y);
            btnSave.Size = new Size(440, 45);
            btnSave.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtDesc.Text)) { MessageBox.Show("Lütfen açıklama girin."); return; }
                var req = new MaintenanceRequest {
                    RoomId = cmbRoom.SelectedIndex > 0 ? rooms[cmbRoom.SelectedIndex - 1].Id : (int?)null,
                    Category = cmbCat.Text,
                    Priority = cmbPrio.Text,
                    Description = txtDesc.Text,
                    AssignedTo = txtAsg.Text
                };
                MaintenanceHelper.CreateRequest(req);
                f.Close();
                UpdateMaintenanceBadge();
            };
            f.Controls.Add(btnSave);
            f.ShowDialog();
        }



        private void ExportToPdf(DateTime start, DateTime end, string roomFilter, bool isManagement)
        {
            string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "AFM_Grand_Raporlar");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            string fileName = isManagement ? $"Yonetim_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf" : $"Finans_Raporu_{DateTime.Now:yyyyMMdd_HHmm}.pdf";
            string fullPath = Path.Combine(folderPath, fileName);

            var pd = new System.Drawing.Printing.PrintDocument();
            pd.DocumentName = fileName;
            
            // Otomatik Kayıt Ayarları
            pd.PrinterSettings.PrinterName = "Microsoft Print to PDF";
            pd.PrinterSettings.PrintToFile = true;
            pd.PrinterSettings.PrintFileName = fullPath;

            pd.PrintPage += (s, e) => {
                Graphics g = e.Graphics!;
                Font titleFont = new Font("Segoe UI", 18, FontStyle.Bold);
                Font subTitleFont = new Font("Segoe UI", 12, FontStyle.Bold);
                Font headerFont = new Font("Segoe UI", 10, FontStyle.Bold);
                Font bodyFont = new Font("Segoe UI", 9);
                Brush goldBrush = new SolidBrush(Color.FromArgb(180, 140, 20));
                Brush blueBrush = new SolidBrush(Color.FromArgb(20, 60, 120));
                
                int curY = 40;
                string reportTitle = isManagement ? "📋 AFM GRAND HOTEL - YÖNETİM AKTİVİTE RAPORU" : "💰 AFM GRAND HOTEL - FİNANSAL TAHSİLAT RAPORU";
                g.DrawString(reportTitle, titleFont, isManagement ? blueBrush : goldBrush, 40, curY); curY += 40;
                g.DrawString($"Dönem: {start:dd.MM.yyyy} - {end:dd.MM.yyyy} | Oda: {roomFilter}", subTitleFont, Brushes.Black, 40, curY); curY += 45;

                int? roomId = cmbRooms_ref?.SelectedIndex > 0 ? allRooms_ref?[cmbRooms_ref.SelectedIndex - 1].Id : null;

                if (!isManagement)
                {
                    // --- FİNANSAL RAPOR İÇERİĞİ ---
                    g.FillRectangle(new SolidBrush(Color.FromArgb(240, 240, 240)), 40, curY, 720, 30);
                    g.DrawString("FİNANSAL ÖZET", headerFont, Brushes.Black, 45, curY + 7); curY += 40;
                    
                    decimal filteredRev = PaymentHelper.GetFilteredRevenue(start, end, roomId);
                    g.DrawString($"Dönem Toplam Geliri: ₺{filteredRev:N2}", subTitleFont, Brushes.DarkGreen, 50, curY); curY += 40;

                    g.DrawString("Tarih", headerFont, Brushes.Black, 45, curY);
                    g.DrawString("Oda", headerFont, Brushes.Black, 180, curY);
                    g.DrawString("Misafir", headerFont, Brushes.Black, 250, curY);
                    g.DrawString("Yöntem", headerFont, Brushes.Black, 500, curY);
                    g.DrawString("Tutar", headerFont, Brushes.Black, 650, curY);
                    curY += 25; g.DrawLine(Pens.Black, 40, curY, 760, curY); curY += 10;

                    var payments = PaymentHelper.GetFilteredPayments(start, end, roomId);
                    foreach (var p in payments) {
                        if (curY > 1050) break;
                        g.DrawString(p.PaymentDate.ToString("dd.MM.yyyy HH:mm"), bodyFont, Brushes.Black, 45, curY);
                        g.DrawString(p.RoomNumber, bodyFont, Brushes.Black, 180, curY);
                        g.DrawString(p.GuestName, bodyFont, Brushes.Black, 250, curY);
                        g.DrawString(p.PaymentMethod, bodyFont, Brushes.Black, 500, curY);
                        g.DrawString($"₺{p.Amount:N0}", bodyFont, Brushes.Black, 650, curY);
                        curY += 20;
                    }
                }
                else
                {
                    // --- YÖNETİM RAPORU İÇERİĞİ ---
                    // 1. Giriş Yapanlar
                    g.FillRectangle(new SolidBrush(Color.FromArgb(230, 240, 250)), 40, curY, 720, 30);
                    g.DrawString("🏨 GİRİŞ YAPAN MİSAFİRLER (CHECK-IN)", headerFont, Brushes.Black, 45, curY + 7); curY += 40;
                    var checkIns = ReservationHelper.GetFilteredCheckIns(start, end, roomId);
                    foreach (var c in checkIns) {
                        g.DrawString($"{c.CheckInDate:dd.MM.yyyy} - Oda {c.RoomNumber}: {c.GuestName}", bodyFont, Brushes.Black, 50, curY); curY += 18;
                    }
                    if (checkIns.Count == 0) { g.DrawString("Kayıt yok.", bodyFont, Brushes.Gray, 50, curY); curY += 18; }
                    curY += 20;

                    // 2. Çıkış Yapanlar
                    g.FillRectangle(new SolidBrush(Color.FromArgb(250, 240, 230)), 40, curY, 720, 30);
                    g.DrawString("🚪 ÇIKIŞ YAPAN MİSAFİRLER (CHECK-OUT)", headerFont, Brushes.Black, 45, curY + 7); curY += 40;
                    var checkOuts = ReservationHelper.GetFilteredCheckOuts(start, end, roomId);
                    foreach (var co in checkOuts) {
                        g.DrawString($"{co.CheckOutDate:dd.MM.yyyy} - Oda {co.RoomNumber}: {co.GuestName}", bodyFont, Brushes.Black, 50, curY); curY += 18;
                    }
                    if (checkOuts.Count == 0) { g.DrawString("Kayıt yok.", bodyFont, Brushes.Gray, 50, curY); curY += 18; }
                    curY += 20;

                    // 3. Online Talepler
                    g.FillRectangle(new SolidBrush(Color.FromArgb(230, 250, 230)), 40, curY, 720, 30);
                    g.DrawString("🌐 WEB SİTESİNDEN GELEN TALEPLER", headerFont, Brushes.Black, 45, curY + 7); curY += 40;
                    var online = OnlineReservationHelper.GetFilteredOnlineReservations(start, end, roomId);
                    foreach (var o in online) {
                        g.DrawString($"{o.CreatedAt:dd.MM HH:mm} - {o.FullName} (Oda {o.RoomNumber}) - Durum: {o.Status}", bodyFont, Brushes.Black, 50, curY); curY += 18;
                    }
                    if (online.Count == 0) { g.DrawString("Kayıt yok.", bodyFont, Brushes.Gray, 50, curY); curY += 18; }
                    curY += 30;

                    // 4. Oda Performans Özetleri
                    g.FillRectangle(new SolidBrush(Color.FromArgb(245, 245, 220)), 40, curY, 720, 30);
                    g.DrawString("📈 ODA BAZLI PERFORMANS ÖZETİ", headerFont, Brushes.Black, 45, curY + 7); curY += 40;
                    var perfStats = PaymentHelper.GetRoomPerformanceStats(start, end);
                    foreach (var ps in perfStats.Take(15)) { 
                        if (curY > 1050) break;
                        g.DrawString($"{ps.RoomNumber} ({ps.RoomType}): {ps.OccupancyDays} Gün Dolu | Gelir: ₺{ps.TotalRevenue:N0} | Kâr: ₺{ps.NetProfit:N0}", bodyFont, Brushes.Black, 50, curY); 
                        curY += 18;
                    }
                }

                curY = 1100; g.DrawLine(Pens.Gray, 40, curY, 760, curY);
                g.DrawString("AFM Grand Hotel Yönetim Sistemi", new Font("Segoe UI", 8), Brushes.Gray, 40, curY + 5);
            };

            try {
                pd.Print();
                MessageBox.Show($"✅ Rapor başarıyla oluşturuldu ve kaydedildi:\n{fullPath}", "Otomatik Kayıt Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Dosyayı otomatik aç (isteğe bağlı)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
            } catch (Exception ex) {
                MessageBox.Show($"❌ PDF Kayıt Hatası: {ex.Message}\nLütfen 'Microsoft Print to PDF' yazıcısının kurulu olduğundan emin olun.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private ComboBox? cmbRooms_ref;
        private List<Room>? allRooms_ref;

        // ========== PERSONEL ==========
        private void ShowPersonel()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("🧑‍💼 Personel Yönetimi"));
            var btnAdd = MakeBtn("➕ Yeni Personel", cGreen, 20, 55); pnlMainContent.Controls.Add(btnAdd);
            var btnDel = MakeBtn("🗑️ Sil", cRed, 190, 55); pnlMainContent.Controls.Add(btnDel);
            var dg = MakeGrid(100, 500); dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 130);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=40},
                new DataGridViewTextBoxColumn{Name="Ad",HeaderText="Ad Soyad"},
                new DataGridViewTextBoxColumn{Name="Kullanici",HeaderText="Kullanıcı Adı",Width=120},
                new DataGridViewTextBoxColumn{Name="Rol",HeaderText="Rol",Width=110},
                new DataGridViewTextBoxColumn{Name="Email",HeaderText="Email",Width=150},
                new DataGridViewTextBoxColumn{Name="Tel",HeaderText="Telefon",Width=120},
                new DataGridViewTextBoxColumn{Name="Durum",HeaderText="Durum",Width=70},
            });
            Action loadStaff = () => { try { dg.Rows.Clear();
                var users = UserHelper.GetAllUsers();
                foreach (var u in users) {
                    int idx = dg.Rows.Add(u.Id, u.FullName, u.Username, u.Role, u.Email ?? "", u.Phone ?? "", u.IsActive ? "✅ Aktif" : "❌ Pasif");
                    Color rc = u.Role switch { "Admin" => cGold, "Resepsiyonist" => cBlue, "Muhasebe" => cGreen, _ => Color.Gray };
                    dg.Rows[idx].Cells["Rol"].Style.BackColor = rc; dg.Rows[idx].Cells["Rol"].Style.ForeColor = Color.White;
                }
            } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            loadStaff();
            btnAdd.Click += (s, e) => { if (ShowPersonelDialog(0)) loadStaff(); };
            dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) { int id = Convert.ToInt32(dg.Rows[e.RowIndex].Cells["Id"].Value); if (ShowPersonelDialog(id)) loadStaff(); } };
            btnDel.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                if (id == AuthHelper.CurrentUser?.Id) { MessageBox.Show("Kendinizi silemezsiniz!"); return; }
                if (MessageBox.Show("Bu personeli silmek istiyor musunuz?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    try { UserHelper.DeleteUser(id); loadStaff();
                    } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } } } };
            pnlMainContent.Controls.Add(dg);
        }

        private bool ShowPersonelDialog(int editId)
        {
            bool isEdit = editId > 0;
            User? u = isEdit ? UserHelper.GetUserById(editId) : null;
            EmployeeDetail? detail = isEdit ? EmployeeHelper.GetByUserId(editId) : null;

            var f = new Form { Text = isEdit ? "Personel Düzenle" : "Yeni Personel", Size = new Size(500, 650), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 20;

            TextBox MakeField(string label, string val = "", int w = 390) { 
                f.Controls.Add(new Label { Text = label, Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = cGold }); y += 22;
                var t = new TextBox { Text = val, Location = new Point(20, y), Size = new Size(w, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; 
                f.Controls.Add(t); y += 38; return t; 
            }

            var tName = MakeField("Ad Soyad *", u?.FullName ?? "");
            var tUser = MakeField("Kullanıcı Adı *", u?.Username ?? "");
            var tPass = MakeField(isEdit ? "Yeni Şifre (Boş bırakılırsa değişmez)" : "Şifre *", "");
            
            f.Controls.Add(new Label { Text = "Rol *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = cGold }); y += 22;
            var cmbRole = new ComboBox { Location = new Point(20, y), Size = new Size(190, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbRole.Items.AddRange(new[] { "Admin", "Resepsiyonist", "Muhasebe" }); cmbRole.SelectedItem = u?.Role ?? "Resepsiyonist";
            f.Controls.Add(cmbRole);

            f.Controls.Add(new Label { Text = "Vardiya:", Location = new Point(220, y - 22), AutoSize = true, Font = new Font("Segoe UI", 9F), ForeColor = cGold });
            var cmbShift = new ComboBox { Location = new Point(220, y), Size = new Size(190, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbShift.Items.AddRange(new[] { "Gündüz", "Gece", "Vardiyalı" }); cmbShift.SelectedItem = detail?.Shift ?? "Gündüz";
            f.Controls.Add(cmbShift); y += 45;

            var tSal = MakeField("Maaş ₺:", detail?.Salary.ToString("N0") ?? "0");
            var tIban = MakeField("IBAN:", detail?.Iban ?? "");
            var tEmail = MakeField("Email", u?.Email ?? "");
            var tPhone = MakeField("Telefon", u?.Phone ?? "");

            var btnSave = MakeBtn("💾 Kaydet", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCancel = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCancel);
            
            bool saved = false;
            btnSave.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(tName.Text) || string.IsNullOrWhiteSpace(tUser.Text)) { MessageBox.Show("Zorunlu alanları doldurun."); return; }
                try {
                    string passwordToSave = (!string.IsNullOrWhiteSpace(tPass.Text)) ? AuthHelper.HashPassword(tPass.Text) : (u?.Password ?? "");
                    var newUser = new User { Id = editId, FullName = tName.Text.Trim(), Username = tUser.Text.Trim(), Password = passwordToSave, Role = cmbRole.Text, Email = tEmail.Text, Phone = tPhone.Text };
                    
                    int userId = isEdit ? editId : UserHelper.AddUser(newUser);
                    if (isEdit) UserHelper.UpdateUser(newUser);

                    var newDetail = new EmployeeDetail { 
                        UserId = userId, 
                        Position = cmbRole.Text, 
                        Salary = decimal.TryParse(tSal.Text, out decimal sal) ? sal : 0, 
                        Shift = cmbShift.Text, 
                        Iban = tIban.Text,
                        HireDate = detail?.HireDate ?? DateTime.Today
                    };
                    EmployeeHelper.SaveDetail(newDetail);

                    saved = true; f.Close();
                } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); }
            };
            btnCancel.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        private void ShowInventory()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📦 Stok ve Envanter Yönetimi"));
            int w = pnlMainContent.ClientSize.Width - 40;

            // === ÜST PANEL ===
            var pnlTop = new Panel { Location = new Point(20, 65), Size = new Size(w, 80), BackColor = cCard };
            pnlTop.Controls.Add(new Label { Text = "📦", Font = new Font("Segoe UI Emoji", 24F), Location = new Point(15, 15), AutoSize = true });
            var btnAddStock = MakeBtn("➕ Mal Girişi", cGreen, 70, 22);
            btnAddStock.Click += (s, e) => ShowStockDialog("Giriş");
            pnlTop.Controls.Add(btnAddStock);
            var btnRemoveStock = MakeBtn("➖ Mal Çıkışı", cRed, 230, 22);
            btnRemoveStock.Click += (s, e) => ShowStockDialog("Çıkış");
            pnlTop.Controls.Add(btnRemoveStock);
            pnlMainContent.Controls.Add(pnlTop);

            // === STOK LİSTESİ ===
            var dg = MakeGrid(155, pnlMainContent.Height - 175);
            dg.Size = new Size(w, pnlMainContent.ClientSize.Height - 175);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Kat", HeaderText="Kategori", Width=120},
                new DataGridViewTextBoxColumn{Name="Ad", HeaderText="Ürün Adı"},
                new DataGridViewTextBoxColumn{Name="Miktar", HeaderText="Mevcut Stok", Width=100},
                new DataGridViewTextBoxColumn{Name="Birim", HeaderText="Birim", Width=70},
                new DataGridViewTextBoxColumn{Name="Kritik", HeaderText="Min. Stok", Width=100},
                new DataGridViewTextBoxColumn{Name="Durum", HeaderText="Durum", Width=110}
            });
            pnlMainContent.Controls.Add(dg);

            var items = InventoryHelper.GetAll();
            foreach (var i in items)
            {
                int idx = dg.Rows.Add(i.Category, i.Name, i.Quantity, i.Unit, i.MinStock);
                if (i.Quantity <= i.MinStock) {
                    dg.Rows[idx].Cells["Durum"].Value = "⚠️ Kritik Seviye";
                    dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.Red;
                } else {
                    dg.Rows[idx].Cells["Durum"].Value = "✅ Yeterli";
                    dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.Lime;
                }
            }
        }

        private void ShowStockDialog(string type)
        {
            var f = new Form { Text = $"Stok {type} İşlemi", Size = new Size(400, 400), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White };
            f.Controls.Add(new Label { Text = "Ürün Seçin:", Location = new Point(20, 20), AutoSize = true });
            var cmb = new ComboBox { Location = new Point(20, 40), Size = new Size(340, 28), DropDownStyle = ComboBoxStyle.DropDownList };
            var items = InventoryHelper.GetAll();
            foreach (var i in items) cmb.Items.Add(i.Name);
            f.Controls.Add(cmb);

            f.Controls.Add(new Label { Text = "Miktar:", Location = new Point(20, 80), AutoSize = true });
            var tQty = new TextBox { Location = new Point(20, 100), Size = new Size(340, 28) };
            f.Controls.Add(tQty);

            f.Controls.Add(new Label { Text = "Notlar:", Location = new Point(20, 140), AutoSize = true });
            var tNotes = new TextBox { Location = new Point(20, 160), Size = new Size(340, 80), Multiline = true };
            f.Controls.Add(tNotes);

            var btn = MakeBtn("💾 KAYDET", type == "Giriş" ? cGreen : cRed, 20, 260);
            btn.Size = new Size(340, 45);
            btn.Click += (s, e) => {
                if (cmb.SelectedIndex >= 0 && decimal.TryParse(tQty.Text, out decimal q)) {
                    InventoryHelper.UpdateStock(items[cmb.SelectedIndex].Id, q, type, tNotes.Text);
                    f.Close();
                    ShowInventory();
                }
            };
            f.Controls.Add(btn);
            f.ShowDialog();
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { base.OnFormClosing(e); }

        // ========== ONLINE REZERVASYONLAR ==========
        private void ShowGuestMonitoring()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("Gerçek Zamanlı Misafir İzleme ve Borç Paneli"));

            int w = pnlMainContent.ClientSize.Width - 40;
            var dg = MakeGrid(20, 70);
            dg.Size = new Size(w, pnlMainContent.ClientSize.Height - 100);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { Name = "Oda", HeaderText = "Oda No", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "Misafir", HeaderText = "Misafir Adı", Width = 180 },
                new DataGridViewTextBoxColumn { Name = "Giris", HeaderText = "Giriş Tarihi", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Cikis", HeaderText = "Planlanan Çıkış", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Sure", HeaderText = "Kalan Gün", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "OdaUcret", HeaderText = "Oda Toplam", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Ekstra", HeaderText = "Ekstralar", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Odenen", HeaderText = "Ödenen", Width = 110 },
                new DataGridViewTextBoxColumn { Name = "Borc", HeaderText = "Kalan Borç", Width = 120 },
                new DataGridViewButtonColumn { Name = "PayLink", HeaderText = "İşlem", Text = "Ödeme Linki", UseColumnTextForButtonValue = true, Width = 100 }
            });
            pnlMainContent.Controls.Add(dg);

            dg.CellContentClick += (s, e) => {
                if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name == "PayLink") {
                    var roomNo = dg.Rows[e.RowIndex].Cells["Oda"].Value.ToString();
                    var res = ReservationHelper.GetActiveInHouseGuests().FirstOrDefault(x => x.RoomNumber == roomNo);
                    if (res != null) {
                        string link = $"http://localhost:5050/checkout-payment.html?resId={res.Id}";
                        Clipboard.SetText(link);
                        MessageBox.Show($"Ödeme linki kopyalandı:\n{link}\n\nMisafire bu linki iletebilirsiniz.", "Ödeme Linki", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            };

            var guests = ReservationHelper.GetActiveInHouseGuests();
            foreach (var g in guests)
            {
                int stayedDays = (DateTime.Today - g.CheckInDate.Date).Days;
                int remainingDays = (g.CheckOutDate.Date - DateTime.Today).Days;
                
                // Ekstraları çek (Basitlik için SQL ile değil helper ile)
                decimal extras = 0;
                using (var conn = DatabaseHelper.GetConnection()) {
                    conn.Open();
                    var cmd = new MySqlCommand("SELECT SUM(total_amount) FROM restaurant_orders WHERE room_id = @rid AND status = 'OdayaYaz'", conn);
                    cmd.Parameters.AddWithValue("@rid", g.RoomId);
                    var res = cmd.ExecuteScalar();
                    extras = res == DBNull.Value ? 0 : Convert.ToDecimal(res);
                }
                
                decimal paid = PaymentHelper.GetTotalPaid(g.Id);
                decimal balance = (g.TotalPrice + extras) - paid;

                int idx = dg.Rows.Add(
                    g.RoomNumber,
                    g.GuestName,
                    g.CheckInDate.ToString("dd.MM.yyyy"),
                    g.CheckOutDate.ToString("dd.MM.yyyy"),
                    remainingDays < 0 ? "Gecikti!" : remainingDays + " Gün",
                    $"₺{g.TotalPrice:N2}",
                    $"₺{extras:N2}",
                    $"₺{paid:N2}",
                    $"₺{balance:N2}"
                );

                if (balance > 0) dg.Rows[idx].Cells["Borc"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                else if (balance < 0) dg.Rows[idx].Cells["Borc"].Style.ForeColor = Color.FromArgb(34, 197, 94);
                
                if (remainingDays == 0) dg.Rows[idx].Cells["Sure"].Style.BackColor = Color.FromArgb(245, 158, 11);
            }
        }

        private void ShowOnlineReservations()
        {
            ClearContent();
            UpdateOnlineBadge();

            int w = pnlMainContent.ClientSize.Width;
            int h = pnlMainContent.ClientSize.Height;

            pnlMainContent.Controls.Add(MakeTitle("Online Rezervasyon Talepleri Kontrol Paneli"));

            // --- ÜST PANEL (Filtreler ve İstatistikler) ---
            var pnlTop = new Panel { Location = new Point(20, 55), Size = new Size(w - 40, 75), BackColor = Color.Transparent };
            
            // Verileri çekip sayıları hesapla
            var allData = OnlineReservationHelper.GetAll(null);
            int countAll = allData.Count;
            int countPending = allData.Count(x => x.Status == "Bekliyor");
            int countApproved = allData.Count(x => x.Status == "Onaylandi");
            int countRejected = allData.Count(x => x.Status == "Reddedildi");

            var btnAll      = MakeBtn($"Tümü ({countAll})",       Color.FromArgb(45, 55, 75), 0, 0);   btnAll.Size = new Size(110, 38);
            var btnPending  = MakeBtn($"Bekliyor ({countPending})",  Color.FromArgb(180, 140, 20), 0, 0); btnPending.Size = new Size(130, 38);
            var btnApproved = MakeBtn($"Onaylandı ({countApproved})", Color.FromArgb(40, 120, 80), 0, 0); btnApproved.Size = new Size(135, 38);
            var btnRejected = MakeBtn($"Reddedildi ({countRejected})", Color.FromArgb(160, 40, 40), 0, 0); btnRejected.Size = new Size(135, 38);
            var btnPaid     = MakeBtn("💳 Ödenmiş & Aktif", Color.FromArgb(100, 80, 160), 0, 0); btnPaid.Size = new Size(160, 38);

            var txtSearch = new TextBox { 
                Size = new Size(250, 38), 
                Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(25, 40, 75), 
                ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, 
                PlaceholderText = "🔍 Ara..." 
            };

            var btnRefresh = MakeBtn("🔄 Yenile", Color.FromArgb(35, 45, 65), 0, 0); btnRefresh.Size = new Size(100, 38);
            var btnManual = MakeBtn("➕ Manuel Talep", Color.FromArgb(35, 45, 65), 0, 0); btnManual.Size = new Size(140, 38);

            // TableLayoutPanel for responsive layout!
            var tableTop = new TableLayoutPanel {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                ColumnCount = 8,
                RowCount = 1,
                Padding = new Padding(0, 10, 0, 0)
            };

            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115));    // btnAll
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));    // btnPending
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));    // btnApproved
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));    // btnRejected
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));    // btnPaid
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));    // txtSearch (fill remaining)
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));    // btnRefresh
            tableTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));    // btnManual

            tableTop.Controls.Add(btnAll, 0, 0);
            tableTop.Controls.Add(btnPending, 1, 0);
            tableTop.Controls.Add(btnApproved, 2, 0);
            tableTop.Controls.Add(btnRejected, 3, 0);
            tableTop.Controls.Add(btnPaid, 4, 0);
            tableTop.Controls.Add(txtSearch, 5, 0);
            tableTop.Controls.Add(btnRefresh, 6, 0);
            tableTop.Controls.Add(btnManual, 7, 0);

            // Set anchors for the textbox to expand correctly!
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            pnlTop.Controls.Add(tableTop);
            pnlMainContent.Controls.Add(pnlTop);

            // --- ANA İÇERİK (SOL: Liste, SAĞ: Quick View) ---
            var split = new SplitContainer {
                Location = new Point(20, 125),
                Size = new Size(w - 40, h - 145),
                SplitterDistance = (int)((w - 40) * 0.70),
                BorderStyle = BorderStyle.None
            };
            pnlMainContent.Controls.Add(split);

            // --- SOL: GRID ---
            var dg = MakeGrid(0, 0);
            dg.Dock = DockStyle.Fill;
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn { Name = "Id",      HeaderText = "ID",          Width = 40 },
                new DataGridViewTextBoxColumn { Name = "Tarih",   HeaderText = "Talep Tarihi", Width = 140 },
                new DataGridViewTextBoxColumn { Name = "Ad",      HeaderText = "Ad Soyad",    Width = 160 },
                new DataGridViewTextBoxColumn { Name = "Email",   HeaderText = "E-posta",    Width = 160 },
                new DataGridViewTextBoxColumn { Name = "Oda",     HeaderText = "Oda",        Width = 50 },
                new DataGridViewTextBoxColumn { Name = "Giris",   HeaderText = "Giriş",      Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Cikis",   HeaderText = "Çıkış",      Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Fiyat",   HeaderText = "Tutar ₺",    Width = 90 },
                new DataGridViewTextBoxColumn { Name = "Durum",   HeaderText = "Durum",      Width = 110 },
                new DataGridViewButtonColumn { Name = "Pdf",      HeaderText = "Dekont",      Width = 75 }
            });
            split.Panel1.Controls.Add(dg);

            dg.CellContentClick += (s, e) => {
                if (e.RowIndex >= 0 && dg.Columns[e.ColumnIndex].Name == "Pdf") {
                    var pdfVal = dg.Rows[e.RowIndex].Cells["Pdf"].Value?.ToString();
                    if (pdfVal == "📄 PDF") {
                        var idVal = dg.Rows[e.RowIndex].Cells["Id"].Value;
                        if (idVal != null) {
                            int id = Convert.ToInt32(idVal);
                            var res = OnlineReservationHelper.GetAll(null).FirstOrDefault(x => x.Id == id);
                            if (res != null && !string.IsNullOrEmpty(res.PdfPath)) {
                                string url = $"http://localhost:5050{res.PdfPath}";
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
                                    FileName = url,
                                    UseShellExecute = true
                                });
                            }
                        }
                    }
                }
            };

            // --- SAĞ: QUICK VIEW ---
            var pnlQuick = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(28, 38, 58), Padding = new Padding(15) };
            pnlQuick.Paint += (s, e) => { e.Graphics.DrawRectangle(new Pen(Color.FromArgb(50, 60, 85), 1), 0, 0, pnlQuick.Width - 1, pnlQuick.Height - 1); };
            split.Panel2.Controls.Add(pnlQuick);

            Action<OnlineReservationHelper.OnlineReservation?> updateQuickView = (r) => {
                pnlQuick.Controls.Clear();
                if (r == null) {
                    pnlQuick.Controls.Add(new Label { Text = "BİR TALEP SEÇİN", ForeColor = Color.Gray, Font = new Font("Segoe UI", 10, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter });
                    return;
                }

                int dy = 15;
                // Avatar & Başlık
                var pb = new PictureBox { Size = new Size(65, 65), Location = new Point(15, dy), BackColor = Color.FromArgb(45, 55, 80), SizeMode = PictureBoxSizeMode.CenterImage };
                var bmp = new Bitmap(65, 65);
                using (var g = Graphics.FromImage(bmp)) {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.FromArgb(60, 70, 100));
                    g.DrawString(r.FullName[0].ToString().ToUpper(), new Font("Segoe UI", 26, FontStyle.Bold), Brushes.White, new Rectangle(0,0,65,65), new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center});
                }
                pb.Image = bmp; pnlQuick.Controls.Add(pb);

                var lblName = new Label { Text = r.FullName, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.White, Location = new Point(90, dy + 10), AutoSize = true };
                var lblId = new Label { Text = "TALEP ID: " + r.Id, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(120, 140, 180), Location = new Point(90, dy + 35), AutoSize = true };
                pnlQuick.Controls.Add(lblName); pnlQuick.Controls.Add(lblId);
                dy += 90;

                // Bölüm Çizgisi ve Başlık Yardımcısı
                Action<string, string, string> addSec = (title, label1, val1) => {
                    var lT = new Label { Text = title, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(100, 120, 160), Location = new Point(15, dy), AutoSize = true };
                    pnlQuick.Controls.Add(lT); dy += 22;
                    var l1 = new Label { Text = label1 + ":", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5F), Location = new Point(15, dy), AutoSize = true };
                    var v1 = new Label { Text = val1, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Location = new Point(85, dy), AutoSize = true };
                    pnlQuick.Controls.Add(l1); pnlQuick.Controls.Add(v1); dy += 22;
                };

                addSec("İletişim Bilgileri", "Email", r.Email);
                var lTel = new Label { Text = "Telefon:", ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5F), Location = new Point(15, dy), AutoSize = true };
                var vTel = new Label { Text = r.Phone ?? "-", ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Location = new Point(85, dy), AutoSize = true };
                pnlQuick.Controls.Add(lTel); pnlQuick.Controls.Add(vTel); dy += 35;

                addSec("Rezervasyon Detayları", "Oda No", $"{r.RoomNumber} ({r.RoomTypeName})");
                var lDat = new Label { Text = $"{r.CheckInDate:dd.MM.yyyy} — {r.CheckOutDate:dd.MM.yyyy} ({r.NightCount} Gece)", ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), Location = new Point(15, dy), AutoSize = true };
                pnlQuick.Controls.Add(lDat); dy += 40;

                var lFin = new Label { Text = "Finansal Bilgiler", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(100, 120, 160), Location = new Point(15, dy), AutoSize = true };
                pnlQuick.Controls.Add(lFin); dy += 22;
                var vPrc = new Label { Text = $"Toplam Tutar: ₺{r.TotalPrice:N0}", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(210, 180, 80), Location = new Point(15, dy), AutoSize = true };
                pnlQuick.Controls.Add(vPrc); dy += 45;

                var lNot = new Label { Text = "Notlar / Özel İstekler", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(100, 120, 160), Location = new Point(15, dy), AutoSize = true };
                pnlQuick.Controls.Add(lNot); dy += 22;
                var vNot = new Label { Text = string.IsNullOrEmpty(r.Notes) ? "Özel istek belirtilmemiş." : r.Notes, ForeColor = Color.FromArgb(160, 170, 190), Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), Location = new Point(15, dy), Size = new Size(pnlQuick.Width - 30, 45), AutoSize = false };
                pnlQuick.Controls.Add(vNot); dy += 50;

                // PDF Dekont Butonu (Kredi Kartı ile ödendiyse)
                if (!string.IsNullOrEmpty(r.PdfPath)) {
                    var btnPdf = MakeBtn("📄 Sistem Dekontunu Görüntüle", Color.FromArgb(180, 130, 20), 15, dy);
                    btnPdf.Size = new Size(pnlQuick.Width - 30, 35);
                    btnPdf.Click += (s, e) => {
                        string url = $"http://localhost:5050{r.PdfPath}";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
                    };
                    pnlQuick.Controls.Add(btnPdf);
                    dy += 40;
                }

                // Müşteri Yüklediği Dekont Butonu (Havale/EFT ile ödendiyse)
                if (!string.IsNullOrEmpty(r.ReceiptPath)) {
                    var btnReceipt = MakeBtn("🖼️ Müşteri Dekontunu Görüntüle", Color.FromArgb(100, 80, 160), 15, dy);
                    btnReceipt.Size = new Size(pnlQuick.Width - 30, 35);
                    btnReceipt.Click += (s, e) => {
                        string url = $"http://localhost:5050{r.ReceiptPath}";
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = url, UseShellExecute = true });
                    };
                    pnlQuick.Controls.Add(btnReceipt);
                    dy += 40;
                }

                // Alt Butonlar
                var pnlBtm = new Panel { Dock = DockStyle.Bottom, Height = 100, BackColor = Color.Transparent };
                var btnApp = MakeBtn("Onayla", Color.FromArgb(40, 120, 80), 5, 5); btnApp.Size = new Size(85, 38);
                var btnRej = MakeBtn("Reddet", Color.FromArgb(160, 40, 40), 95, 5); btnRej.Size = new Size(85, 38);
                var btnDtl = MakeBtn("Detay", Color.FromArgb(40, 80, 160), 185, 5); btnDtl.Size = new Size(85, 38);
                var btnIn = MakeBtn("ODAYA GİRİŞ YAP", Color.FromArgb(0, 100, 120), 5, 50); btnIn.Size = new Size(265, 38);
                
                btnApp.Enabled = r.Status == "Bekliyor";
                btnRej.Enabled = r.Status == "Bekliyor";
                btnIn.Enabled = r.Status == "Onaylandi";

                btnApp.Click += (s, e) => {
                    if (MessageBox.Show($"{r.FullName} talebini onaylıyor musunuz?", "Talep Onayı", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
                        var (suc, msg) = OnlineReservationHelper.Approve(r);
                        if (suc) { MessageBox.Show(msg); ShowOnlineReservations(); } else MessageBox.Show(msg, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                btnRej.Click += (s, e) => {
                    using (var frm = new Form()) {
                        frm.Text = "Rezervasyonu Reddet";
                        frm.Size = new Size(400, 320);
                        frm.StartPosition = FormStartPosition.CenterParent;
                        frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                        frm.MaximizeBox = false;
                        frm.MinimizeBox = false;
                        frm.BackColor = Color.FromArgb(35, 45, 65);
                        frm.ForeColor = Color.White;
                        frm.Font = new Font("Segoe UI", 9F);

                        var lblReason = new Label { Text = "Red Sebebi (Kısa):", Location = new Point(20, 20), AutoSize = true };
                        var txtReason = new TextBox { Location = new Point(20, 45), Size = new Size(340, 25), BackColor = Color.FromArgb(25, 35, 55), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                        txtReason.Text = "Kapasite yetersizliği";

                        var lblMsg = new Label { Text = "Müşteriye Gönderilecek Mesaj:", Location = new Point(20, 85), AutoSize = true };
                        var txtMsg = new TextBox { Location = new Point(20, 110), Size = new Size(340, 100), Multiline = true, BackColor = Color.FromArgb(25, 35, 55), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
                        txtMsg.Text = "Üzgünüz, seçtiğiniz tarihlerde otelimiz tam kapasite ile çalışmaktadır. Başka bir tarihte sizi ağırlamaktan mutluluk duyarız.";

                        var btnOk = new Button { Text = "Reddet", DialogResult = DialogResult.OK, Location = new Point(160, 230), Size = new Size(100, 35), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(160, 40, 40), Cursor = Cursors.Hand };
                        btnOk.FlatAppearance.BorderSize = 0;
                        var btnCancel = new Button { Text = "İptal", DialogResult = DialogResult.Cancel, Location = new Point(270, 230), Size = new Size(90, 35), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                        btnCancel.FlatAppearance.BorderSize = 0;

                        frm.Controls.AddRange(new Control[] { lblReason, txtReason, lblMsg, txtMsg, btnOk, btnCancel });
                        frm.AcceptButton = btnOk;
                        frm.CancelButton = btnCancel;

                        if (frm.ShowDialog() == DialogResult.OK) {
                            var (suc, msg) = OnlineReservationHelper.Reject(r.Id, txtReason.Text, txtMsg.Text);
                            if (suc) { 
                                MessageBox.Show(msg, "İşlem Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                                ShowOnlineReservations(); 
                            } else {
                                MessageBox.Show(msg, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };
                btnIn.Click += (s, e) => {
                    var (suc, msg) = OnlineReservationHelper.CheckIn(r.Id);
                    if (suc) { MessageBox.Show(msg, "Giriş Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information); ShowOnlineReservations(); }
                };
                btnDtl.Click += (s, e) => {
                    string dStr = $"Talep Zamanı: {r.CreatedAt:dd.MM.yyyy HH:mm}\nTC: {r.TcNo ?? "-"}\nUyruk: {r.Nationality}\nKişi: {r.Adults} Yetş. {r.Children} Çocuk";
                    MessageBox.Show(dStr, "Rezervasyon Detayları", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                pnlBtm.Controls.AddRange(new Control[] { btnApp, btnRej, btnDtl, btnIn });
                pnlQuick.Controls.Add(pnlBtm);
            };

            List<OnlineReservationHelper.OnlineReservation> list = new();
            Action<string?> load = (f) => {
                dg.Rows.Clear();
                list = OnlineReservationHelper.GetAll(f);
                foreach (var r in list) {
                    int idx = dg.Rows.Add(r.Id, r.CreatedAt.ToString("dd.MM HH:mm"), r.FullName, r.Email, r.RoomNumber, r.CheckInDate.ToString("dd.MM"), r.CheckOutDate.ToString("dd.MM"), $"₺{r.TotalPrice:N0}", r.Status.ToUpper());
                    
                    var cellStatus = dg.Rows[idx].Cells["Durum"];
                    cellStatus.Style.BackColor = r.Status switch { "Bekliyor" => Color.FromArgb(180, 140, 20), "Onaylandi" => Color.FromArgb(40, 120, 80), _ => Color.FromArgb(120, 40, 40) };
                    cellStatus.Style.ForeColor = Color.White;

                    var cellPdf = dg.Rows[idx].Cells["Pdf"];
                    if (!string.IsNullOrEmpty(r.PdfPath)) {
                        cellPdf.Value = "📄 PDF";
                        cellPdf.Style.BackColor = Color.FromArgb(40, 80, 120);
                        cellPdf.Style.ForeColor = Color.White;
                    } else {
                        cellPdf.Value = "-";
                    }
                }
                updateQuickView(null);
            };

            btnAll.Click += (s, e) => load(null);
            btnPending.Click += (s, e) => load("Bekliyor");
            btnApproved.Click += (s, e) => load("Onaylandi");
            btnRejected.Click += (s, e) => load("Reddedildi");
            btnPaid.Click += (s, e) => {
                dg.Rows.Clear();
                var filtered = OnlineReservationHelper.GetAll(null).Where(x => x.IsPaid && (x.Status == "Bekliyor" || x.Status == "Onaylandi")).ToList();
                foreach (var r in filtered) {
                    int idx = dg.Rows.Add(r.Id, r.CreatedAt.ToString("dd.MM HH:mm"), r.FullName, r.Email, r.RoomNumber, r.CheckInDate.ToString("dd.MM"), r.CheckOutDate.ToString("dd.MM"), $"₺{r.TotalPrice:N0}", r.Status.ToUpper());
                    dg.Rows[idx].Cells["Durum"].Style.BackColor = Color.FromArgb(100, 80, 160);
                    dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.White;

                    var cellPdf = dg.Rows[idx].Cells["Pdf"];
                    if (!string.IsNullOrEmpty(r.PdfPath)) {
                        cellPdf.Value = "📄 PDF";
                    } else {
                        cellPdf.Value = "-";
                    }
                }
                updateQuickView(null);
            };
            btnRefresh.Click += (s, e) => ShowOnlineReservations();
            btnManual.Click += (s, e) => {
                // Simple manual online reservation form!
                using var frmManual = new Form {
                    Text = "Manuel Online Rezervasyon Ekle",
                    Size = new Size(500, 500),
                    StartPosition = FormStartPosition.CenterParent,
                    BackColor = Color.FromArgb(20, 30, 50),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10F)
                };

                var lblFullName = new Label { Text = "Ad Soyad:", Location = new Point(20, 20), AutoSize = true };
                var txtFullName = new TextBox { Location = new Point(150, 20), Size = new Size(320, 30), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var lblEmail = new Label { Text = "E-posta:", Location = new Point(20, 60), AutoSize = true };
                var txtEmail = new TextBox { Location = new Point(150, 60), Size = new Size(320, 30), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var lblPhone = new Label { Text = "Telefon:", Location = new Point(20, 100), AutoSize = true };
                var txtPhone = new TextBox { Location = new Point(150, 100), Size = new Size(320, 30), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

                var lblRoom = new Label { Text = "Oda Seç:", Location = new Point(20, 140), AutoSize = true };
                var cmbRoom = new ComboBox { Location = new Point(150, 140), Size = new Size(320, 30), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, DropDownStyle = ComboBoxStyle.DropDownList };
                // Get available rooms to fill the combo!
                using (var conn = DatabaseHelper.GetConnection()) {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT r.id, r.room_number, rt.name as room_type_name FROM rooms r LEFT JOIN room_types rt ON r.room_type_id = rt.id ORDER BY r.room_number", conn))
                    using (var rdr = cmd.ExecuteReader()) {
                        while (rdr.Read()) {
                            string roomType = rdr.IsDBNull(2) ? "Unknown" : rdr.GetString(2);
                            string roomNum = rdr.GetString(1);
                            cmbRoom.Items.Add(new { 
                                Id = rdr.GetInt32(0), 
                                RoomNumber = roomNum, 
                                RoomType = roomType,
                                DisplayText = $"{roomNum} - {roomType}"
                            });
                        }
                    }
                }
                cmbRoom.DisplayMember = "DisplayText"; // We'll create a DisplayText property!
                cmbRoom.ValueMember = "Id";

                var lblCheckIn = new Label { Text = "Giriş Tarihi:", Location = new Point(20, 180), AutoSize = true };
                var dtpCheckIn = new DateTimePicker { Location = new Point(150, 180), Size = new Size(320, 30), Format = DateTimePickerFormat.Short, MinDate = DateTime.Today };

                var lblCheckOut = new Label { Text = "Çıkış Tarihi:", Location = new Point(20, 220), AutoSize = true };
                var dtpCheckOut = new DateTimePicker { Location = new Point(150, 220), Size = new Size(320, 30), Format = DateTimePickerFormat.Short, MinDate = DateTime.Today.AddDays(1) };

                var lblAdults = new Label { Text = "Yetişkin Sayısı:", Location = new Point(20, 260), AutoSize = true };
                var numAdults = new NumericUpDown { Location = new Point(150, 260), Size = new Size(320, 30), Minimum = 1, Maximum = 10, Value = 2 };

                var lblChildren = new Label { Text = "Çocuk Sayısı:", Location = new Point(20, 300), AutoSize = true };
                var numChildren = new NumericUpDown { Location = new Point(150, 300), Size = new Size(320, 30), Minimum = 0, Maximum = 10 };

                var lblNotes = new Label { Text = "Notlar:", Location = new Point(20, 340), AutoSize = true };
                var txtNotes = new TextBox { Location = new Point(150, 340), Size = new Size(320, 60), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Multiline = true };

                var btnSave = new Button { Text = "Kaydet", Location = new Point(280, 420), Size = new Size(120, 35), BackColor = Color.FromArgb(40, 120, 80), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand };
                var btnCancel = new Button { Text = "İptal", Location = new Point(410, 420), Size = new Size(120, 35), BackColor = Color.FromArgb(160, 40, 40), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };

                btnSave.Click += (s2, e2) => {
                    if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtEmail.Text) || cmbRoom.SelectedItem == null) {
                        MessageBox.Show("Lütfen zorunlu alanları doldurun!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try {
                        // Get selected room's price
                        var selectedRoom = cmbRoom.SelectedItem;
                        int roomId = (int)selectedRoom.GetType().GetProperty("Id").GetValue(selectedRoom);
                        string roomNumber = selectedRoom.GetType().GetProperty("RoomNumber").GetValue(selectedRoom).ToString();
                        string roomTypeName = selectedRoom.GetType().GetProperty("RoomType").GetValue(selectedRoom).ToString();

                        decimal pricePerNight = 0;
                        using (var conn = DatabaseHelper.GetConnection()) {
                            conn.Open();
                            using (var cmdPrice = new MySqlCommand("SELECT price_per_night FROM rooms WHERE id = @id", conn)) {
                                cmdPrice.Parameters.AddWithValue("@id", roomId);
                                pricePerNight = Convert.ToDecimal(cmdPrice.ExecuteScalar());
                            }

                            int nights = (dtpCheckOut.Value - dtpCheckIn.Value).Days;
                            decimal total = pricePerNight * nights;
                            string resCode = "AFM" + new Random().Next(100000, 999999).ToString();

                            // Insert into online_reservations
                            string insertSql = @"INSERT INTO online_reservations 
                            (res_code, full_name, email, phone, room_id, room_number, room_type_name, check_in_date, check_out_date, adults, children, total_price, notes, status, is_paid, created_at)
                            VALUES (@resCode, @fullName, @email, @phone, @roomId, @roomNumber, @roomTypeName, @checkIn, @checkOut, @adults, @children, @total, @notes, 'Bekliyor', 0, NOW())";
                            using (var cmdInsert = new MySqlCommand(insertSql, conn)) {
                                cmdInsert.Parameters.AddWithValue("@resCode", resCode);
                                cmdInsert.Parameters.AddWithValue("@fullName", txtFullName.Text.Trim());
                                cmdInsert.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                                cmdInsert.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? (object)DBNull.Value : txtPhone.Text.Trim());
                                cmdInsert.Parameters.AddWithValue("@roomId", roomId);
                                cmdInsert.Parameters.AddWithValue("@roomNumber", roomNumber);
                                cmdInsert.Parameters.AddWithValue("@roomTypeName", roomTypeName);
                                cmdInsert.Parameters.AddWithValue("@checkIn", dtpCheckIn.Value.ToString("yyyy-MM-dd"));
                                cmdInsert.Parameters.AddWithValue("@checkOut", dtpCheckOut.Value.ToString("yyyy-MM-dd"));
                                cmdInsert.Parameters.AddWithValue("@adults", (int)numAdults.Value);
                                cmdInsert.Parameters.AddWithValue("@children", (int)numChildren.Value);
                                cmdInsert.Parameters.AddWithValue("@total", total);
                                cmdInsert.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(txtNotes.Text) ? (object)DBNull.Value : txtNotes.Text.Trim());
                                cmdInsert.ExecuteNonQuery();
                            }
                        }

                        MessageBox.Show("Manuel rezervasyon başarıyla eklendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        frmManual.DialogResult = DialogResult.OK;
                        frmManual.Close();
                        ShowOnlineReservations();
                    } catch (Exception ex) {
                        MessageBox.Show("Hata oluştu: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                frmManual.Controls.AddRange(new Control[] { lblFullName, txtFullName, lblEmail, txtEmail, lblPhone, txtPhone, lblRoom, cmbRoom, lblCheckIn, dtpCheckIn, lblCheckOut, dtpCheckOut, lblAdults, numAdults, lblChildren, numChildren, lblNotes, txtNotes, btnSave, btnCancel });

                if (frmManual.ShowDialog() == DialogResult.OK) {
                    // Already refreshed in btnSave click!
                }
            };
            
            txtSearch.TextChanged += (s, e) => {
                dg.Rows.Clear();
                var filt = list.Where(x => x.FullName.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase) || x.Email.Contains(txtSearch.Text, StringComparison.OrdinalIgnoreCase) || x.Id.ToString() == txtSearch.Text).ToList();
                foreach (var r in filt) {
                    int idx = dg.Rows.Add(r.Id, r.CreatedAt.ToString("dd.MM HH:mm"), r.FullName, r.Email, r.RoomNumber, r.CheckInDate.ToString("dd.MM"), r.CheckOutDate.ToString("dd.MM"), $"₺{r.TotalPrice:N0}", r.Status.ToUpper());
                    dg.Rows[idx].Cells["Durum"].Style.BackColor = r.Status switch { "Bekliyor" => Color.FromArgb(180, 140, 20), "Onaylandi" => Color.FromArgb(40, 120, 80), _ => Color.FromArgb(120, 40, 40) };
                    dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.White;

                    var cellPdf = dg.Rows[idx].Cells["Pdf"];
                    if (!string.IsNullOrEmpty(r.PdfPath)) {
                        cellPdf.Value = "📄 PDF";
                    } else {
                        cellPdf.Value = "-";
                    }
                }
            };

            dg.SelectionChanged += (s, e) => {
                if (dg.SelectedRows.Count > 0) {
                    int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    updateQuickView(list.FirstOrDefault(x => x.Id == id));
                }
            };

            load(null);
        }
    }
}
