using KufeArt.PrinterManager.Models;
using Newtonsoft.Json;
using System.Drawing.Printing; // ? Bu System.Drawing.Printing.PrinterSettings için

namespace KufeArt.PrinterManager
{
    public partial class MainForm : Form
    {
        // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
        private PrinterManagerConfig _printerConfig;
        private string _configFilePath;

        public MainForm()
        {
            InitializeComponent();
            // ? DEÐÝÞÝKLÝK: PrinterSettings ? PrinterManagerConfig
            _printerConfig = new PrinterManagerConfig();

            // Konfigürasyon dosyasý yolu
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "KufeArt", "PrinterManager");
            Directory.CreateDirectory(configDir);
            _configFilePath = Path.Combine(configDir, "printer-settings.json");
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadSystemPrinters();
            LoadSavedSettings();
            UpdateAssignedPrintersView();
            UpdateStatus("Form yüklendi - Yazýcýlar listelendi");
        }

        private void LoadSystemPrinters()
        {
            try
            {
                listBoxPrinters.Items.Clear();

                // ? System.Drawing.Printing.PrinterSettings kullanýmý
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    listBoxPrinters.Items.Add(printerName);
                }

                UpdateStatus($"{listBoxPrinters.Items.Count} yazýcý bulundu");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazýcýlar yüklenirken hata oluþtu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Yazýcý yükleme hatasý!");
            }
        }

        private void LoadPrinterSettings(string printerName)
        {
            lblPrinterName.Text = printerName;

            // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
            var existingConfig = _printerConfig.Printers.FirstOrDefault(p => p.Name == printerName);

            if (existingConfig != null)
            {
                switch (existingConfig.Type)
                {
                    case "Mutfak":
                        rbMutfak.Checked = true;
                        break;
                    case "Bar":
                        rbBar.Checked = true;
                        break;
                    default:
                        rbAtanmamis.Checked = true;
                        break;
                }
                chkEnabled.Checked = existingConfig.IsEnabled;
            }
            else
            {
                rbAtanmamis.Checked = true;
                chkEnabled.Checked = true;
            }
        }

        private void btnSavePrinter_Click(object sender, EventArgs e)
        {
            if (listBoxPrinters.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir yazýcý seçin!", "Uyarý",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string printerName = listBoxPrinters.SelectedItem.ToString()!;
                string printerType = GetSelectedPrinterType();
                bool isEnabled = chkEnabled.Checked;

                // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
                var existingConfig = _printerConfig.Printers.FirstOrDefault(p => p.Name == printerName);

                if (existingConfig != null)
                {
                    existingConfig.Type = printerType;
                    existingConfig.IsEnabled = isEnabled;
                    existingConfig.LastModified = DateTime.Now;
                }
                else
                {
                    _printerConfig.Printers.Add(new PrinterConfig
                    {
                        Name = printerName,
                        Type = printerType,
                        IsEnabled = isEnabled,
                        LastModified = DateTime.Now
                    });
                }

                UpdateAssignedPrintersView();
                UpdateStatus($"{printerName} yazýcýsý {printerType} olarak kaydedildi");

                MessageBox.Show($"? {printerName} yazýcýsý baþarýyla kaydedildi!\n\nTür: {printerType}\nDurum: {(isEnabled ? "Aktif" : "Pasif")}",
                              "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yazýcý kaydedilirken hata oluþtu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintTestPage(string printerName, string printerType)
        {
            // ? System.Drawing.Printing.PrintDocument kullanýmý
            using var printDoc = new PrintDocument();

            // ? System.Drawing.Printing.PrinterSettings kullanýmý
            printDoc.PrinterSettings.PrinterName = printerName;

            printDoc.PrintPage += (sender, e) =>
            {
                if (e.Graphics == null) return;

                var font = new Font("Courier New", 10);
                var brush = Brushes.Black;
                float y = 50;
                float lineHeight = font.GetHeight();

                var testContent = new[]
                {
                    "=============================",
                    "    KUFE ART TEST SAYFASI",
                    "=============================",
                    "",
                    $"Yazýcý Adý: {printerName}",
                    $"Atanan Tür: {printerType}",
                    $"Test Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    "",
                    "Bu bir test sayfasýdýr.",
                    "Yazýcý düzgün çalýþýyorsa",
                    "bu metni görebilmelisiniz.",
                    "",
                    "=============================",
                    "        TEST TAMAMLANDI",
                    "============================="
                };

                for (int i = 0; i < testContent.Length; i++)
                {
                    e.Graphics.DrawString(testContent[i], font, brush, 50, y + (i * lineHeight));
                }

                font.Dispose();
            };

            printDoc.Print();
        }

        private void UpdateAssignedPrintersView()
        {
            // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
            listBoxMutfak.Items.Clear();
            var kitchenPrinters = _printerConfig.Printers
                .Where(p => p.Type == "Mutfak" && p.IsEnabled)
                .Select(p => $"? {p.Name}");

            foreach (var printer in kitchenPrinters)
            {
                listBoxMutfak.Items.Add(printer);
            }

            listBoxBar.Items.Clear();
            var barPrinters = _printerConfig.Printers
                .Where(p => p.Type == "Bar" && p.IsEnabled)
                .Select(p => $"? {p.Name}");

            foreach (var printer in barPrinters)
            {
                listBoxBar.Items.Add(printer);
            }

            lblMutfak.Text = $"??? Mutfak ({listBoxMutfak.Items.Count}):";
            lblBar.Text = $"?? Bar ({listBoxBar.Items.Count}):";
        }

        private void btnSaveAll_Click(object sender, EventArgs e)
        {
            try
            {
                SaveSettingsToFile();
                MessageBox.Show("? Tüm ayarlar baþarýyla kaydedildi!\n\n" +
                              $"?? Konum: {_configFilePath}\n" +
                              $"?? Toplam: {_printerConfig.Printers.Count} yazýcý\n" +
                              $"??? Mutfak: {_printerConfig.Printers.Count(p => p.Type == "Mutfak")} yazýcý\n" +
                              $"?? Bar: {_printerConfig.Printers.Count(p => p.Type == "Bar")} yazýcý",
                              "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateStatus("Tüm ayarlar JSON dosyasýna kaydedildi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata oluþtu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettingsToFile()
        {
            // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
            _printerConfig.LastUpdate = DateTime.Now;
            _printerConfig.ConfigPath = _configFilePath;

            var json = JsonConvert.SerializeObject(_printerConfig, Formatting.Indented);
            File.WriteAllText(_configFilePath, json);
        }

        private void LoadSavedSettings()
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                // ? DEÐÝÞÝKLÝK: PrinterSettings ? PrinterManagerConfig
                var loadedSettings = JsonConvert.DeserializeObject<PrinterManagerConfig>(json);

                if (loadedSettings != null)
                {
                    // ? DEÐÝÞÝKLÝK: _printerSettings ? _printerConfig
                    _printerConfig = loadedSettings;
                }
            }
        }

        // Diðer metodlar ayný kalýyor...
        private string GetSelectedPrinterType()
        {
            if (rbMutfak.Checked) return "Mutfak";
            if (rbBar.Checked) return "Bar";
            return "Atanmamýþ";
        }

        private void btnRefreshPrinters_Click(object sender, EventArgs e)
        {
            btnRefreshPrinters.Enabled = false;
            btnRefreshPrinters.Text = "Yükleniyor...";

            try
            {
                LoadSystemPrinters();
                UpdateAssignedPrintersView();
                UpdateStatus("Yazýcý listesi yenilendi");
            }
            finally
            {
                btnRefreshPrinters.Enabled = true;
                btnRefreshPrinters.Text = "?? Yenile";
            }
        }

        private void listBoxPrinters_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxPrinters.SelectedItem != null)
            {
                string selectedPrinter = listBoxPrinters.SelectedItem.ToString()!;
                LoadPrinterSettings(selectedPrinter);
                UpdateStatus($"Seçili yazýcý: {selectedPrinter}");
            }
        }

        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            if (listBoxPrinters.SelectedItem == null)
            {
                MessageBox.Show("Lütfen bir yazýcý seçin!", "Uyarý",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string printerName = listBoxPrinters.SelectedItem.ToString()!;
                string printerType = GetSelectedPrinterType();

                PrintTestPage(printerName, printerType);
                UpdateStatus($"{printerName} yazýcýsýna test sayfasý gönderildi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test yazdýrma hatasý:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadSettings_Click(object sender, EventArgs e)
        {
            try
            {
                LoadSavedSettings();
                UpdateAssignedPrintersView();

                MessageBox.Show("? Ayarlar baþarýyla yüklendi!",
                              "Baþarýlý", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateStatus("Kaydedilmiþ ayarlar yüklendi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar yüklenirken hata oluþtu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
    }
}