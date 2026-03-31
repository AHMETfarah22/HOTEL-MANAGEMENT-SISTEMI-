using System.Drawing.Drawing2D;

namespace ORYS.Forms
{
    partial class RegisterForm
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
            // FORM AYARLARI
            // ==========================================
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(750, 680);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "RegisterForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "ORYS - Yeni Kayıt";
            this.BackColor = Color.FromArgb(12, 20, 45);
            this.DoubleBuffered = true;

            // ==========================================
            // MERKEZİ KAYIT KARTI
            // ==========================================
            this.pnlCard = new Panel();
            this.pnlCard.Size = new Size(430, 600);
            this.pnlCard.BackColor = Color.FromArgb(18, 30, 62);

            // Logo
            this.lblLogoIcon = new Label();
            this.lblLogoIcon.Text = "🏨";
            this.lblLogoIcon.Font = new Font("Segoe UI Emoji", 36F);
            this.lblLogoIcon.ForeColor = Color.FromArgb(218, 165, 32);
            this.lblLogoIcon.TextAlign = ContentAlignment.MiddleCenter;
            this.lblLogoIcon.Size = new Size(430, 55);
            this.lblLogoIcon.Location = new Point(0, 10);
            this.lblLogoIcon.BackColor = Color.Transparent;

            // Başlık
            this.lblTitle = new Label();
            this.lblTitle.Text = "YENİ KULLANICI KAYDI";
            this.lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            this.lblTitle.ForeColor = Color.FromArgb(218, 165, 32);
            this.lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblTitle.Size = new Size(430, 30);
            this.lblTitle.Location = new Point(0, 65);
            this.lblTitle.BackColor = Color.Transparent;

            // Alt başlık
            this.lblSubTitle = new Label();
            this.lblSubTitle.Text = "Lütfen bilgilerinizi eksiksiz doldurunuz";
            this.lblSubTitle.Font = new Font("Segoe UI", 9.5F);
            this.lblSubTitle.ForeColor = Color.FromArgb(140, 150, 180);
            this.lblSubTitle.TextAlign = ContentAlignment.MiddleCenter;
            this.lblSubTitle.Size = new Size(430, 20);
            this.lblSubTitle.Location = new Point(0, 95);
            this.lblSubTitle.BackColor = Color.Transparent;

            // ==========================================
            // AD SOYAD
            // ==========================================
            this.lblFullNameLabel = new Label();
            this.lblFullNameLabel.Text = "👤  Ad Soyad";
            this.lblFullNameLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblFullNameLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblFullNameLabel.Size = new Size(340, 20);
            this.lblFullNameLabel.Location = new Point(45, 130);
            this.lblFullNameLabel.BackColor = Color.Transparent;

            this.pnlFullName = new Panel();
            this.pnlFullName.Size = new Size(340, 40);
            this.pnlFullName.Location = new Point(45, 152);
            this.pnlFullName.BackColor = Color.FromArgb(25, 40, 75);

            this.txtFullName = new TextBox();
            this.txtFullName.Font = new Font("Segoe UI", 11F);
            this.txtFullName.Size = new Size(320, 28);
            this.txtFullName.Location = new Point(10, 7);
            this.txtFullName.BackColor = Color.FromArgb(25, 40, 75);
            this.txtFullName.ForeColor = Color.White;
            this.txtFullName.BorderStyle = BorderStyle.None;
            this.txtFullName.MaxLength = 100;
            this.pnlFullName.Controls.Add(this.txtFullName);

            // ==========================================
            // KULLANICI ADI
            // ==========================================
            this.lblUsernameLabel = new Label();
            this.lblUsernameLabel.Text = "🔑  Kullanıcı Adı";
            this.lblUsernameLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblUsernameLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblUsernameLabel.Size = new Size(340, 20);
            this.lblUsernameLabel.Location = new Point(45, 200);
            this.lblUsernameLabel.BackColor = Color.Transparent;

            this.pnlUsername = new Panel();
            this.pnlUsername.Size = new Size(340, 40);
            this.pnlUsername.Location = new Point(45, 222);
            this.pnlUsername.BackColor = Color.FromArgb(25, 40, 75);

            this.txtRegUsername = new TextBox();
            this.txtRegUsername.Font = new Font("Segoe UI", 11F);
            this.txtRegUsername.Size = new Size(320, 28);
            this.txtRegUsername.Location = new Point(10, 7);
            this.txtRegUsername.BackColor = Color.FromArgb(25, 40, 75);
            this.txtRegUsername.ForeColor = Color.White;
            this.txtRegUsername.BorderStyle = BorderStyle.None;
            this.txtRegUsername.MaxLength = 50;
            this.pnlUsername.Controls.Add(this.txtRegUsername);

            // ==========================================
            // E-POSTA
            // ==========================================
            this.lblEmailLabel = new Label();
            this.lblEmailLabel.Text = "📧  E-Posta";
            this.lblEmailLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblEmailLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblEmailLabel.Size = new Size(340, 20);
            this.lblEmailLabel.Location = new Point(45, 270);
            this.lblEmailLabel.BackColor = Color.Transparent;

            this.pnlEmail = new Panel();
            this.pnlEmail.Size = new Size(340, 40);
            this.pnlEmail.Location = new Point(45, 292);
            this.pnlEmail.BackColor = Color.FromArgb(25, 40, 75);

            this.txtEmail = new TextBox();
            this.txtEmail.Font = new Font("Segoe UI", 11F);
            this.txtEmail.Size = new Size(320, 28);
            this.txtEmail.Location = new Point(10, 7);
            this.txtEmail.BackColor = Color.FromArgb(25, 40, 75);
            this.txtEmail.ForeColor = Color.White;
            this.txtEmail.BorderStyle = BorderStyle.None;
            this.txtEmail.MaxLength = 100;
            this.pnlEmail.Controls.Add(this.txtEmail);

            // ==========================================
            // TELEFON
            // ==========================================
            this.lblPhoneLabel = new Label();
            this.lblPhoneLabel.Text = "📱  Telefon";
            this.lblPhoneLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblPhoneLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblPhoneLabel.Size = new Size(340, 20);
            this.lblPhoneLabel.Location = new Point(45, 340);
            this.lblPhoneLabel.BackColor = Color.Transparent;

            this.pnlPhone = new Panel();
            this.pnlPhone.Size = new Size(340, 40);
            this.pnlPhone.Location = new Point(45, 362);
            this.pnlPhone.BackColor = Color.FromArgb(25, 40, 75);

            this.txtPhone = new TextBox();
            this.txtPhone.Font = new Font("Segoe UI", 11F);
            this.txtPhone.Size = new Size(320, 28);
            this.txtPhone.Location = new Point(10, 7);
            this.txtPhone.BackColor = Color.FromArgb(25, 40, 75);
            this.txtPhone.ForeColor = Color.White;
            this.txtPhone.BorderStyle = BorderStyle.None;
            this.txtPhone.MaxLength = 20;
            this.pnlPhone.Controls.Add(this.txtPhone);

            // ==========================================
            // ŞİFRE
            // ==========================================
            this.lblPasswordLabel = new Label();
            this.lblPasswordLabel.Text = "🔒  Şifre";
            this.lblPasswordLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblPasswordLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblPasswordLabel.Size = new Size(340, 20);
            this.lblPasswordLabel.Location = new Point(45, 410);
            this.lblPasswordLabel.BackColor = Color.Transparent;

            this.pnlPassword = new Panel();
            this.pnlPassword.Size = new Size(340, 40);
            this.pnlPassword.Location = new Point(45, 432);
            this.pnlPassword.BackColor = Color.FromArgb(25, 40, 75);

            this.txtRegPassword = new TextBox();
            this.txtRegPassword.Font = new Font("Segoe UI", 11F);
            this.txtRegPassword.Size = new Size(320, 28);
            this.txtRegPassword.Location = new Point(10, 7);
            this.txtRegPassword.BackColor = Color.FromArgb(25, 40, 75);
            this.txtRegPassword.ForeColor = Color.White;
            this.txtRegPassword.BorderStyle = BorderStyle.None;
            this.txtRegPassword.PasswordChar = '●';
            this.txtRegPassword.MaxLength = 50;
            this.pnlPassword.Controls.Add(this.txtRegPassword);

            // ==========================================
            // ROL SEÇİMİ
            // ==========================================
            this.lblRoleLabel = new Label();
            this.lblRoleLabel.Text = "📌  Rol Seçiniz";
            this.lblRoleLabel.Font = new Font("Segoe UI", 9.5F);
            this.lblRoleLabel.ForeColor = Color.FromArgb(160, 170, 200);
            this.lblRoleLabel.Size = new Size(340, 20);
            this.lblRoleLabel.Location = new Point(45, 480);
            this.lblRoleLabel.BackColor = Color.Transparent;

            this.cmbRegRole = new ComboBox();
            this.cmbRegRole.Font = new Font("Segoe UI", 11F);
            this.cmbRegRole.Size = new Size(340, 32);
            this.cmbRegRole.Location = new Point(45, 502);
            this.cmbRegRole.BackColor = Color.FromArgb(25, 40, 75);
            this.cmbRegRole.ForeColor = Color.White;
            this.cmbRegRole.FlatStyle = FlatStyle.Flat;
            this.cmbRegRole.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbRegRole.Items.AddRange(new object[] { "Rol Seçiniz", "Resepsiyonist", "Muhasebe" });
            this.cmbRegRole.SelectedIndex = 0;

            // ==========================================
            // KAYIT OL BUTONU
            // ==========================================
            this.btnRegister = new Button();
            this.btnRegister.Text = "KAYIT OL";
            this.btnRegister.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            this.btnRegister.Size = new Size(340, 48);
            this.btnRegister.Location = new Point(45, 545);
            this.btnRegister.FlatStyle = FlatStyle.Flat;
            this.btnRegister.FlatAppearance.BorderSize = 0;
            this.btnRegister.BackColor = Color.FromArgb(218, 165, 32);
            this.btnRegister.ForeColor = Color.White;
            this.btnRegister.Cursor = Cursors.Hand;

            // ==========================================
            // DURUM + GERİ DÖN LİNKİ
            // ==========================================
            this.lblRegStatus = new Label();
            this.lblRegStatus.Text = "";
            this.lblRegStatus.Font = new Font("Segoe UI", 9F);
            this.lblRegStatus.ForeColor = Color.FromArgb(248, 113, 113);
            this.lblRegStatus.TextAlign = ContentAlignment.MiddleCenter;
            this.lblRegStatus.Size = new Size(340, 20);
            this.lblRegStatus.Location = new Point(45, 598);
            this.lblRegStatus.BackColor = Color.Transparent;

            // ==========================================
            // KARTA KONTROLLERİ EKLE
            // ==========================================
            this.pnlCard.Controls.Add(this.lblLogoIcon);
            this.pnlCard.Controls.Add(this.lblTitle);
            this.pnlCard.Controls.Add(this.lblSubTitle);
            this.pnlCard.Controls.Add(this.lblFullNameLabel);
            this.pnlCard.Controls.Add(this.pnlFullName);
            this.pnlCard.Controls.Add(this.lblUsernameLabel);
            this.pnlCard.Controls.Add(this.pnlUsername);
            this.pnlCard.Controls.Add(this.lblEmailLabel);
            this.pnlCard.Controls.Add(this.pnlEmail);
            this.pnlCard.Controls.Add(this.lblPhoneLabel);
            this.pnlCard.Controls.Add(this.pnlPhone);
            this.pnlCard.Controls.Add(this.lblPasswordLabel);
            this.pnlCard.Controls.Add(this.pnlPassword);
            this.pnlCard.Controls.Add(this.lblRoleLabel);
            this.pnlCard.Controls.Add(this.cmbRegRole);
            this.pnlCard.Controls.Add(this.btnRegister);
            this.pnlCard.Controls.Add(this.lblRegStatus);

            this.Controls.Add(this.pnlCard);
        }

        #endregion

        private Panel pnlCard;
        private Label lblLogoIcon;
        private Label lblTitle;
        private Label lblSubTitle;

        private Label lblFullNameLabel;
        private Panel pnlFullName;
        private TextBox txtFullName;

        private Label lblUsernameLabel;
        private Panel pnlUsername;
        private TextBox txtRegUsername;

        private Label lblEmailLabel;
        private Panel pnlEmail;
        private TextBox txtEmail;

        private Label lblPhoneLabel;
        private Panel pnlPhone;
        private TextBox txtPhone;

        private Label lblPasswordLabel;
        private Panel pnlPassword;
        private TextBox txtRegPassword;

        private Label lblRoleLabel;
        private ComboBox cmbRegRole;

        private Button btnRegister;
        private Label lblRegStatus;
    }
}
