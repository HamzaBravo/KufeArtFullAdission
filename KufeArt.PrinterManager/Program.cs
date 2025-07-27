namespace KufeArt.PrinterManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // Tek instance kontrol� (opsiyonel)
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "KufeArtPrinterManager", out createdNew))
            {
                if (!createdNew)
                {
                    // Uygulama zaten �al���yor
                    MessageBox.Show("KufeArt Yaz�c� Y�neticisi zaten �al���yor!","Uyar�",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                    return;
                }

                var mainForm = new MainForm();

                // Komut sat�r� arg�manlar�n� kontrol et
                var args = Environment.GetCommandLineArgs();
                bool startMinimized = args.Contains("--minimized") ||
                                    args.Contains("/minimized") ||
                                    args.Contains("-minimized");

                if (startMinimized)
                {
                    // Ba�lang��ta tray'de ba�lat
                    mainForm.WindowState = FormWindowState.Minimized;
                    mainForm.ShowInTaskbar = false;
                    mainForm.Visible = false;

                    // Form'u g�stermeden application context'i �al��t�r
                    Application.Run();
                }
                else
                {
                    // Normal ba�latma
                    Application.Run(mainForm);
                }
            }
        }
    }
}