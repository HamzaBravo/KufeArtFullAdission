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

                Application.Run(mainForm);
            }
        }
    }
}