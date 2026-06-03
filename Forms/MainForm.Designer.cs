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

            // NOTIFICATION BAR (top stripe - full width, sits above everything)
            this.pnlNotify = new Panel {
                Dock = DockStyle.Top, Height = 44,
                BackColor = Color.FromArgb(10, 40, 90), Visible = false, Padding = new Padding(10, 0, 10, 0)
            };

            // LEFT SIDEBAR (vertical navigation - 250px)
            this.pnlHeader = new Panel {
                Dock = DockStyle.Left, Width = 250,
                BackColor = Color.FromArgb(10, 14, 26),
            };

            this.lblLogo = new Label {
                Text = "👑 AFM GRAND", Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(218, 165, 32), AutoSize = false,
                Size = new Size(250, 55), Location = new Point(0, 14),
                TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent
            };

            // Nav buttons container inside sidebar
            this.pnlNavbar = new Panel {
                BackColor = Color.Transparent, Location = new Point(0, 145),
                Size = new Size(250, 680)
            };

            this.btnNavDashboard   = MakeNavBtn("📊  Dashboard",     0);
            this.btnNavMisafirler  = MakeNavBtn("👥  Misafirler",    1);
            this.btnNavOdalar      = MakeNavBtn("🛏   Odalar",        2);
            this.btnNavRezervasyon = MakeNavBtn("📅  Rezervasyon",   3);
            this.btnNavOdeme       = MakeNavBtn("💳  Ödeme",          4);
            this.btnNavPersonel    = MakeNavBtn("🧑  Personel",       5);
            this.btnNavRaporlar    = MakeNavBtn("📈  Raporlar",       6);
            this.btnNavCikis       = MakeNavBtn("🚪  Çıkış",          7);

            this.pnlNavbar.Controls.AddRange(new Control[] {
                btnNavDashboard, btnNavMisafirler, btnNavOdalar,
                btnNavRezervasyon, btnNavOdeme, btnNavPersonel,
                btnNavRaporlar, btnNavCikis
            });

            this.pnlHeader.Controls.AddRange(new Control[] { lblLogo, pnlNavbar });

            // MAIN CONTENT (fills remaining right area)
            this.pnlMainContent = new Panel {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(11, 15, 28),
                AutoScroll = true, Padding = new Padding(20)
            };

            // FORM SETUP
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(1400, 900);
            // Dock order: Fill first, then Left, then Top
            this.Controls.Add(this.pnlMainContent);
            this.Controls.Add(this.pnlHeader);
            this.Controls.Add(this.pnlNotify);
            this.Name = "MainForm";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "AFM Grand Hotel | Yönetim Paneli";
            this.BackColor = Color.FromArgb(11, 15, 28);
            this.WindowState = FormWindowState.Maximized;
            this.ResumeLayout(false);
        }

        private Button MakeNavBtn(string text, int idx)
        {
            return new Button
            {
                Text = text, Font = new Font("Segoe UI", 10F),
                Size = new Size(250, 48), Location = new Point(0, idx * 50),
                FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(160, 175, 205), Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(22, 0, 0, 0),
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(20, 35, 65) }
            };
        }
        #endregion

        private Panel pnlHeader, pnlNavbar, pnlMainContent, pnlNotify;
        private Label lblLogo;
        private Button btnNavDashboard, btnNavMisafirler, btnNavOdalar,
                       btnNavRezervasyon, btnNavOdeme, btnNavPersonel,
                       btnNavRaporlar, btnNavCikis;
    }
}
