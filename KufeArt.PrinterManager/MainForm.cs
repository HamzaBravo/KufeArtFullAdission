using Microsoft.Win32;
using KufeArt.PrinterManager.Models;
using KufeArt.PrinterManager.Services;
using Newtonsoft.Json;
using System.Drawing.Printing;

namespace KufeArt.PrinterManager;

public partial class MainForm : Form
{
    #region Mevcut Alanlar (Korundu)
    private PrinterManagerConfig _printerConfig;
    private string _configFilePath;
    #endregion

    #region Yeni Alanlar (Background Services)
    private SignalRClientService? _signalRService;
    private PrintingService? _printingService;
    private bool _backgroundServicesStarted = false;

    private bool _isReallyClosing = false;
    private bool _hasShownTrayNotification = false;

    #endregion

    public MainForm()
    {
        InitializeComponent();
        _printerConfig = new PrinterManagerConfig();

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appDataPath, "KufeArt", "PrinterManager");
        Directory.CreateDirectory(configDir);
        _configFilePath = Path.Combine(configDir, "printer-settings.json");

        SetupTrayIcon();

        // Event'leri baðla
        this.FormClosing += MainForm_FormClosing;
        this.Resize += MainForm_Resize;


        if (!IsAutoStartEnabled())
        {
            var result = MessageBox.Show(
                "KufeArt Yazýcý Yöneticisi'ni Windows ile birlikte baþlatmak ister misiniz?",
                "Otomatik Baþlatma",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                EnableAutoStart();
            }
        }
    }

    private bool IsAutoStartEnabled()
    {
        try
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);

            if (rk != null)
            {
                object value = rk.GetValue("KufeArtPrinterManager");
                rk.Close();
                return value != null;
            }
        }
        catch
        {
            // Hata durumunda false döndür
        }

        return false;
    }
    private void DisableAutoStart()
    {
        try
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk != null)
            {
                rk.DeleteValue("KufeArtPrinterManager", false);
                rk.Close();

                MessageBox.Show("Otomatik baþlatma devre dýþý býrakýldý!", "Baþarýlý",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Otomatik baþlatma kaldýrýlamadý: {ex.Message}", "Hata",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void EnableAutoStart()
    {
        try
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk != null)
            {
                string appPath = Application.ExecutablePath + " --minimized";
                rk.SetValue("KufeArtPrinterManager", appPath);
                rk.Close();

                MessageBox.Show("Otomatik baþlatma etkinleþtirildi!", "Baþarýlý",
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Otomatik baþlatma ayarlanamadý: {ex.Message}", "Hata",
                          MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        // Eðer gerçekten kapatýlmak isteniyorsa
        if (_isReallyClosing)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            return;
        }

        // X butonuna basýldýðýnda arka plana gönder
        e.Cancel = true;
        this.Hide();
        this.ShowInTaskbar = false;
        notifyIcon1.Visible = true;

        // Kullanýcýya bilgi ver (sadece ilk kez)
        if (!_hasShownTrayNotification)
        {
            notifyIcon1.ShowBalloonTip(3000,
                "KufeArt Yazýcý Yöneticisi",
                "Uygulama arka planda çalýþmaya devam ediyor. Açmak için tray ikonuna çift týklayýn.",
                ToolTipIcon.Info);
            _hasShownTrayNotification = true;
        }
    }

    private void MainForm_Resize(object sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Minimized)
        {
            this.Hide();
            this.ShowInTaskbar = false;
            notifyIcon1.Visible = true;
        }
    }

    private void SetupTrayIcon()
    {
        try
        {
            // Icon ayarla (varsayýlan sistem ikonu)
            notifyIcon1.Icon = SystemIcons.Application;
        }
        catch
        {
            notifyIcon1.Icon = SystemIcons.Information;
        }

        notifyIcon1.Text = "KufeArt Yazýcý Yöneticisi - Arka Planda Çalýþýyor";
        notifyIcon1.Visible = false; // Baþlangýçta gizli
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        // Mevcut iþlevsellik (Korundu)
        LoadSystemPrinters();
        LoadSavedSettings();
        UpdateAssignedPrintersView();
        UpdateStatus("Form yüklendi - Yazýcýlar listelendi");

        // ?? YENÝ: Background services baþlat (yazýcý ayarlarý varsa)
        if (HasConfiguredPrinters())
        {
            await InitializeBackgroundServicesAsync();
        }
    }


    #region Mevcut Ýþlevsellik (Deðiþmeden korundu)
    private void LoadSystemPrinters()
    {
        try
        {
            listBoxPrinters.Items.Clear();

            // System.Drawing.Printing.PrinterSettings kullanýmý
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

    private async void btnSavePrinter_Click(object sender, EventArgs e)
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

            // ?? YENÝ: Background services'i baþlat/güncelle
            if (HasConfiguredPrinters() && !_backgroundServicesStarted)
            {
                await InitializeBackgroundServicesAsync();
            }

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
        using var printDoc = new PrintDocument();

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

    private async void btnSaveAll_Click(object sender, EventArgs e)
    {
        try
        {
            SaveSettingsToFile();

            // ?? YENÝ: Background services'i yeniden baþlat
            if (HasConfiguredPrinters())
            {
                await RestartBackgroundServicesAsync();
            }

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
            var loadedSettings = JsonConvert.DeserializeObject<PrinterManagerConfig>(json);

            if (loadedSettings != null)
            {
                _printerConfig = loadedSettings;
            }
        }
    }

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
    #endregion

    #region Background Services (YENÝ)
    private bool HasConfiguredPrinters()
    {
        return _printerConfig.Printers.Any(p => p.IsEnabled && (p.Type == "Mutfak" || p.Type == "Bar"));
    }

    private async Task InitializeBackgroundServicesAsync()
    {
        try
        {
            if (_backgroundServicesStarted) return;

            UpdateStatus("?? Background servisler baþlatýlýyor...");

            // PrintingService'i baþlat
            _printingService = new PrintingService();
            _printingService.LogMessageReceived += OnServiceLogMessage;
            _printingService.PrintCompleted += OnPrintCompleted;
            _printingService.LoadPrinterConfig(); // Mevcut config'i yükle

            // SignalR Service'i baþlat
            _signalRService = new SignalRClientService();
            _signalRService.LogMessageReceived += OnServiceLogMessage;
            _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _signalRService.OrderReceived += OnOrderReceived; // ?? Ana event

            // SignalR'ý baðla
            await _signalRService.ConnectAsync();

            _backgroundServicesStarted = true;
            UpdateStatus("? Background servisler aktif - Sipariþler bekleniyor");

            // Title'ý güncelle
            this.Text = "??? KufeArt Yazýcý Yöneticisi - ?? Aktif";
        }
        catch (Exception ex)
        {
            UpdateStatus($"? Background servis hatasý: {ex.Message}");
            _backgroundServicesStarted = false;
        }
    }

    private async Task RestartBackgroundServicesAsync()
    {
        try
        {
            if (_signalRService != null)
            {
                await _signalRService.DisconnectAsync();
            }

            _backgroundServicesStarted = false;
            await InitializeBackgroundServicesAsync();
        }
        catch (Exception ex)
        {
            UpdateStatus($"Servis yeniden baþlatma hatasý: {ex.Message}");
        }
    }

    // ?? EN ÖNEMLÝ METHOD - Sipariþ geldiðinde otomatik yazdýr
    private async void OnOrderReceived(OrderNotificationModel order)
    {
        try
        {
            if (InvokeRequired)
            {
                Invoke(new Action<OrderNotificationModel>(OnOrderReceived), order);
                return;
            }

            UpdateStatus($"?? Sipariþ alýndý: {order.TableName} - {order.WaiterName}");
            lblTitle.ForeColor = Color.Orange; // Görsel feedback

            // ??? Otomatik yazdýr
            await _printingService!.ProcessOrderAsync(order);

            UpdateStatus($"? Yazdýrma tamamlandý: {order.TableName}");
            lblTitle.ForeColor = Color.Green;

            // 2 saniye sonra normal renge dön
            await Task.Delay(2000);
            lblTitle.ForeColor = Color.DarkBlue;
        }
        catch (Exception ex)
        {
            UpdateStatus($"? Sipariþ iþleme hatasý: {ex.Message}");
            lblTitle.ForeColor = Color.Red;
        }
    }

    private void OnConnectionStatusChanged(bool isConnected)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<bool>(OnConnectionStatusChanged), isConnected);
            return;
        }

        var statusIcon = isConnected ? "??" : "??";
        var statusText = isConnected ? "Baðlý" : "Baðlantý Yok";

        this.Text = $"??? KufeArt Yazýcý Yöneticisi - {statusIcon} {statusText}";
        UpdateStatus($"SignalR: {statusText}");
    }

    private void OnPrintCompleted(PrintLogModel printLog)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<PrintLogModel>(OnPrintCompleted), printLog);
            return;
        }

        var status = printLog.Status == "Success" ? "?" : "?";
        UpdateStatus($"{status} {printLog.PrinterName}: {printLog.TableName}");
    }

    private void OnServiceLogMessage(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(OnServiceLogMessage), message);
            return;
        }

        // Console'a log yaz (debug için)
        Console.WriteLine(message);

        // Status'a önemli mesajlarý göster
        if (message.Contains("Sipariþ") || message.Contains("Baðlantý") || message.Contains("Yazdýr"))
        {
            UpdateStatus(message.Replace("[", "").Replace("]", "").Substring(9)); // Timestamp'i temizle
        }
    }
    #endregion

    private void UpdateStatus(string message)
    {
        lblStatus.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
    }

    private void ShowForm()
    {
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.ShowInTaskbar = true;
        this.BringToFront();
        this.Activate();
        notifyIcon1.Visible = false;
    }

    private void notifyIcon1_DoubleClick(object sender, EventArgs e)
    {
        ShowForm();
    }

    private void contextMenuStrip1_Click(object sender, EventArgs e)
    {
        ShowForm();
    }

    private void toolStripTextBox2_Click(object sender, EventArgs e)
    {
        _isReallyClosing = true;
        Application.Exit();
    }

    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
        if (checkBox1.Checked)
            EnableAutoStart();
        
        else
            DisableAutoStart();
        
    }
}