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

        this.FormClosing += MainForm_FormClosing;
        this.Resize += MainForm_Resize;

    }


    private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
    {
     
        if (_isReallyClosing)
        {
            notifyIcon1.Visible = false;
            notifyIcon1.Dispose();
            return;
        }

        e.Cancel = true;
        this.Hide();
        this.ShowInTaskbar = false;
        notifyIcon1.Visible = true;

        if (!_hasShownTrayNotification)
        {
            notifyIcon1.ShowBalloonTip(3000,
                "KufeArt Yaz�c� Y�neticisi",
                "Uygulama arka planda �al��maya devam ediyor. A�mak i�in tray ikonuna �ift t�klay�n.",
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
            notifyIcon1.Icon = SystemIcons.Application;
        }
        catch
        {
            notifyIcon1.Icon = SystemIcons.Information;
        }

        notifyIcon1.Text = "KufeArt Yaz�c� Y�neticisi - Arka Planda �al���yor";
        notifyIcon1.Visible = false; 
    }

    private async void MainForm_Load(object sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
        LoadSystemPrinters();
        LoadSavedSettings();
        UpdateAssignedPrintersView();
        UpdateStatus("Form y�klendi - Yaz�c�lar listelendi");

        if (HasConfiguredPrinters())
        {
            await InitializeBackgroundServicesAsync();
        }
    }


    #region Mevcut ��levsellik (De�i�meden korundu)
    private void LoadSystemPrinters()
    {
        try
        {
            listBoxPrinters.Items.Clear();

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
            MessageBox.Show("L�tfen bir yaz�c� se�in!", "Uyar�",MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            UpdateStatus($"{printerName} yaz�c�s� {printerType} olarak kaydedildi");

            if (HasConfiguredPrinters() && !_backgroundServicesStarted)
            {
                await InitializeBackgroundServicesAsync();
            }

            MessageBox.Show($"? {printerName} yaz�c�s� ba�ar�yla kaydedildi!\n\nT�r: {printerType}\nDurum: {(isEnabled ? "Aktif" : "Pasif")}", "Ba�ar�l�", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Yaz�c� kaydedilirken hata olu�tu:\n{ex.Message}","Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            if (HasConfiguredPrinters())
            {
                await RestartBackgroundServicesAsync();
            }

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
    #endregion

    #region Background Services (YEN�)
    private bool HasConfiguredPrinters()
    {
        return _printerConfig.Printers.Any(p => p.IsEnabled && (p.Type == "Mutfak" || p.Type == "Bar"));
    }

    private async Task InitializeBackgroundServicesAsync()
    {
        try
        {
            if (_backgroundServicesStarted) return;

            UpdateStatus("?? Background servisler ba�lat�l�yor...");

            _printingService = new PrintingService();
            _printingService.LogMessageReceived += OnServiceLogMessage;
            _printingService.PrintCompleted += OnPrintCompleted;
            _printingService.LoadPrinterConfig(); 

            _signalRService = new SignalRClientService();
            _signalRService.LogMessageReceived += OnServiceLogMessage;
            _signalRService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _signalRService.OrderReceived += OnOrderReceived; 

            await _signalRService.ConnectAsync();

            _backgroundServicesStarted = true;
            UpdateStatus("? Background servisler aktif - Sipari�ler bekleniyor");

            this.Text = "??? KufeArt Yaz�c� Y�neticisi - ?? Aktif";
        }
        catch (Exception ex)
        {
            UpdateStatus($"? Background servis hatas�: {ex.Message}");
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
            UpdateStatus($"Servis yeniden ba�latma hatas�: {ex.Message}");
        }
    }

    private async void OnOrderReceived(OrderNotificationModel order)
    {
        try
        {
            if (InvokeRequired)
            {
                Invoke(new Action<OrderNotificationModel>(OnOrderReceived), order);
                return;
            }

            UpdateStatus($"?? Sipari� al�nd�: {order.TableName} - {order.WaiterName}");
            lblTitle.ForeColor = Color.Orange; 

            await _printingService!.ProcessOrderAsync(order);

            UpdateStatus($"? Yazd�rma tamamland�: {order.TableName}");
            lblTitle.ForeColor = Color.Green;

            await Task.Delay(2000);
            lblTitle.ForeColor = Color.DarkBlue;
        }
        catch (Exception ex)
        {
            UpdateStatus($"? Sipari� i�leme hatas�: {ex.Message}");
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
        var statusText = isConnected ? "Ba�l�" : "Ba�lant� Yok";

        this.Text = $"??? KufeArt Yaz�c� Y�neticisi - {statusIcon} {statusText}";
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

        Console.WriteLine(message);

        if (message.Contains("Sipari�") || message.Contains("Ba�lant�") || message.Contains("Yazd�r"))
        {
            UpdateStatus(message.Replace("[", "").Replace("]", "").Substring(9)); 
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
}