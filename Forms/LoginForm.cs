using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using ORYS.Database;
using ORYS.Helpers;

namespace ORYS.Forms
{
    /// <summary>
    /// Giriş Formu - Admin, Resepsiyonist ve Muhasebe rolleri için
    /// Tasarım: Merkezi kart, koyu lacivert arka plan, altın rengi vurgular
    /// </summary>
    public partial class LoginForm : Form
    {
        private System.Windows.Forms.Timer? animTimer;
        private float animOpacity = 0f;
        private bool passwordVisible = false;

        public LoginForm()
        {
            InitializeComponent();
            SetupForm();
            SetupAnimations();
            SetupPlaceholders();
        }

        /// <summary>
        /// Form ayarlarını yapar
        /// </summary>
        private void SetupForm()
        {
            // Form yüklendiğinde
            this.Load += (s, e) =>
            {
                CenterCard();

                // Veritabanı bağlantısı
                try
                {
                    lblStatus.ForeColor = Color.FromArgb(218, 165, 32);
                    lblStatus.Text = "⏳ Veritabanına bağlanılıyor...";
                    Application.DoEvents();

                    DatabaseHelper.InitializeDatabase();

                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                    lblStatus.Text = "✅ Veritabanı bağlantısı başarılı!";

                    var clearTimer = new System.Windows.Forms.Timer();
                    clearTimer.Interval = 2000;
                    clearTimer.Tick += (ts, te) =>
                    {
                        lblStatus.Text = "";
                        clearTimer.Stop();
                        clearTimer.Dispose();
                    };
                    clearTimer.Start();
                }
                catch (Exception ex)
                {
                    lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                    lblStatus.Text = $"❌ DB Hatası: {ex.Message}";
                }

                txtUsername.Focus();
            };

            this.Resize += (s, e) => CenterCard();

            // Enter tuşu ile giriş
            this.AcceptButton = btnLogin;

            // Giriş butonu click
            btnLogin.Click += btnLogin_Click;

            // Buton hover efektleri
            btnLogin.MouseEnter += (s, e) =>
            {
                btnLogin.BackColor = Color.FromArgb(195, 145, 25);
            };
            btnLogin.MouseLeave += (s, e) =>
            {
                btnLogin.BackColor = Color.FromArgb(218, 165, 32);
            };

            // Şifre göster/gizle
            btnTogglePassword.Click += (s, e) =>
            {
                passwordVisible = !passwordVisible;
                txtPassword.PasswordChar = passwordVisible ? '\0' : '●';
                btnTogglePassword.Text = passwordVisible ? "🙈" : "👁";
            };

            // Şifremi unuttum
            lblForgotPassword.Click += (s, e) =>
            {
                MessageBox.Show(
                    "Şifre sıfırlama işlemi için lütfen sistem yöneticinize başvurunuz.",
                    "Şifremi Unuttum",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            lblForgotPassword.MouseEnter += (s, e) =>
            {
                lblForgotPassword.ForeColor = Color.FromArgb(218, 165, 32);
            };
            lblForgotPassword.MouseLeave += (s, e) =>
            {
                lblForgotPassword.ForeColor = Color.FromArgb(120, 135, 175);
            };

            // Kayıt Ol linki
            lblRegisterLink.Click += (s, e) =>
            {
                var registerForm = new RegisterForm();
                registerForm.ShowDialog(this);
            };
            lblRegisterLink.MouseEnter += (s, e) =>
            {
                lblRegisterLink.ForeColor = Color.FromArgb(255, 200, 50);
            };
            lblRegisterLink.MouseLeave += (s, e) =>
            {
                lblRegisterLink.ForeColor = Color.FromArgb(218, 165, 32);
            };

            // TextBox odak efektleri
            SetupTextBoxFocus(pnlUsernameContainer, txtUsername);
            SetupTextBoxFocus(pnlPasswordContainer, txtPassword);

            // Arka plan gradient boyama
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

            // Kart paneli border çizimi (rounded corners + border)
            pnlCard.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, pnlCard.Width - 1, pnlCard.Height - 1);

                // Border
                using (var borderPen = new Pen(Color.FromArgb(40, 60, 110), 2))
                {
                    DrawRoundedRect(g, borderPen, rect, 20);
                }

                // Üst kenar altın çizgisi
                using (var goldPen = new Pen(Color.FromArgb(218, 165, 32), 2))
                {
                    g.DrawLine(goldPen, 20, 1, pnlCard.Width - 20, 1);
                }
            };

            // Username & Password container border
            PaintContainerBorder(pnlUsernameContainer);
            PaintContainerBorder(pnlPasswordContainer);
        }

        /// <summary>
        /// Container'a border çizer
        /// </summary>
        private void PaintContainerBorder(Panel container)
        {
            container.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, container.Width - 1, container.Height - 1);
                using (var pen = new Pen(Color.FromArgb(40, 60, 110), 1))
                {
                    DrawRoundedRect(g, pen, rect, 8);
                }
            };
        }

        /// <summary>
        /// TextBox odak efektleri
        /// </summary>
        private void SetupTextBoxFocus(Panel container, TextBox textBox)
        {
            textBox.GotFocus += (s, e) =>
            {
                container.BackColor = Color.FromArgb(30, 48, 85);
                container.Invalidate();
            };
            textBox.LostFocus += (s, e) =>
            {
                container.BackColor = Color.FromArgb(25, 40, 75);
                container.Invalidate();
            };
        }

        /// <summary>
        /// Placeholder setup
        /// </summary>
        private void SetupPlaceholders()
        {
            // Username placeholder
            SetupPlaceholder(txtUsername, lblUsernamePlaceholder);
            // Password placeholder
            SetupPlaceholder(txtPassword, lblPasswordPlaceholder);
        }

        private void SetupPlaceholder(TextBox textBox, Label placeholder)
        {
            placeholder.Click += (s, e) => textBox.Focus();

            textBox.TextChanged += (s, e) =>
            {
                placeholder.Visible = string.IsNullOrEmpty(textBox.Text);
            };
            textBox.GotFocus += (s, e) =>
            {
                if (!string.IsNullOrEmpty(textBox.Text))
                    placeholder.Visible = false;
            };
            textBox.LostFocus += (s, e) =>
            {
                placeholder.Visible = string.IsNullOrEmpty(textBox.Text);
            };
        }

        /// <summary>
        /// Animasyonlar
        /// </summary>
        private void SetupAnimations()
        {
            this.Opacity = 0;
            animTimer = new System.Windows.Forms.Timer();
            animTimer.Interval = 15;
            animTimer.Tick += (s, e) =>
            {
                animOpacity += 0.05f;
                if (animOpacity >= 1f)
                {
                    animOpacity = 1f;
                    animTimer.Stop();
                }
                this.Opacity = animOpacity;
            };
            this.Shown += (s, e) => animTimer.Start();
        }

        /// <summary>
        /// Kartı ortalar
        /// </summary>
        private void CenterCard()
        {
            if (pnlCard != null)
            {
                pnlCard.Location = new Point(
                    (this.ClientSize.Width - pnlCard.Width) / 2,
                    (this.ClientSize.Height - pnlCard.Height) / 2);
            }
        }

        /// <summary>
        /// Giriş butonu
        /// </summary>
        private void btnLogin_Click(object? sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            // Rol kontrolü
            if (cmbRole.SelectedIndex == 0)
            {
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                lblStatus.Text = "❌ Lütfen bir rol seçiniz!";
                cmbRole.Focus();
                return;
            }

            string selectedRole = cmbRole.SelectedItem?.ToString() ?? "";

            // Butonu devre dışı bırak
            btnLogin.Enabled = false;
            btnLogin.Text = "⏳ GİRİŞ YAPILIYOR...";
            btnLogin.BackColor = Color.FromArgb(120, 130, 150);
            Application.DoEvents();

            try
            {
                if (AuthHelper.Login(username, password, out string errorMessage))
                {
                    // Rol kontrolü
                    if (AuthHelper.CurrentUser!.Role != selectedRole)
                    {
                        lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                        lblStatus.Text = "❌ Seçilen rol ile hesap rolü eşleşmiyor!";
                        AuthHelper.Logout();
                        ResetLoginButton();
                        return;
                    }

                    // Başarılı giriş
                    lblStatus.ForeColor = Color.FromArgb(74, 222, 128);
                    lblStatus.Text = $"✅ Hoş geldiniz, {AuthHelper.CurrentUser.FullName}!";
                    Application.DoEvents();

                    var openTimer = new System.Windows.Forms.Timer();
                    openTimer.Interval = 1000;
                    openTimer.Tick += (ts, te) =>
                    {
                        openTimer.Stop();
                        openTimer.Dispose();

                        this.Hide();
                        var mainForm = new MainForm();
                        mainForm.FormClosed += (ms, me) =>
                        {
                            AuthHelper.Logout();
                            txtPassword.Clear();
                            cmbRole.SelectedIndex = 0;
                            lblStatus.Text = "";
                            ResetLoginButton();
                            this.Show();
                            txtUsername.Focus();
                        };
                        mainForm.Show();
                    };
                    openTimer.Start();
                }
                else
                {
                    lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                    lblStatus.Text = $"❌ {errorMessage}";
                    ShakeForm();
                    ResetLoginButton();
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                lblStatus.Text = $"❌ Hata: {ex.Message}";
                ResetLoginButton();
            }
        }

        /// <summary>
        /// Butonu sıfırla
        /// </summary>
        private void ResetLoginButton()
        {
            btnLogin.Enabled = true;
            btnLogin.Text = "GİRİŞ YAP";
            btnLogin.BackColor = Color.FromArgb(218, 165, 32);
        }

        /// <summary>
        /// Hata titremesi
        /// </summary>
        private void ShakeForm()
        {
            var original = this.Location;
            var shakeTimer = new System.Windows.Forms.Timer();
            int shakeCount = 0;
            shakeTimer.Interval = 30;
            shakeTimer.Tick += (s, e) =>
            {
                shakeCount++;
                if (shakeCount <= 10)
                {
                    this.Location = new Point(
                        original.X + (shakeCount % 2 == 0 ? 5 : -5),
                        original.Y);
                }
                else
                {
                    this.Location = original;
                    shakeTimer.Stop();
                    shakeTimer.Dispose();
                }
            };
            shakeTimer.Start();
        }

        /// <summary>
        /// Yuvarlak köşeli dikdörtgen
        /// </summary>
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

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CenterCard();
        }
    }
}
