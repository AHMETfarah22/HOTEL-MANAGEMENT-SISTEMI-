using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ORYS.Helpers;
using ORYS.Models;

namespace ORYS.Forms
{
    public partial class MainForm : Form
    {
        private Button? activeNav = null;
        private string Role => AuthHelper.CurrentUser?.Role ?? "Resepsiyonist";
        private bool IsAdmin => Role == "Admin";
        private bool IsResepsiyon => Role == "Resepsiyonist";
        private bool IsMuhasebe => Role == "Muhasebe";
        private readonly Color cBg = Color.FromArgb(12, 18, 35);
        private readonly Color cCard = Color.FromArgb(18, 25, 45);
        private readonly Color cGold = Color.FromArgb(218, 165, 32);
        private readonly Color cText = Color.FromArgb(200, 210, 230);
        private readonly Color cGreen = Color.FromArgb(34, 139, 34);
        private readonly Color cRed = Color.FromArgb(178, 34, 34);
        private readonly Color cYellow = Color.FromArgb(218, 165, 32);
        private readonly Color cBlue = Color.FromArgb(70, 130, 180);

        public MainForm()
        {
            InitializeComponent();
            this.Load += (s, e) => { SetupNav(); ShowDashboard(); };
            this.Resize += (s, e) => { if (activeNav?.Text.Contains("Dashboard") == true) ShowDashboard(); };
            pnlHeader.Paint += (s, e) => { using var p = new Pen(cGold, 2); e.Graphics.DrawLine(p, 0, pnlHeader.Height - 2, pnlHeader.Width, pnlHeader.Height - 2); };
        }

        private void SetupNav()
        {
            // Rol bazlı navbar görünürlüğü
            btnNavOdalar.Visible = !IsMuhasebe;           // Muhasebe oda yönetimi göremez
            btnNavRezervasyon.Visible = !IsMuhasebe;      // Muhasebe rezervasyon göremez
            btnNavPersonel.Visible = IsAdmin;             // Sadece Admin personel yönetir
            btnNavRaporlar.Visible = IsAdmin || IsMuhasebe; // Resepsiyonist rapor göremez

            var allBtns = new[] { btnNavDashboard, btnNavMisafirler, btnNavOdalar, btnNavRezervasyon, btnNavOdeme, btnNavPersonel, btnNavRaporlar };
            foreach (var b in allBtns)
            {
                if (!b.Visible) continue;
                b.Click += (s, e) => { SetActive(b); SwitchPanel(b.Text); };
                b.MouseEnter += (s, e) => { if (b != activeNav) b.ForeColor = cGold; };
                b.MouseLeave += (s, e) => { if (b != activeNav) b.ForeColor = Color.FromArgb(180, 190, 210); };
            }

            // Çıkış butonu eventleri
            btnNavCikis.Click += (s, e) => {
                if (MessageBox.Show("Oturumu kapatmak istediğinize emin misiniz?", "Çıkış İşlemi", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    this.Close();
            };
            btnNavCikis.MouseEnter += (s, e) => btnNavCikis.ForeColor = cRed;
            btnNavCikis.MouseLeave += (s, e) => btnNavCikis.ForeColor = Color.FromArgb(180, 190, 210);
            SetActive(btnNavDashboard);

            // Başlıkta rol göster
            lblLogo.Text = "🏨 AFM GRAND";
        }

        private void SetActive(Button b)
        {
            if (activeNav != null) { activeNav.BackColor = Color.Transparent; activeNav.ForeColor = Color.FromArgb(180, 190, 210); activeNav.Font = new Font("Segoe UI", 9.5F); }
            activeNav = b; b.BackColor = Color.FromArgb(40, 55, 85); b.ForeColor = Color.White; b.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        }

        private void SwitchPanel(string txt)
        {
            if (txt.Contains("Dashboard")) ShowDashboard();
            else if (txt.Contains("Misafir")) ShowGuests();
            else if (txt.Contains("Oda")) ShowRooms();
            else if (txt.Contains("Rezerv")) ShowReservations();
            else if (txt.Contains("Ödeme") || txt.Contains("deme")) ShowPayments();
            else if (txt.Contains("Personel")) ShowPersonel();
            else if (txt.Contains("Rapor")) ShowReports();
        }

        private void ClearContent() { pnlMainContent.Controls.Clear(); }
        private Label MakeTitle(string t) => new Label { Text = t, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(20, 15), BackColor = Color.Transparent };
        private DataGridView MakeGrid(int y, int h) {
            var g = new DataGridView { Location = new Point(20, y), BackgroundColor = cCard, BorderStyle = BorderStyle.None, GridColor = Color.FromArgb(40, 55, 80),
                EnableHeadersVisualStyles = false, RowHeadersVisible = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ScrollBars = ScrollBars.Both };
            g.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(30, 42, 68), ForeColor = cGold, Font = new Font("Segoe UI", 9F, FontStyle.Bold), SelectionBackColor = Color.FromArgb(30, 42, 68) };
            g.ColumnHeadersHeight = 35; g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(22, 32, 56), ForeColor = Color.White, SelectionBackColor = Color.FromArgb(35, 50, 80), SelectionForeColor = Color.White, Font = new Font("Segoe UI", 9F) };
            g.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(18, 28, 50), ForeColor = Color.White, SelectionBackColor = Color.FromArgb(35, 50, 80) };
            g.RowTemplate.Height = 32; return g;
        }
        private Button MakeBtn(string t, Color bg, int x, int y) => new Button { Text = t, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Size = new Size(150, 38), Location = new Point(x, y),
            FlatStyle = FlatStyle.Flat, BackColor = bg, ForeColor = Color.White, Cursor = Cursors.Hand, FlatAppearance = { BorderSize = 0 } };
        private Panel MakeStatCard(string icon, string title, string val, int x) {
            var p = new Panel { Size = new Size(220, 85), Location = new Point(x, 55), BackColor = cCard };
            p.Controls.Add(new Label { Text = icon, Font = new Font("Segoe UI Emoji", 22F), Size = new Size(50, 50), Location = new Point(10, 18), TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
            p.Controls.Add(new Label { Text = val, Font = new Font("Segoe UI", 20F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(65, 12), BackColor = Color.Transparent, Name = "val" });
            p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 9F), ForeColor = cText, AutoSize = true, Location = new Point(67, 50), BackColor = Color.Transparent });
            p.Paint += (s, e) => { using var pen = new Pen(cGold, 2); e.Graphics.DrawLine(pen, 0, 0, p.Width, 0); };
            return p;
        }

        // ========== DASHBOARD ==========
        private void ShowDashboard()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📊 Dashboard"));
            int w = pnlMainContent.ClientSize.Width - 40;
            int cardW = (w - 30) / 4;

            // === İSTATİSTİK KARTLARI ===
            int total = 0, occ = 0, avail = 0; decimal rev = 0;
            try
            {
                total = RoomHelper.GetRoomCount();
                occ = RoomHelper.GetRoomCount("Occupied");
                avail = RoomHelper.GetRoomCount("Available");
            }
            catch { }
            try { rev = ReservationHelper.GetTodayRevenue(); } catch { }

            pnlMainContent.Controls.Add(MakeStatCard("🛏️", "Toplam Oda", total.ToString(), 20));
            pnlMainContent.Controls.Add(MakeStatCard("🛌", "Dolu Oda", occ.ToString(), 20 + cardW + 10));
            pnlMainContent.Controls.Add(MakeStatCard("🔑", "Müsait Oda", avail.ToString(), 20 + (cardW + 10) * 2));
            pnlMainContent.Controls.Add(MakeStatCard("💰", "Günlük Gelir", $"₺{rev:N0}", 20 + (cardW + 10) * 3));

            // === ODA DURUMU GRID ===
            int roomGridBottom = 420; // varsayılan
            try
            {
                var rooms = RoomHelper.GetAllRooms();
                int perRow = 10;
                int rowCount = (int)Math.Ceiling(rooms.Count / (double)perRow);
                int cardW2 = (w - 30) / perRow - 3;
                int gridH = 45 + rowCount * 65 + 10;
                var pnlRooms = new Panel { Location = new Point(20, 150), Size = new Size(w, gridH), BackColor = cCard };
                pnlRooms.Controls.Add(new Label { Text = "Oda Durumu Görünümü", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 10), BackColor = Color.Transparent });
                int rx = 10, ry = 42, ri = 0;
                foreach (var room in rooms)
                {
                    Color rc = room.Status switch { "Available" => cGreen, "Occupied" => cRed, "Reserved" => cYellow, "Maintenance" => cBlue, _ => Color.Gray };
                    string ic = room.Status switch { "Available" => "🟢", "Occupied" => "🔴", "Reserved" => "🟡", _ => "🔧" };
                    string displayStatus = room.Status switch { "Available" => "Müsait", "Occupied" => "Dolu", "Reserved" => "Rezerve", "Maintenance" => "Bakımda", _ => "Bilinmiyor" };
                    var card = new Panel { Size = new Size(cardW2, 55), Location = new Point(rx, ry), BackColor = rc, Cursor = Cursors.Hand };
                    card.Controls.Add(new Label { Text = $"{ic} {room.RoomNumber}", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
                    card.Controls.Add(new Label { Text = displayStatus, Font = new Font("Segoe UI", 8F, FontStyle.Bold), ForeColor = Color.FromArgb(240, 240, 240), Dock = DockStyle.Bottom, Height = 18, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent });
                    card.Click += (s, e) => { SetActive(btnNavOdalar); ShowRooms(); };
                    foreach (Control c in card.Controls) c.Click += (s, e) => { SetActive(btnNavOdalar); ShowRooms(); };
                    pnlRooms.Controls.Add(card);
                    rx += cardW2 + 5; ri++;
                    if (ri % perRow == 0) { rx = 10; ry += 60; }
                }
                pnlMainContent.Controls.Add(pnlRooms);
                roomGridBottom = 150 + gridH + 10;
            }
            catch (Exception ex)
            {
                pnlMainContent.Controls.Add(new Label { Text = $"⚠️ Oda verileri yüklenemedi: {ex.Message}", Font = new Font("Segoe UI", 10F), ForeColor = Color.FromArgb(248, 113, 113), AutoSize = true, Location = new Point(20, 150), BackColor = Color.Transparent });
                roomGridBottom = 190;
            }

            // === BUGÜN GİRİŞ LİSTESİ ===
            int halfW = (w - 10) / 2;
            var ciPanel = new Panel { Location = new Point(20, roomGridBottom), Size = new Size(halfW, 350), BackColor = cCard };
            ciPanel.Controls.Add(new Label { Text = "BU GÜNÜN GİRİŞ LİSTESİ", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGold, AutoSize = true, Location = new Point(10, 8), BackColor = Color.Transparent });
            
            var btnRefresh = new Button { Text = "🔄 Yenile", Location = new Point(halfW - 100, 5), Size = new Size(80, 25), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(30, 45, 80), ForeColor = Color.White, Font = new Font("Segoe UI", 8F) };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => ShowDashboard();
            ciPanel.Controls.Add(btnRefresh);

            try
            {
                var dgCI = MakeGrid(35, 305); dgCI.Size = new Size(halfW - 10, 305);
                dgCI.Columns.AddRange(new DataGridViewColumn[] { new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=50}, new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir Adı"}, new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Giriş/Çıkış",Width=160} });
                var btnColCI = new DataGridViewButtonColumn { Name = "Islem", HeaderText = "", Width = 85 };
                dgCI.Columns.Add(btnColCI);
                var checkIns = ReservationHelper.GetTodayCheckIns();
                foreach (var r in checkIns) {
                    int idx = dgCI.Rows.Add(r.RoomNumber, r.GuestName, $"{r.CheckInDate:dd.MM.yyyy} - {r.CheckOutDate:dd.MM.yyyy}");
                    dgCI.Rows[idx].Cells["Islem"].Value = r.Status == "GirisYapildi" ? "✅ Yapıldı" : "Giriş Yap";
                }
                if (checkIns.Count == 0) dgCI.Rows.Add("", "Bugün giriş yok", "", "");
                dgCI.CellClick += (s, e) => { 
                    if (e.ColumnIndex >= 0 && dgCI.Columns[e.ColumnIndex].Name == "Islem" && e.RowIndex >= 0 && e.RowIndex < checkIns.Count) { 
                        if (checkIns[e.RowIndex].Status != "GirisYapildi") {
                            ReservationHelper.CheckIn(checkIns[e.RowIndex].Id, checkIns[e.RowIndex].RoomId); 
                            ShowDashboard(); 
                        }
                    } 
                };
                ciPanel.Controls.Add(dgCI);
            }
            catch (Exception ex)
            {
                ciPanel.Controls.Add(new Label { Text = $"⚠️ {ex.Message}", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(248, 113, 113), AutoSize = true, Location = new Point(10, 40), BackColor = Color.Transparent });
            }
            pnlMainContent.Controls.Add(ciPanel);

            // === BUGÜN ÇIKIŞ LİSTESİ ===
            var coPanel = new Panel { Location = new Point(20 + halfW + 10, roomGridBottom), Size = new Size(halfW, 350), BackColor = cCard };
            coPanel.Controls.Add(new Label { Text = "BU GÜNÜN ÇIKIŞ LİSTESİ", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = cGold, AutoSize = true, Location = new Point(10, 8), BackColor = Color.Transparent });
            try
            {
                var dgCO = MakeGrid(35, 305); dgCO.Size = new Size(halfW - 10, 305);
                dgCO.Columns.AddRange(new DataGridViewColumn[] { new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=50}, new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir Adı"}, new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Giriş/Çıkış",Width=160} });
                var btnColCO = new DataGridViewButtonColumn { Name = "Islem", HeaderText = "", Width = 85 };
                dgCO.Columns.Add(btnColCO);
                var checkOuts = ReservationHelper.GetTodayCheckOuts();
                foreach (var r in checkOuts) {
                    int idx = dgCO.Rows.Add(r.RoomNumber, r.GuestName, $"{r.CheckInDate:dd.MM.yyyy} - {r.CheckOutDate:dd.MM.yyyy}");
                    dgCO.Rows[idx].Cells["Islem"].Value = r.Status == "CikisYapildi" ? "✅ Yapıldı" : "Çıkış Yap";
                }
                if (checkOuts.Count == 0) dgCO.Rows.Add("", "Bugün çıkış yok", "", "");
                dgCO.CellClick += (s, e) => { 
                    if (e.ColumnIndex >= 0 && dgCO.Columns[e.ColumnIndex].Name == "Islem" && e.RowIndex >= 0 && e.RowIndex < checkOuts.Count) { 
                        if (checkOuts[e.RowIndex].Status != "CikisYapildi") {
                            // Redirect to payments
                            SetActive(btnNavOdeme);
                            ShowPayments();
                            if (ShowPaymentDialog(checkOuts[e.RowIndex].Id)) ShowDashboard(); 
                            else ShowDashboard(); // Her durumda listeyi yenile
                        }
                    } 
                };
                coPanel.Controls.Add(dgCO);
            }
            catch (Exception ex)
            {
                coPanel.Controls.Add(new Label { Text = $"⚠️ {ex.Message}", Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(248, 113, 113), AutoSize = true, Location = new Point(10, 40), BackColor = Color.Transparent });
            }
            pnlMainContent.Controls.Add(coPanel);
        }

        // ========== MİSAFİRLER ==========
        private void ShowGuests()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle(IsMuhasebe ? "👥 Misafir Listesi (Salt Okunur)" : "👥 Misafir Yönetimi"));
            var txtSearch = new TextBox { Size = new Size(300, 30), Location = new Point(20, 55), Font = new Font("Segoe UI", 11F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "🔍 Misafir ara (ad, TC, telefon)..." };
            pnlMainContent.Controls.Add(txtSearch);
            // Muhasebe ekle/sil göremez
            if (!IsMuhasebe) { var btnAdd = MakeBtn("➕ Yeni Misafir", cGreen, 340, 55); pnlMainContent.Controls.Add(btnAdd);
                btnAdd.Click += (s, e) => { if (ShowGuestDialog(null)) loadGuests_ref?.Invoke(); }; }
            var dg = MakeGrid(100, 500); dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 130);
            if (IsMuhasebe) dg.ReadOnly = true;
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=50},
                new DataGridViewTextBoxColumn{Name="Ad",HeaderText="Ad Soyad"},
                new DataGridViewTextBoxColumn{Name="TC",HeaderText="TC No",Width=110},
                new DataGridViewTextBoxColumn{Name="Pasaport",HeaderText="Pasaport No",Width=110},
                new DataGridViewTextBoxColumn{Name="Tel",HeaderText="Telefon",Width=120},
                new DataGridViewTextBoxColumn{Name="Email",HeaderText="Email",Width=150},
                new DataGridViewTextBoxColumn{Name="Ulke",HeaderText="Uyruk",Width=80},
            });
            Action loadGuests = () => { try { dg.Rows.Clear(); var guests = string.IsNullOrWhiteSpace(txtSearch.Text) ? GuestHelper.GetAllGuests() : GuestHelper.SearchGuests(txtSearch.Text);
                foreach (var g2 in guests) dg.Rows.Add(g2.Id, g2.FullName, g2.TcNo ?? "", g2.PassportNo ?? "", g2.Phone ?? "", g2.Email ?? "", g2.Nationality); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            loadGuests_ref = loadGuests;
            loadGuests();
            txtSearch.TextChanged += (s, e) => loadGuests();
            // Admin ve Resepsiyonist düzenleyebilir
            if (!IsMuhasebe) dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) { int id = Convert.ToInt32(dg.Rows[e.RowIndex].Cells["Id"].Value); var guest = GuestHelper.GetGuestById(id); if (guest != null && ShowGuestDialog(guest)) loadGuests(); } };
            // Sadece Admin silebilir
            if (IsAdmin) { var btnDel = MakeBtn("🗑️ Sil", cRed, 510, 55); pnlMainContent.Controls.Add(btnDel);
                btnDel.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    if (MessageBox.Show("Bu misafiri silmek istiyor musunuz?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes) { try { GuestHelper.DeleteGuest(id); loadGuests(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } } } }; }
            pnlMainContent.Controls.Add(dg);
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
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle(IsAdmin ? "🛏️ Oda Yönetimi" : "🛏️ Oda Durumu"));
            // Sadece Admin oda ekleyebilir
            if (IsAdmin) { var btnAdd = MakeBtn("➕ Yeni Oda", cGreen, 20, 55); pnlMainContent.Controls.Add(btnAdd);
                btnAdd.Click += (s, e) => { if (ShowRoomDialog(null)) loadRooms_ref?.Invoke(); }; }
            var dg = MakeGrid(100, 500); dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 130);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=40},
                new DataGridViewTextBoxColumn{Name="No",HeaderText="Oda No",Width=70},
                new DataGridViewTextBoxColumn{Name="Tip",HeaderText="Tip",Width=100},
                new DataGridViewTextBoxColumn{Name="Kat",HeaderText="Kat",Width=50},
                new DataGridViewTextBoxColumn{Name="Kap",HeaderText="Kapasite",Width=70},
                new DataGridViewTextBoxColumn{Name="Fiyat",HeaderText="Gecelik ₺",Width=100},
                new DataGridViewTextBoxColumn{Name="Durum",HeaderText="Durum",Width=100},
            });
            Action loadRooms = () => { try { dg.Rows.Clear(); var rooms = RoomHelper.GetAllRooms();
                foreach (var r in rooms) { 
                    string sDisp = r.Status switch { "Available" => "Müsait", "Occupied" => "Dolu", "Reserved" => "Rezerve", "Maintenance" => "Bakımda", _ => "Bilinmiyor" };
                    int idx = dg.Rows.Add(r.Id, r.RoomNumber, r.RoomTypeName, r.Floor, r.Capacity, $"₺{r.PricePerNight:N0}", sDisp);
                    Color sc = r.Status switch { "Available" => cGreen, "Occupied" => cRed, "Reserved" => cYellow, "Maintenance" => cBlue, _ => Color.Gray };
                    dg.Rows[idx].Cells["Durum"].Style.BackColor = sc; dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.White; } } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            loadRooms_ref = loadRooms;
            loadRooms();
            // Sadece Admin düzenleyebilir
            if (IsAdmin) dg.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) { int id = Convert.ToInt32(dg.Rows[e.RowIndex].Cells["Id"].Value);
                var rooms = RoomHelper.GetAllRooms(); var room = rooms.Find(r => r.Id == id); if (room != null && ShowRoomDialog(room)) loadRooms(); } };
            // Admin ve Resepsiyonist durum değiştirebilir
            var btnStatus = MakeBtn("🔄 Durum Değiştir", cBlue, IsAdmin ? 190 : 20, 55); pnlMainContent.Controls.Add(btnStatus);
            btnStatus.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                var sf = new Form { Text = "Durum Değiştir", Size = new Size(300, 200), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog };
                var cmb = new ComboBox { Location = new Point(20, 30), Size = new Size(240, 30), Font = new Font("Segoe UI", 11F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
                var secenekler = new Dictionary<string, string> { {"Müsait", "Available"}, {"Dolu", "Occupied"}, {"Rezerve", "Reserved"}, {"Bakımda", "Maintenance"} };
                foreach (var kb in secenekler.Keys) cmb.Items.Add(kb);
                cmb.SelectedIndex = 0;
                var btnOk = MakeBtn("✅ Uygula", cGreen, 20, 80); sf.Controls.AddRange(new Control[] { cmb, btnOk });
                btnOk.Click += (s2, e2) => { try { string targetStatus = secenekler[cmb.SelectedItem!.ToString()!]; RoomHelper.UpdateRoomStatus(id, targetStatus); sf.Close(); loadRooms(); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
                sf.ShowDialog(); } };
            // Sadece Admin silebilir
            if (IsAdmin) { var btnDel = MakeBtn("🗑️ Sil", cRed, 360, 55); pnlMainContent.Controls.Add(btnDel);
                btnDel.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    if (MessageBox.Show("Bu odayı silmek istiyor musunuz?", "Sil", MessageBoxButtons.YesNo) == DialogResult.Yes) { try { RoomHelper.DeleteRoom(id); loadRooms(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } } } }; }
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
            var btnNew = MakeBtn("➕ Yeni Rez.", cGreen, 20, 55); pnlMainContent.Controls.Add(btnNew);
            var btnEdit = MakeBtn("✏️ Düzenle", cBlue, 180, 55); pnlMainContent.Controls.Add(btnEdit);
            var btnCI = MakeBtn("🏨 Giriş Yap", Color.FromArgb(0, 120, 80), 340, 55); pnlMainContent.Controls.Add(btnCI);
            var btnCO = MakeBtn("🚪 Çıkış Yap", Color.FromArgb(160, 80, 0), 500, 55); pnlMainContent.Controls.Add(btnCO);
            var btnCancel = MakeBtn("❌ İptal Et", cRed, 660, 55); pnlMainContent.Controls.Add(btnCancel);
            var dg = MakeGrid(100, 500); dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 130);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=40},
                new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir"},
                new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=60},
                new DataGridViewTextBoxColumn{Name="Giris",HeaderText="Giriş",Width=90},
                new DataGridViewTextBoxColumn{Name="Cikis",HeaderText="Çıkış",Width=90},
                new DataGridViewTextBoxColumn{Name="Gece",HeaderText="Gece",Width=50},
                new DataGridViewTextBoxColumn{Name="Fiyat",HeaderText="Toplam ₺",Width=80},
                new DataGridViewTextBoxColumn{Name="Kalan",HeaderText="Kalan ₺",Width=80},
                new DataGridViewTextBoxColumn{Name="Durum",HeaderText="Durum",Width=100},
            });
            var dtStart = new DateTimePicker { Location = new Point(pnlMainContent.ClientSize.Width - 380, 58), Size = new Size(110, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var dtEnd = new DateTimePicker { Location = new Point(pnlMainContent.ClientSize.Width - 260, 58), Size = new Size(110, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(2) };
            var btnFilter = MakeBtn("🔍 Filtrele", cCard, pnlMainContent.ClientSize.Width - 140, 55); 
            btnFilter.Size = new Size(100, 38);
            System.Windows.Forms.Label lblTire = new System.Windows.Forms.Label { Text = "-", BackColor = Color.Transparent, ForeColor = Color.White, Location = new Point(pnlMainContent.ClientSize.Width - 270, 60), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            pnlMainContent.Controls.AddRange(new Control[] { dtStart, dtEnd, btnFilter, lblTire });

            Action loadRes = () => { try { dg.Rows.Clear(); 
                var list = ReservationHelper.GetAllReservations();
                
                // Filter by dates
                DateTime sDate = dtStart.Value.Date;
                DateTime eDate = dtEnd.Value.Date.AddDays(1).AddTicks(-1);
                list = list.FindAll(r => r.CheckInDate >= sDate && r.CheckInDate <= eDate);

                foreach (var r in list) {
                    decimal odenen = PaymentHelper.GetTotalPaidForReservation(r.Id);
                    decimal kalan = r.TotalPrice - odenen;
                    int idx = dg.Rows.Add(r.Id, r.GuestName, r.RoomNumber, r.CheckInDate.ToString("dd.MM.yyyy"), r.CheckOutDate.ToString("dd.MM.yyyy"), r.NightCount, $"₺{r.TotalPrice:N0}", $"₺{kalan:N0}", r.StatusDisplay);
                    Color sc = r.Status switch { "Bekliyor" => cYellow, "Onaylandi" => cGreen, "GirisYapildi" => cBlue, "CikisYapildi" => Color.Gray, "Iptal" => cRed, _ => Color.Gray };
                    dg.Rows[idx].Cells["Durum"].Style.BackColor = sc; dg.Rows[idx].Cells["Durum"].Style.ForeColor = Color.White;
                    if (kalan > 0 && r.Status != "Iptal") dg.Rows[idx].Cells["Kalan"].Style.ForeColor = Color.FromArgb(248, 113, 113); // red
                    else if (kalan <= 0 && r.Status != "Iptal") dg.Rows[idx].Cells["Kalan"].Style.ForeColor = Color.FromArgb(74, 222, 128); // green
                } } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            loadRes();
            btnFilter.Click += (s, e) => loadRes();
            btnNew.Click += (s, e) => { if (ShowReservationDialog(null)) loadRes(); };
            btnEdit.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value); var all = ReservationHelper.GetAllReservations(); var res = all.Find(x => x.Id == id); if (res != null) { if (ShowReservationDialog(res)) loadRes(); } } };
            btnCI.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value); string st = dg.SelectedRows[0].Cells["Durum"].Value?.ToString() ?? "";
                if (!st.Contains("Onay") && !st.Contains("Bekl")) { MessageBox.Show("Sadece Bekliyor/Onaylandı durumunda giriş yapılabilir!"); return; }
                var all = ReservationHelper.GetAllReservations(); var res = all.Find(x => x.Id == id);
                if (res != null) { ReservationHelper.CheckIn(res.Id, res.RoomId); MessageBox.Show("✅ Giriş yapıldı!"); loadRes(); } } };
            btnCO.Click += (s, e) => { 
                if (dg.SelectedRows.Count > 0) { 
                    int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value); 
                    string st = dg.SelectedRows[0].Cells["Durum"].Value?.ToString() ?? "";
                    if (!st.Contains("Giriş")) { MessageBox.Show("Sadece Giriş Yapıldı durumunda çıkış yapılabilir!"); return; }
                    var all = ReservationHelper.GetAllReservations(); 
                    var res = all.Find(x => x.Id == id);
                    if (res != null) { 
                        // Switch to Payment view and show dialog
                        SetActive(btnNavOdeme);
                        ShowPayments(); // Ensure the payments panel is loaded
                        if (ShowPaymentDialog(res.Id)) ShowPayments(); // Reload after payment
                    } 
                } else { MessageBox.Show("Lütfen çıkış yapılacak rezervasyonu seçin!"); }
            };
            btnCancel.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                if (MessageBox.Show("Rezervasyonu iptal etmek istiyor musunuz?", "İptal", MessageBoxButtons.YesNo) == DialogResult.Yes)
                { try { ReservationHelper.UpdateReservationStatus(id, "Iptal"); loadRes(); } catch (Exception ex) { MessageBox.Show(ex.Message); } } } };
            
            var btnExtra = MakeBtn("💎 Ekstra Ekle", Color.FromArgb(120, 0, 160), 820, 55); pnlMainContent.Controls.Add(btnExtra);
            btnExtra.Click += (s, e) => { 
                if (dg.SelectedRows.Count > 0) { 
                    int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                    string st = dg.SelectedRows[0].Cells["Durum"].Value?.ToString() ?? "";
                    if (st.Contains("Çıkış") || st.Contains("İptal")) { MessageBox.Show("Çıkış yapmış veya iptal olmuş rezervasyona ekstra eklenemez!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                    var all = ReservationHelper.GetAllReservations(); var res = all.Find(x => x.Id == id); 
                    if (res != null) { if (ShowExtraChargeDialog(res)) loadRes(); }
                } else { MessageBox.Show("Lütfen rezervasyon seçin!"); }
            };
            pnlMainContent.Controls.Add(dg);
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
            var f = new Form { Text = currentRes == null ? "Yeni Rezervasyon" : "Rezervasyon Düzenle", Size = new Size(500, 520), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 15;
            f.Controls.Add(new Label { Text = "Misafir Seç *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbGuest = new ComboBox { Location = new Point(20, y), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            List<Guest> guests = new(); try { guests = GuestHelper.GetAllGuests(); } catch { }
            foreach (var g in guests) cmbGuest.Items.Add($"{g.Id} - {g.FullName}"); 
            if (cmbGuest.Items.Count > 0) cmbGuest.SelectedIndex = 0;
            if (currentRes != null && guests.Exists(g => g.Id == currentRes.GuestId)) cmbGuest.SelectedIndex = guests.FindIndex(g => g.Id == currentRes.GuestId);
            f.Controls.Add(cmbGuest); y += 38;
            f.Controls.Add(new Label { Text = "Giriş Tarihi *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var dtIn = new DateTimePicker { Location = new Point(20, y), Size = new Size(440, 28), Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Value = currentRes?.CheckInDate ?? DateTime.Today }; f.Controls.Add(dtIn); y += 38;
            f.Controls.Add(new Label { Text = "Çıkış Tarihi *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var dtOut = new DateTimePicker { Location = new Point(20, y), Size = new Size(440, 28), Font = new Font("Segoe UI", 10F), Format = DateTimePickerFormat.Short, Value = currentRes?.CheckOutDate ?? DateTime.Today.AddDays(3) }; f.Controls.Add(dtOut); y += 38;
            f.Controls.Add(new Label { Text = "Oda Seç *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbRoom = new ComboBox { Location = new Point(20, y), Size = new Size(440, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            List<Room> availRooms = new(); Action loadAvail = () => { cmbRoom.Items.Clear(); try { availRooms = RoomHelper.GetAvailableRooms(dtIn.Value, dtOut.Value, currentRes?.Id ?? 0); foreach (var r in availRooms) cmbRoom.Items.Add($"{r.RoomNumber} - {r.RoomTypeName} (₺{r.PricePerNight:N0}/gece)"); if (cmbRoom.Items.Count > 0) cmbRoom.SelectedIndex = 0; if (currentRes != null && availRooms.Exists(x => x.Id == currentRes.RoomId)) cmbRoom.SelectedIndex = availRooms.FindIndex(x => x.Id == currentRes.RoomId); } catch { } };
            loadAvail(); dtIn.ValueChanged += (s, e) => loadAvail(); dtOut.ValueChanged += (s, e) => loadAvail();
            f.Controls.Add(cmbRoom); y += 38;
            f.Controls.Add(new Label { Text = "Yetişkin / Çocuk", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var nAdult = new NumericUpDown { Value = currentRes?.Adults ?? 1, Minimum = 1, Maximum = 10, Location = new Point(20, y), Size = new Size(210, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            var nChild = new NumericUpDown { Value = currentRes?.Children ?? 0, Minimum = 0, Maximum = 10, Location = new Point(250, y), Size = new Size(210, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            f.Controls.AddRange(new Control[] { nAdult, nChild }); y += 38;
            f.Controls.Add(new Label { Text = "Notlar", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tNotes = new TextBox { Text = currentRes?.Notes ?? "", Location = new Point(20, y), Size = new Size(440, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; f.Controls.Add(tNotes); y += 40;
            var btnSave = MakeBtn("💾 Kaydet", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCnl = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCnl);
            bool saved = false;
            btnSave.Click += (s, e) => { if (cmbGuest.SelectedIndex < 0 || cmbRoom.SelectedIndex < 0) { MessageBox.Show("Misafir ve oda seçimi zorunlu!"); return; }
                if (dtOut.Value <= dtIn.Value) { MessageBox.Show("Çıkış tarihi giriş tarihinden sonra olmalı!"); return; }
                try { var res = new Reservation { GuestId = guests[cmbGuest.SelectedIndex].Id, RoomId = availRooms[cmbRoom.SelectedIndex].Id,
                    CheckInDate = dtIn.Value, CheckOutDate = dtOut.Value, Adults = (int)nAdult.Value, Children = (int)nChild.Value,
                    Status = currentRes?.Status ?? "Onaylandi", TotalPrice = availRooms[cmbRoom.SelectedIndex].PricePerNight * (dtOut.Value - dtIn.Value).Days,
                    Notes = tNotes.Text, CreatedBy = AuthHelper.CurrentUser?.Id, Id = currentRes?.Id ?? 0 };
                    
                    if (currentRes == null) { ReservationHelper.AddReservation(res); RoomHelper.UpdateRoomStatus(res.RoomId, "Reserved"); }
                    else { ReservationHelper.UpdateReservation(res); if (currentRes.RoomId != res.RoomId) { RoomHelper.UpdateRoomStatus(currentRes.RoomId, "Available"); RoomHelper.UpdateRoomStatus(res.RoomId, "Reserved"); } }
                    saved = true; f.Close(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } };
            btnCnl.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        // ========== ÖDEME ==========
        private void ShowPayments()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("💳 Ödeme İşlemleri"));
            var btnNew = MakeBtn("➕ Yeni Ödeme", cGreen, 20, 55); pnlMainContent.Controls.Add(btnNew);
            var btnDel = MakeBtn("🗑️ Ödeme Sil", cRed, 190, 55); pnlMainContent.Controls.Add(btnDel);
            var dtStart = new DateTimePicker { Location = new Point(pnlMainContent.ClientSize.Width - 380, 58), Size = new Size(110, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
            var dtEnd = new DateTimePicker { Location = new Point(pnlMainContent.ClientSize.Width - 260, 58), Size = new Size(110, 25), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            var btnFilter = MakeBtn("🔍 Filtrele", cCard, pnlMainContent.ClientSize.Width - 140, 55); 
            btnFilter.Size = new Size(100, 38);
            System.Windows.Forms.Label lblTire = new System.Windows.Forms.Label { Text = "-", BackColor = Color.Transparent, ForeColor = Color.White, Location = new Point(pnlMainContent.ClientSize.Width - 270, 60), AutoSize = true, Font = new Font("Segoe UI", 12F) };
            pnlMainContent.Controls.AddRange(new Control[] { dtStart, dtEnd, btnFilter, lblTire });

            var dg = MakeGrid(100, 500); dg.Size = new Size(pnlMainContent.ClientSize.Width - 40, pnlMainContent.ClientSize.Height - 130);
            dg.Columns.AddRange(new DataGridViewColumn[] {
                new DataGridViewTextBoxColumn{Name="Id",HeaderText="ID",Width=40},
                new DataGridViewTextBoxColumn{Name="Rez",HeaderText="Rez.ID",Width=60},
                new DataGridViewTextBoxColumn{Name="Misafir",HeaderText="Misafir"},
                new DataGridViewTextBoxColumn{Name="Oda",HeaderText="Oda",Width=60},
                new DataGridViewTextBoxColumn{Name="Tutar",HeaderText="Tutar ₺",Width=100},
                new DataGridViewTextBoxColumn{Name="Yontem",HeaderText="Yöntem",Width=110},
                new DataGridViewTextBoxColumn{Name="Tarih",HeaderText="Tarih",Width=120},
                new DataGridViewTextBoxColumn{Name="Not",HeaderText="Notlar"},
            });
            Action loadPay = () => { try { dg.Rows.Clear(); 
                var list = PaymentHelper.GetAllPayments();
                
                DateTime sDate = dtStart.Value.Date;
                DateTime eDate = dtEnd.Value.Date.AddDays(1).AddTicks(-1);
                list = list.FindAll(p => p.PaymentDate >= sDate && p.PaymentDate <= eDate);

                foreach (var p in list) dg.Rows.Add(p.Id, p.ReservationId, p.GuestName, p.RoomNumber, $"₺{p.Amount:N0}", p.PaymentMethodDisplay, p.PaymentDate.ToString("dd.MM.yyyy HH:mm"), p.Notes ?? ""); } catch (Exception ex) { MessageBox.Show(ex.Message); } };
            loadPay();
            btnFilter.Click += (s, e) => loadPay();
            btnNew.Click += (s, e) => { if (ShowPaymentDialog()) loadPay(); };
            btnDel.Click += (s, e) => { if (dg.SelectedRows.Count > 0) { int id = Convert.ToInt32(dg.SelectedRows[0].Cells["Id"].Value);
                if (MessageBox.Show("Seçili ödemeyi silmek istediğinize emin misiniz?", "Ödeme Sil", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    try { PaymentHelper.DeletePayment(id); loadPay(); } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } } } };
            pnlMainContent.Controls.Add(dg);
        }

        private bool ShowPaymentDialog(int preSelectedResId = 0)
        {
            var f = new Form { Text = "Yeni Ödeme", Size = new Size(450, 520), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 15;
            f.Controls.Add(new Label { Text = "Rezervasyon Seç *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbRes = new ComboBox { Location = new Point(20, y), Size = new Size(390, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            f.Controls.Add(cmbRes); y += 38;

            f.Controls.Add(new Label { Text = "Para Birimi", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbCurr = new ComboBox { Location = new Point(20, y), Size = new Size(180, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbCurr.Items.AddRange(new[] { "TRY", "USD", "EUR", "GBP" }); cmbCurr.SelectedIndex = 0;
            var lblRatePreview = new Label { Text = "Kur: 1.00", Location = new Point(220, y + 5), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = Color.LightGray };
            f.Controls.AddRange(new Control[] { cmbCurr, lblRatePreview }); y += 38;

            f.Controls.Add(new Label { Text = "Tutar (Seçilen Kurda) *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var nAmt = new NumericUpDown { Minimum = 1, Maximum = 999999, DecimalPlaces = 2, Location = new Point(20, y), Size = new Size(390, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            f.Controls.Add(nAmt); y += 38;

            var lblTlvPreview = new Label { Text = "Tahsil Edilecek: ₺0", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = cGreen };
            f.Controls.Add(lblTlvPreview); y += 30;

            decimal currentRate = 1m;
            Action updateCalc = () => {
                string curr = cmbCurr.SelectedItem?.ToString() ?? "TRY";
                currentRate = ORYS.Helpers.ExchangeRateHelper.GetRate(curr);
                lblRatePreview.Text = curr == "TRY" ? "Kur: 1.00" : $"Kur: {currentRate:N4}";
                decimal tryAmt = nAmt.Value * currentRate;
                lblTlvPreview.Text = $"Tahsil Edilecek: ₺{tryAmt:N0}";
            };
            cmbCurr.SelectedIndexChanged += (s, e) => updateCalc();
            nAmt.ValueChanged += (s, e) => updateCalc();

            List<Reservation> resList = new(); try { resList = ReservationHelper.GetActiveReservations(); } catch { }
            foreach (var r in resList) cmbRes.Items.Add($"#{r.Id} - {r.GuestName} (Oda {r.RoomNumber})"); 
            
            cmbRes.SelectedIndexChanged += (s, e) => {
                if (cmbRes.SelectedIndex >= 0) {
                    var r = resList[cmbRes.SelectedIndex];
                    decimal odenilen = PaymentHelper.GetTotalPaidForReservation(r.Id);
                    decimal kalan = r.TotalPrice - odenilen;
                    if (kalan > 0 && cmbCurr.SelectedIndex == 0) nAmt.Value = kalan <= nAmt.Maximum ? Math.Round(kalan, 0) : nAmt.Maximum;
                    else if (kalan <= 0) nAmt.Value = nAmt.Minimum;
                }
            };

            if (preSelectedResId > 0) {
                int findIdx = resList.FindIndex(x => x.Id == preSelectedResId);
                if (findIdx >= 0) cmbRes.SelectedIndex = findIdx;
                else if (cmbRes.Items.Count > 0) cmbRes.SelectedIndex = 0;
            }
            else if (cmbRes.Items.Count > 0) cmbRes.SelectedIndex = 0;

            f.Controls.Add(new Label { Text = "Ödeme Yöntemi", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbMethod = new ComboBox { Location = new Point(20, y), Size = new Size(390, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbMethod.Items.AddRange(new[] { "Nakit", "Kredi Karti", "Havale", "Diger" }); cmbMethod.SelectedIndex = 0;
            f.Controls.Add(cmbMethod); y += 38;
            f.Controls.Add(new Label { Text = "Notlar", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var tNotes = new TextBox { Location = new Point(20, y), Size = new Size(390, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; f.Controls.Add(tNotes); y += 35;
            var chkCheckout = new CheckBox { Text = "Ödeme sonrası otomatik Çıkış Yap", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = cGold, Checked = true };
            f.Controls.Add(chkCheckout); y += 35;

            var btnSave = MakeBtn("💾 Kaydet", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCnl = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCnl);
            bool saved = false;
            btnSave.Click += (s, e) => { 
                if (cmbRes.SelectedIndex < 0) { MessageBox.Show("Rezervasyon seçimi zorunlu!"); return; }
                try { 
                    var selectedRes = resList[cmbRes.SelectedIndex];
                    string curr = cmbCurr.SelectedItem?.ToString() ?? "TRY";
                    decimal tryAmount = nAmt.Value * currentRate;
                    string noteAdd = curr != "TRY" ? $"[DÖVİZ] Alınan: {nAmt.Value:N2} {curr} (Kur: {currentRate:N4}). " : "";
                    
                    var pmt = new Payment { 
                        ReservationId = selectedRes.Id, Amount = tryAmount,
                        PaymentMethod = cmbMethod.SelectedItem!.ToString()!, PaymentDate = DateTime.Now, 
                        Notes = noteAdd + tNotes.Text, CreatedBy = AuthHelper.CurrentUser?.Id 
                    };
                    PaymentHelper.AddPayment(pmt); 
                    
                    if (chkCheckout.Checked)
                    {
                        ReservationHelper.CheckOut(selectedRes.Id, selectedRes.RoomId);
                        MessageBox.Show($"Ödeme ({tryAmount:N0} ₺) tahsil edildi ve misafirin çıkışı başarıyla yapıldı!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"Ödeme ({tryAmount:N0} ₺) başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    saved = true; f.Close(); 
                } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } 
            };
            btnCnl.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        // ========== RAPORLAR ==========
        private void ShowReports()
        {
            ClearContent();
            pnlMainContent.Controls.Add(MakeTitle("📈 Raporlar"));
            int y = 60; int w = pnlMainContent.ClientSize.Width - 40;
            try
            {
                int totalRooms = RoomHelper.GetRoomCount();
                int occ = RoomHelper.GetRoomCount("Occupied");
                int avail = RoomHelper.GetRoomCount("Available");
                int maint = RoomHelper.GetRoomCount("Maintenance");
                int resv = RoomHelper.GetRoomCount("Reserved");
                decimal todayRev = PaymentHelper.GetDailyRevenue(DateTime.Today);
                decimal monthRev = PaymentHelper.GetMonthlyRevenue(DateTime.Now.Month, DateTime.Now.Year);
                int guestCount = GuestHelper.GetGuestCount();
                var activeRes = ReservationHelper.GetActiveReservations();
                // Stats Cards Row 1
                int cw = (w - 30) / 4;
                pnlMainContent.Controls.Add(MakeStatCard("🏨", "Toplam Oda", totalRooms.ToString(), 20)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(20, y);
                pnlMainContent.Controls.Add(MakeStatCard("📊", "Doluluk %", $"%{(totalRooms > 0 ? occ * 100 / totalRooms : 0)}", cw + 30)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(cw + 30, y);
                pnlMainContent.Controls.Add(MakeStatCard("👥", "Toplam Misafir", guestCount.ToString(), 2 * cw + 40)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(2 * cw + 40, y);
                pnlMainContent.Controls.Add(MakeStatCard("📅", "Aktif Rezervasyon", activeRes.Count.ToString(), 3 * cw + 50)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(3 * cw + 50, y);
                y += 100;
                pnlMainContent.Controls.Add(MakeStatCard("💰", "Bugün Gelir", $"₺{todayRev:N0}", 20)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(20, y);
                pnlMainContent.Controls.Add(MakeStatCard("📈", "Aylık Gelir", $"₺{monthRev:N0}", cw + 30)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(cw + 30, y);
                pnlMainContent.Controls.Add(MakeStatCard("🟢", "Müsait Oda", avail.ToString(), 2 * cw + 40)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(2 * cw + 40, y);
                pnlMainContent.Controls.Add(MakeStatCard("🔧", "Bakımda", maint.ToString(), 3 * cw + 50)); pnlMainContent.Controls[pnlMainContent.Controls.Count - 1].Location = new Point(3 * cw + 50, y);
                y += 110;
                // Oda Durumu Özeti
                var pnlSum = new Panel { Location = new Point(20, y), Size = new Size(w / 2 - 5, 250), BackColor = cCard };
                pnlSum.Controls.Add(new Label { Text = "Oda Durumu Özeti", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 10), BackColor = Color.Transparent });
                string[] statuses = { "Available", "Occupied", "Reserved", "Maintenance" }; Color[] colors = { cGreen, cRed, cYellow, cBlue }; string[] names = { "Müsait", "Dolu", "Rezerve", "Bakım" };
                int[] counts = { avail, occ, resv, maint }; int barY = 45;
                for (int i = 0; i < 4; i++)
                {
                    pnlSum.Controls.Add(new Label { Text = $"{names[i]}: {counts[i]}", Font = new Font("Segoe UI", 10F), ForeColor = Color.White, AutoSize = true, Location = new Point(15, barY), BackColor = Color.Transparent });
                    var bar = new Panel { Location = new Point(150, barY + 2), Size = new Size(totalRooms > 0 ? counts[i] * 200 / totalRooms : 0, 18), BackColor = colors[i] }; pnlSum.Controls.Add(bar);
                    pnlSum.Controls.Add(new Label { Text = $"%{(totalRooms > 0 ? counts[i] * 100 / totalRooms : 0)}", Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = colors[i], AutoSize = true, Location = new Point(360, barY), BackColor = Color.Transparent });
                    barY += 40;
                }
                pnlMainContent.Controls.Add(pnlSum);
                // Ödeme Yöntemi İstatistikleri
                var pnlPay = new Panel { Location = new Point(20 + w / 2 + 5, y), Size = new Size(w / 2 - 5, 250), BackColor = cCard };
                pnlPay.Controls.Add(new Label { Text = "Ödeme Yöntemi Dağılımı", Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 10), BackColor = Color.Transparent });
                try { var payStats = PaymentHelper.GetPaymentMethodStats(); int py = 50;
                    foreach (var ps in payStats) { pnlPay.Controls.Add(new Label { Text = $"{ps.Method}: {ps.Count} işlem - ₺{ps.Total:N0}", Font = new Font("Segoe UI", 11F), ForeColor = cText, AutoSize = true, Location = new Point(15, py), BackColor = Color.Transparent }); py += 35; }
                    if (payStats.Count == 0) pnlPay.Controls.Add(new Label { Text = "Henüz ödeme kaydı yok", Font = new Font("Segoe UI", 11F), ForeColor = Color.Gray, AutoSize = true, Location = new Point(15, 50), BackColor = Color.Transparent });
                } catch { pnlPay.Controls.Add(new Label { Text = "Veri yüklenemedi", Font = new Font("Segoe UI", 11F), ForeColor = cRed, AutoSize = true, Location = new Point(15, 50), BackColor = Color.Transparent }); }
                pnlMainContent.Controls.Add(pnlPay);
            }
            catch (Exception ex) { pnlMainContent.Controls.Add(new Label { Text = $"⚠️ Hata: {ex.Message}", Font = new Font("Segoe UI", 12F), ForeColor = cRed, AutoSize = true, Location = new Point(20, 60), BackColor = Color.Transparent }); }
        }

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
            string eName = "", eUser = "", ePass = "", eRole = "Resepsiyonist", eEmail = "", ePhone = "";
            if (isEdit) {
                try { var u = UserHelper.GetUserById(editId);
                    if (u != null) { eName = u.FullName; eUser = u.Username; ePass = u.Password;
                        eRole = u.Role; eEmail = u.Email ?? ""; ePhone = u.Phone ?? ""; }
                } catch { }
            }
            var f = new Form { Text = isEdit ? "Personel Düzenle" : "Yeni Personel", Size = new Size(450, 500), StartPosition = FormStartPosition.CenterParent, BackColor = cCard, ForeColor = Color.White, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false };
            int y = 20;
            TextBox MakeField(string label, string val = "") { f.Controls.Add(new Label { Text = label, Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
                var t = new TextBox { Text = val, Location = new Point(20, y), Size = new Size(390, 28), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; f.Controls.Add(t); y += 38; return t; }
            var tName = MakeField("Ad Soyad *", eName);
            var tUser = MakeField("Kullanıcı Adı *", eUser);
            var tPass = MakeField("Şifre *", ePass);
            f.Controls.Add(new Label { Text = "Rol *", Location = new Point(20, y), AutoSize = true, Font = new Font("Segoe UI", 10F), ForeColor = cGold }); y += 22;
            var cmbRole = new ComboBox { Location = new Point(20, y), Size = new Size(390, 30), Font = new Font("Segoe UI", 10F), DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(25, 40, 75), ForeColor = Color.White };
            cmbRole.Items.AddRange(new[] { "Admin", "Resepsiyonist", "Muhasebe" }); cmbRole.SelectedItem = eRole; if (cmbRole.SelectedIndex < 0) cmbRole.SelectedIndex = 1;
            f.Controls.Add(cmbRole); y += 38;
            var tEmail = MakeField("Email", eEmail);
            var tPhone = MakeField("Telefon", ePhone);
            var btnSave = MakeBtn("💾 Kaydet", cGreen, 20, y); f.Controls.Add(btnSave);
            var btnCancel = MakeBtn("❌ İptal", cRed, 180, y); f.Controls.Add(btnCancel);
            bool saved = false;
            btnSave.Click += (s, e) => { if (string.IsNullOrWhiteSpace(tName.Text) || string.IsNullOrWhiteSpace(tUser.Text) || (!isEdit && string.IsNullOrWhiteSpace(tPass.Text))) { MessageBox.Show("Ad, kullanıcı adı ve şifre zorunlu!"); return; }
                try {
                    // Şifre değiştiyse hashle, değişmediyse mevcut hash'i koru
                    string passwordToSave;
                    if (!string.IsNullOrWhiteSpace(tPass.Text) && tPass.Text != ePass)
                        passwordToSave = AuthHelper.HashPassword(tPass.Text);
                    else
                        passwordToSave = ePass;
                    
                    var u = new User { 
                        Id = editId, 
                        FullName = tName.Text.Trim(), 
                        Username = tUser.Text.Trim(), 
                        Password = passwordToSave, 
                        Role = cmbRole.SelectedItem!.ToString()!, 
                        Email = string.IsNullOrWhiteSpace(tEmail.Text) ? null : tEmail.Text.Trim(), 
                        Phone = string.IsNullOrWhiteSpace(tPhone.Text) ? null : tPhone.Text.Trim() 
                    };
                    if (isEdit) UserHelper.UpdateUser(u);
                    else UserHelper.AddUser(u);
                    saved = true; f.Close();
                } catch (Exception ex) { MessageBox.Show($"Hata: {ex.Message}"); } };
            btnCancel.Click += (s, e) => f.Close();
            f.ShowDialog(); return saved;
        }

        protected override void OnFormClosing(FormClosingEventArgs e) { base.OnFormClosing(e); }
    }
}
