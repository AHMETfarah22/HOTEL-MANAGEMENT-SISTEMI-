using System.Drawing.Drawing2D;

namespace ORYS.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            // ==========================================
            // ANA FORM AYARLARI
            // ==========================================
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 600);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "LoginForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ORYS - Otel Rezervasyonu Yönetim Sistemi";
            this.BackColor = Color.FromArgb(12, 20, 45);
            this.DoubleBuffered = true;

            // ==========================================
            // MERKEZİ GİRİŞ KARTI PANELİ
            // ==========================================
            this.pnlCard = new Panel();
            this.pnlCard.Size = new Size(430, 560);
            this.pnlCard.BackColor = Color.FromArgb(18, 30, 62);

            // ==========================================
            // LOGO ALANI
            // ==========================================
            this.lblLogoIcon = new Label();
            this.lblLogoIcon.Text = "🏨";
            this.lblLogoIcon.Font = new Font("Segoe UI Emoji", 48F, FontStyle.Regular);
            this.lblLogoIcon.ForeColor = Color.FromArgb(218, 165, 32);
            this.lblLogoIcon.TextAlign = ContentAlignment.MiddleCenter;
            this.lblLogoIcon.Size = new Size(430, 75);
            this.lblLogoIcon.Location = new Point(0, 15);
            this.lblLogoIcon.BackColor = Color.Transparent;

            // Başlık - OTEL REZERVASYON YÖNETİM SİSTEMİ
            this.lblMainTitle = new Label();
            this.lblMainTitle.Text = "OTEL REZERVASYON YÖNETİM SİSTEMİ";
            this.lblMainTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this.lblMainTitle.ForeColor = Color.FromArgb(218, 165, 32);
            this.lblMainTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblMainTitle.Size = new Size(430, 28);
            this.lblMainTitle.Location = new Point(0, 90);
            this.lblMainTitle.BackColor = Color.Transparent;

            // Alt Başlık - ORYS - Login
            this.lblSubTitle = new Label();
            this.lblSubTitle.Text = "ORYS - Login";
            this.lblSubTitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblSubTitle.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblSubTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblSubTitle.Size = new Size(430, 22);
            this.lblSubTitle.Location = new Point(0, 118);
            this.lblSubTitle.BackColor = Color.Transparent;

            // ==========================================
            // KULLANICI ADI ALANI
            // ==========================================
            this.pnlUsernameContainer = new Panel();
            this.pnlUsernameContainer.Size = new Size(340, 44);
            this.pnlUsernameContainer.Location = new Point(45, 160);
            this.pnlUsernameContainer.BackColor = Color.FromArgb(25, 40, 75);

            this.lblUsernameIcon = new Label();
            this.lblUsernameIcon.Text = "👤";
            this.lblUsernameIcon.Font = new Font("Segoe UI Emoji", 14F);
            this.lblUsernameIcon.Size = new Size(40, 44);
            this.lblUsernameIcon.Location = new Point(0, 0);
            this.lblUsernameIcon.TextAlign = ContentAlignment.MiddleCenter;
            this.lblUsernameIcon.BackColor = Color.Transparent;

            this.txtUsername = new TextBox();
            this.txtUsername.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            this.txtUsername.Size = new Size(290, 30);
            this.txtUsername.Location = new Point(42, 9);
            this.txtUsername.BackColor = Color.FromArgb(25, 40, 75);
            this.txtUsername.ForeColor = Color.White;
            this.txtUsername.BorderStyle = BorderStyle.None;
            this.txtUsername.MaxLength = 50;

            this.pnlUsernameContainer.Controls.Add(this.lblUsernameIcon);
            this.pnlUsernameContainer.Controls.Add(this.txtUsername);

            // Placeholder label for username
            this.lblUsernamePlaceholder = new Label();
            this.lblUsernamePlaceholder.Text = "Kullanıcı Adı";
            this.lblUsernamePlaceholder.Font = new Font("Segoe UI", 11F, FontStyle.Italic);
            this.lblUsernamePlaceholder.ForeColor = Color.FromArgb(100, 110, 140);
            this.lblUsernamePlaceholder.Size = new Size(280, 30);
            this.lblUsernamePlaceholder.Location = new Point(42, 9);
            this.lblUsernamePlaceholder.BackColor = Color.Transparent;
            this.lblUsernamePlaceholder.Cursor = Cursors.IBeam;
            this.pnlUsernameContainer.Controls.Add(this.lblUsernamePlaceholder);
            this.lblUsernamePlaceholder.BringToFront();

            // ==========================================
            // ŞİFRE ALANI
            // ==========================================
            this.pnlPasswordContainer = new Panel();
            this.pnlPasswordContainer.Size = new Size(340, 44);
            this.pnlPasswordContainer.Location = new Point(45, 220);
            this.pnlPasswordContainer.BackColor = Color.FromArgb(25, 40, 75);

            this.lblPasswordIcon = new Label();
            this.lblPasswordIcon.Text = "🔒";
            this.lblPasswordIcon.Font = new Font("Segoe UI Emoji", 14F);
            this.lblPasswordIcon.Size = new Size(40, 44);
            this.lblPasswordIcon.Location = new Point(0, 0);
            this.lblPasswordIcon.TextAlign = ContentAlignment.MiddleCenter;
            this.lblPasswordIcon.BackColor = Color.Transparent;

            this.txtPassword = new TextBox();
            this.txtPassword.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            this.txtPassword.Size = new Size(252, 30);
            this.txtPassword.Location = new Point(42, 9);
            this.txtPassword.BackColor = Color.FromArgb(25, 40, 75);
            this.txtPassword.ForeColor = Color.White;
            this.txtPassword.BorderStyle = BorderStyle.None;
            this.txtPassword.PasswordChar = '●';
            this.txtPassword.MaxLength = 50;

            // Şifre Placeholder
            this.lblPasswordPlaceholder = new Label();
            this.lblPasswordPlaceholder.Text = "Şifre";
            this.lblPasswordPlaceholder.Font = new Font("Segoe UI", 11F, FontStyle.Italic);
            this.lblPasswordPlaceholder.ForeColor = Color.FromArgb(100, 110, 140);
            this.lblPasswordPlaceholder.Size = new Size(240, 30);
            this.lblPasswordPlaceholder.Location = new Point(42, 9);
            this.lblPasswordPlaceholder.BackColor = Color.Transparent;
            this.lblPasswordPlaceholder.Cursor = Cursors.IBeam;

            // Şifre Göster/Gizle butonu (göz ikonu)
            this.btnTogglePassword = new Label();
            this.btnTogglePassword.Text = "👁";
            this.btnTogglePassword.Font = new Font("Segoe UI Emoji", 12F);
            this.btnTogglePassword.Size = new Size(38, 44);
            this.btnTogglePassword.Location = new Point(300, 0);
            this.btnTogglePassword.TextAlign = ContentAlignment.MiddleCenter;
            this.btnTogglePassword.BackColor = Color.Transparent;
            this.btnTogglePassword.Cursor = Cursors.Hand;
            this.btnTogglePassword.ForeColor = Color.FromArgb(100, 110, 140);

            this.pnlPasswordContainer.Controls.Add(this.lblPasswordIcon);
            this.pnlPasswordContainer.Controls.Add(this.txtPassword);
            this.pnlPasswordContainer.Controls.Add(this.lblPasswordPlaceholder);
            this.pnlPasswordContainer.Controls.Add(this.btnTogglePassword);
            this.lblPasswordPlaceholder.BringToFront();

            // ==========================================
            // ROL SEÇİMİ
            // ==========================================
            this.lblRoleTitle = new Label();
            this.lblRoleTitle.Text = "Lütfen Rolünüzü Seçiniz";
            this.lblRoleTitle.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.lblRoleTitle.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblRoleTitle.Size = new Size(340, 22);
            this.lblRoleTitle.Location = new Point(45, 280);
            this.lblRoleTitle.BackColor = Color.Transparent;

            this.cmbRole = new ComboBox();
            this.cmbRole.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            this.cmbRole.Size = new Size(340, 32);
            this.cmbRole.Location = new Point(45, 305);
            this.cmbRole.BackColor = Color.FromArgb(25, 40, 75);
            this.cmbRole.ForeColor = Color.White;
            this.cmbRole.FlatStyle = FlatStyle.Flat;
            this.cmbRole.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRole.Items.AddRange(new object[] { "Rol Seçiniz", "Admin", "Resepsiyonist", "Muhasebe" });
            this.cmbRole.SelectedIndex = 0;

            // ==========================================
            // GİRİŞ YAP BUTONU
            // ==========================================
            this.btnLogin = new Button();
            this.btnLogin.Text = "GİRİŞ YAP";
            this.btnLogin.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.btnLogin.Size = new Size(340, 50);
            this.btnLogin.Location = new Point(45, 365);
            this.btnLogin.FlatStyle = FlatStyle.Flat;
            this.btnLogin.FlatAppearance.BorderSize = 0;
            this.btnLogin.BackColor = Color.FromArgb(218, 165, 32);
            this.btnLogin.ForeColor = Color.White;
            this.btnLogin.Cursor = Cursors.Hand;

            // ==========================================
            // ŞİFREMİ UNUTTUM LİNKİ
            // ==========================================
            this.lblForgotPassword = new Label();
            this.lblForgotPassword.Text = "Şifremi Unuttum";
            this.lblForgotPassword.Font = new Font("Segoe UI", 9.5F, FontStyle.Underline);
            this.lblForgotPassword.ForeColor = Color.FromArgb(120, 135, 175);
            this.lblForgotPassword.TextAlign = ContentAlignment.MiddleCenter;
            this.lblForgotPassword.Size = new Size(170, 22);
            this.lblForgotPassword.Location = new Point(45, 430);
            this.lblForgotPassword.BackColor = Color.Transparent;
            this.lblForgotPassword.Cursor = Cursors.Hand;

            // ==========================================
            // KAYIT OL LİNKİ
            // ==========================================
            this.lblRegisterLink = new Label();
            this.lblRegisterLink.Text = "Kayıt Ol";
            this.lblRegisterLink.Font = new Font("Segoe UI", 9.5F, FontStyle.Underline);
            this.lblRegisterLink.ForeColor = Color.FromArgb(218, 165, 32);
            this.lblRegisterLink.TextAlign = ContentAlignment.MiddleCenter;
            this.lblRegisterLink.Size = new Size(170, 22);
            this.lblRegisterLink.Location = new Point(215, 430);
            this.lblRegisterLink.BackColor = Color.Transparent;
            this.lblRegisterLink.Cursor = Cursors.Hand;

            // ==========================================
            // DURUM ETİKETİ (Hata mesajları)
            // ==========================================
            this.lblStatus = new Label();
            this.lblStatus.Text = "";
            this.lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
            this.lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.lblStatus.Size = new Size(340, 40);
            this.lblStatus.Location = new Point(45, 460);
            this.lblStatus.BackColor = Color.Transparent;

            // ==========================================
            // KART PANELİNE KONTROLLERİ EKLE
            // ==========================================
            this.pnlCard.Controls.Add(this.lblLogoIcon);
            this.pnlCard.Controls.Add(this.lblMainTitle);
            this.pnlCard.Controls.Add(this.lblSubTitle);
            this.pnlCard.Controls.Add(this.pnlUsernameContainer);
            this.pnlCard.Controls.Add(this.pnlPasswordContainer);
            this.pnlCard.Controls.Add(this.lblRoleTitle);
            this.pnlCard.Controls.Add(this.cmbRole);
            this.pnlCard.Controls.Add(this.btnLogin);
            this.pnlCard.Controls.Add(this.lblForgotPassword);
            this.pnlCard.Controls.Add(this.lblRegisterLink);
            this.pnlCard.Controls.Add(this.lblStatus);

            // ==========================================
            // FORMA KART EKLEMESİ
            // ==========================================
            this.Controls.Add(this.pnlCard);
        }

        #endregion

        // Kart paneli
        private Panel pnlCard;

        // Logo & Başlıklar
        private Label lblLogoIcon;
        private Label lblMainTitle;
        private Label lblSubTitle;

        // Kullanıcı Adı
        private Panel pnlUsernameContainer;
        private Label lblUsernameIcon;
        private TextBox txtUsername;
        private Label lblUsernamePlaceholder;

        // Şifre
        private Panel pnlPasswordContainer;
        private Label lblPasswordIcon;
        private TextBox txtPassword;
        private Label lblPasswordPlaceholder;
        private Label btnTogglePassword;

        // Rol Seçimi
        private Label lblRoleTitle;
        private ComboBox cmbRole;

        // Giriş Butonu
        private Button btnLogin;

        // Şifremi Unuttum
        private Label lblForgotPassword;

        // Kayıt Ol
        private Label lblRegisterLink;

        // Durum
        private Label lblStatus;
    }
}
