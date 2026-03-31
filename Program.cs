using System;
using System.Windows.Forms;
using ORYS.Forms;

namespace ORYS
{
    internal static class Program
    {
        /// <summary>
        /// Uygulamanın ana giriş noktası
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Global hata yakalama
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(
                    $"Hata oluştu:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                    "ORYS - Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                MessageBox.Show(
                    $"Kritik hata:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                    "ORYS - Kritik Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            try
            {
                Application.Run(new LoginForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Uygulama hatası:\n\n{ex.Message}\n\n{ex.StackTrace}",
                    "ORYS - Uygulama Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}