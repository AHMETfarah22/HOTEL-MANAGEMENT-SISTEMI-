namespace ORYS.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();

            // HEADER
            this.pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(18, 25, 45), Padding = new Padding(10, 0, 10, 0) };
            this.lblLogo = new Label { Text = "🏨 AFM GRAND", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(218, 165, 32), AutoSize = true, Location = new Point(15, 22), BackColor = Color.Transparent };

            // NAVBAR - 7 buton
            this.pnlNavbar = new Panel { BackColor = Color.Transparent, Height = 70, Location = new Point(220, 0), Size = new Size(1050, 70) };
            this.btnNavDashboard = MakeNavBtn("📊 Dashboard", 0);
            this.btnNavMisafirler = MakeNavBtn("👥 Misafirler", 1);
            this.btnNavOdalar = MakeNavBtn("🛏️ Odalar", 2);
            this.btnNavRezervasyon = MakeNavBtn("📅 Rezervasyon", 3);
            this.btnNavOdeme = MakeNavBtn("💳 Ödeme", 4);
            this.btnNavPersonel = MakeNavBtn("🧑‍💼 Personel", 5);
            this.btnNavRaporlar = MakeNavBtn("📈 Raporlar", 6);
            this.btnNavCikis = MakeNavBtn("🚪 Çıkış", 7);
            this.pnlNavbar.Controls.AddRange(new Control[] { btnNavDashboard, btnNavMisafirler, btnNavOdalar, btnNavRezervasyon, btnNavOdeme, btnNavPersonel, btnNavRaporlar, btnNavCikis });
            this.pnlHeader.Controls.AddRange(new Control[] { pnlNavbar, lblLogo });

            // MAIN CONTENT
            this.pnlMainContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(12, 18, 35), AutoScroll = true, Padding = new Padding(20) };

            // FORM
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1200, 800);
            this.Controls.Add(this.pnlMainContent);
            this.Controls.Add(this.pnlHeader);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AFM Grand Hotel | Yönetim Paneli";
            this.BackColor = Color.FromArgb(12, 18, 35);
            this.WindowState = FormWindowState.Maximized;
            this.ResumeLayout(false);
        }

        private Button MakeNavBtn(string text, int idx)
        {
            return new Button
            {
                Text = text, Font = new Font("Segoe UI", 9.5F), Size = new Size(125, 40),
                Location = new Point(idx * 128, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 190, 210), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(40, 55, 85) }
            };
        }
        #endregion

        private Panel pnlHeader, pnlNavbar, pnlMainContent;
        private Label lblLogo;
        private Button btnNavDashboard, btnNavMisafirler, btnNavOdalar, btnNavRezervasyon, btnNavOdeme, btnNavPersonel, btnNavRaporlar, btnNavCikis;
    }
}
