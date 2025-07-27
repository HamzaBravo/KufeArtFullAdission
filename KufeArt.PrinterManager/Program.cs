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

                Application.Run(mainForm);
            }
        }
    }
}