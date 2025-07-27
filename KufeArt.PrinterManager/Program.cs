namespace KufeArt.PrinterManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Tek instance kontrolü (opsiyonel)
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "KufeArtPrinterManager", out createdNew))
            {
                if (!createdNew)
                {
                    // Uygulama zaten çalýþýyor
                    MessageBox.Show("KufeArt Yazýcý Yöneticisi zaten çalýþýyor!","Uyarý",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }

                var mainForm = new MainForm();

                // Komut satýrý argümanlarýný kontrol et
                var args = Environment.GetCommandLineArgs();
                bool startMinimized = args.Contains("--minimized") ||
                                    args.Contains("/minimized") ||
                                    args.Contains("-minimized");

                if (startMinimized)
                {
                    // Baþlangýçta tray'de baþlat
                    mainForm.WindowState = FormWindowState.Minimized;
                    mainForm.ShowInTaskbar = false;
                    mainForm.Visible = false;

                    // Form'u göstermeden application context'i çalýþtýr
                    Application.Run();
                }
                else
                {
                    // Normal baþlatma
                    Application.Run(mainForm);
                }
            }
        }
    }
}