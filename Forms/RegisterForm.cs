using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using ORYS.Database;

namespace ORYS.Forms
{
    /// <summary>
    /// Kayıt Formu - Yeni kullanıcı kaydı
    /// </summary>
    public partial class RegisterForm : Form
    {
        public RegisterForm()
        {
            InitializeComponent();
            SetupForm();
        }

        private void SetupForm()
        {
            this.Load += (s, e) => CenterCard();
            this.Resize += (s, e) => CenterCard();

            // Kayıt butonu click
            btnRegister.Click += btnRegister_Click;

            // Buton hover
            btnRegister.MouseEnter += (s, e) => btnRegister.BackColor = Color.FromArgb(195, 145, 25);
            btnRegister.MouseLeave += (s, e) => btnRegister.BackColor = Color.FromArgb(218, 165, 32);

            // Arka plan gradient
            this.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(
                    this.ClientRectangle,
                    Color.FromArgb(8, 15, 40),
                    Color.FromArgb(18, 30, 65),
                    LinearGradientMode.ForwardDiagonal))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            };

            // Kart border
            pnlCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                using (var borderPen = new Pen(Color.FromArgb(40, 60, 110), 2))
                {
                    DrawRoundedRect(g, borderPen, rect, 20);
                }
                using (var goldPen = new Pen(Color.FromArgb(218, 165, 32), 2))
                {
                    g.DrawLine(goldPen, 20, 1, pnlCard.Width - 20, 1);
                }
            };

            // Input panel border'ları
            PaintInputBorder(pnlFullName);
            PaintInputBorder(pnlUsername);
            PaintInputBorder(pnlEmail);
            PaintInputBorder(pnlPhone);
            PaintInputBorder(pnlPassword);

            // Fade-in animasyonu
            this.Opacity = 0;
            var animTimer = new System.Windows.Forms.Timer();
            float opacity = 0;
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) =>
            {
                opacity += 0.06f;
                if (opacity >= 1f) { opacity = 1f; animTimer.Stop(); animTimer.Dispose(); }
                this.Opacity = opacity;
            };
            this.Shown += (s, e) => animTimer.Start();
        }

        private void PaintInputBorder(Panel panel)
        {
            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using (var pen = new Pen(Color.FromArgb(40, 60, 110), 1))
                {
                    DrawRoundedRect(g, pen, rect, 8);
                }
            };
        }

        /// <summary>
        /// Kayıt butonuna tıklandığında
        /// </summary>
        private void btnRegister_Click(object? sender, EventArgs e)
        {
            // Validasyonlar
            string fullName = txtFullName.Text.Trim();
            string username = txtRegUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string password = txtRegPassword.Text;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                ShowError("❌ Ad Soyad boş bırakılamaz!"); txtFullName.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("❌ Kullanıcı adı boş bırakılamaz!"); txtRegUsername.Focus(); return;
            }
            if (username.Length < 3)
            {
                ShowError("❌ Kullanıcı adı en az 3 karakter olmalı!"); txtRegUsername.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("❌ Şifre boş bırakılamaz!"); txtRegPassword.Focus(); return;
            }
            if (password.Length < 5)
            {
                ShowError("❌ Şifre en az 5 karakter olmalı!"); txtRegPassword.Focus(); return;
            }
            if (cmbRegRole.SelectedIndex == 0)
            {
                ShowError("❌ Lütfen bir rol seçiniz!"); cmbRegRole.Focus(); return;
            }

            string selectedRole = cmbRegRole.SelectedItem?.ToString() ?? "";

            // Sunucu tarafı güvenlik: Admin rolüyle kayıt engelle
            if (selectedRole == "Admin")
            {
                ShowError("❌ Admin hesabı yalnızca Personel Yönetimi üzerinden oluşturulabilir!");
                ResetButton();
                return;
            }

            // Butonu devre dışı bırak
            btnRegister.Enabled = false;
            btnRegister.Text = "⏳ KAYIT YAPILIYOR...";
            btnRegister.BackColor = Color.FromArgb(120, 130, 150);
            Application.DoEvents();

            try
            {
                // Şifreyi BCrypt ile hashle
                string hashedPassword = ORYS.Helpers.AuthHelper.HashPassword(password);

                using (var connection = DatabaseHelper.GetConnection())
                {
                    connection.Open();

                    // Kullanıcı adı kontrolü
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = @username";
                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            ShowError("❌ Bu kullanıcı adı zaten kullanılıyor!");
                            ResetButton();
                            txtRegUsername.Focus();
                            return;
                        }
                    }

                    // Yeni kullanıcı ekle (hashlenmiş şifre ile)
                    string insertQuery = @"
                        INSERT INTO users (full_name, username, password, role, email, phone, is_active) 
                        VALUES (@fullName, @username, @password, @role, @email, @phone, 1)";

                    using (var cmd = new MySqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@fullName", fullName);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", hashedPassword);
                        cmd.Parameters.AddWithValue("@role", selectedRole);
                        cmd.Parameters.AddWithValue("@email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(phone) ? (object)DBNull.Value : phone);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Başarılı kayıt
                lblRegStatus.ForeColor = Color.FromArgb(74, 222, 128);
                lblRegStatus.Text = "✅ Kayıt başarılı! Giriş yapabilirsiniz.";

                MessageBox.Show(
                    $"Kayıt başarıyla tamamlandı!\n\nAd Soyad: {fullName}\nKullanıcı Adı: {username}\nRol: {selectedRole}\n\nŞimdi giriş yapabilirsiniz.",
                    "Kayıt Başarılı ✅",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Formu kapat
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"❌ Hata: {ex.Message}");
                ResetButton();
            }
        }

        private void ShowError(string message)
        {
            lblRegStatus.ForeColor = Color.FromArgb(248, 113, 113);
            lblRegStatus.Text = message;
        }

        private void ResetButton()
        {
            btnRegister.Enabled = true;
            btnRegister.Text = "KAYIT OL";
            btnRegister.BackColor = Color.FromArgb(218, 165, 32);
        }

        private void CenterCard()
        {
            if (pnlCard != null)
            {
                pnlCard.Location = new Point(
                    (this.ClientSize.Width - pnlCard.Width) / 2,
                    (this.ClientSize.Height - pnlCard.Height) / 2);
            }
        }

        private void DrawRoundedRect(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = new GraphicsPath())
            {
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
                g.DrawPath(pen, path);
            }
        }
    }
}
