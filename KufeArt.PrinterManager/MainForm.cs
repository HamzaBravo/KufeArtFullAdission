using KufeArt.PrinterManager.Models;
using Newtonsoft.Json;
using System.Drawing.Printing; // ? Bu System.Drawing.Printing.PrinterSettings i�in

namespace KufeArt.PrinterManager
{
    public partial class MainForm : Form
    {
        // ? DE����KL�K: _printerSettings ? _printerConfig
        private PrinterManagerConfig _printerConfig;
        private string _configFilePath;

        public MainForm()
        {
            InitializeComponent();
            // ? DE����KL�K: PrinterSettings ? PrinterManagerConfig
            _printerConfig = new PrinterManagerConfig();

            // Konfig�rasyon dosyas� yolu
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
            UpdateStatus("Form y�klendi - Yaz�c�lar listelendi");
        }

        private void LoadSystemPrinters()
        {
            try
            {
                listBoxPrinters.Items.Clear();

                // ? System.Drawing.Printing.PrinterSettings kullan�m�
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    listBoxPrinters.Items.Add(printerName);
                }

                UpdateStatus($"{listBoxPrinters.Items.Count} yaz�c� bulundu");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yaz�c�lar y�klenirken hata olu�tu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Yaz�c� y�kleme hatas�!");
            }
        }

        private void LoadPrinterSettings(string printerName)
        {
            lblPrinterName.Text = printerName;

            // ? DE����KL�K: _printerSettings ? _printerConfig
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
                MessageBox.Show("L�tfen bir yaz�c� se�in!", "Uyar�",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string printerName = listBoxPrinters.SelectedItem.ToString()!;
                string printerType = GetSelectedPrinterType();
                bool isEnabled = chkEnabled.Checked;

                // ? DE����KL�K: _printerSettings ? _printerConfig
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
                UpdateStatus($"{printerName} yaz�c�s� {printerType} olarak kaydedildi");

                MessageBox.Show($"? {printerName} yaz�c�s� ba�ar�yla kaydedildi!\n\nT�r: {printerType}\nDurum: {(isEnabled ? "Aktif" : "Pasif")}",
                              "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yaz�c� kaydedilirken hata olu�tu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PrintTestPage(string printerName, string printerType)
        {
            // ? System.Drawing.Printing.PrintDocument kullan�m�
            using var printDoc = new PrintDocument();

            // ? System.Drawing.Printing.PrinterSettings kullan�m�
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
                    $"Yaz�c� Ad�: {printerName}",
                    $"Atanan T�r: {printerType}",
                    $"Test Tarihi: {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                    "",
                    "Bu bir test sayfas�d�r.",
                    "Yaz�c� d�zg�n �al���yorsa",
                    "bu metni g�rebilmelisiniz.",
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
            // ? DE����KL�K: _printerSettings ? _printerConfig
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
                MessageBox.Show("? T�m ayarlar ba�ar�yla kaydedildi!\n\n" +
                              $"?? Konum: {_configFilePath}\n" +
                              $"?? Toplam: {_printerConfig.Printers.Count} yaz�c�\n" +
                              $"??? Mutfak: {_printerConfig.Printers.Count(p => p.Type == "Mutfak")} yaz�c�\n" +
                              $"?? Bar: {_printerConfig.Printers.Count(p => p.Type == "Bar")} yaz�c�",
                              "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateStatus("T�m ayarlar JSON dosyas�na kaydedildi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar kaydedilirken hata olu�tu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveSettingsToFile()
        {
            // ? DE����KL�K: _printerSettings ? _printerConfig
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
                // ? DE����KL�K: PrinterSettings ? PrinterManagerConfig
                var loadedSettings = JsonConvert.DeserializeObject<PrinterManagerConfig>(json);

                if (loadedSettings != null)
                {
                    // ? DE����KL�K: _printerSettings ? _printerConfig
                    _printerConfig = loadedSettings;
                }
            }
        }

        // Di�er metodlar ayn� kal�yor...
        private string GetSelectedPrinterType()
        {
            if (rbMutfak.Checked) return "Mutfak";
            if (rbBar.Checked) return "Bar";
            return "Atanmam��";
        }

        private void btnRefreshPrinters_Click(object sender, EventArgs e)
        {
            btnRefreshPrinters.Enabled = false;
            btnRefreshPrinters.Text = "Y�kleniyor...";

            try
            {
                LoadSystemPrinters();
                UpdateAssignedPrintersView();
                UpdateStatus("Yaz�c� listesi yenilendi");
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
                UpdateStatus($"Se�ili yaz�c�: {selectedPrinter}");
            }
        }

        private void btnTestPrint_Click(object sender, EventArgs e)
        {
            if (listBoxPrinters.SelectedItem == null)
            {
                MessageBox.Show("L�tfen bir yaz�c� se�in!", "Uyar�",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string printerName = listBoxPrinters.SelectedItem.ToString()!;
                string printerType = GetSelectedPrinterType();

                PrintTestPage(printerName, printerType);
                UpdateStatus($"{printerName} yaz�c�s�na test sayfas� g�nderildi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test yazd�rma hatas�:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLoadSettings_Click(object sender, EventArgs e)
        {
            try
            {
                LoadSavedSettings();
                UpdateAssignedPrintersView();

                MessageBox.Show("? Ayarlar ba�ar�yla y�klendi!",
                              "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);

                UpdateStatus("Kaydedilmi� ayarlar y�klendi");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ayarlar y�klenirken hata olu�tu:\n{ex.Message}",
                              "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
        }
    }
}