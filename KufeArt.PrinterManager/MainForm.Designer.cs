using System.Windows.Forms;

namespace KufeArt.PrinterManager
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lblTitle = new Label();
            lblPrinters = new Label();
            listBoxPrinters = new ListBox();
            btnRefreshPrinters = new Button();
            grpSettings = new GroupBox();
            lblSelectedPrinter = new Label();
            lblPrinterName = new Label();
            rbMutfak = new RadioButton();
            rbBar = new RadioButton();
            rbAtanmamis = new RadioButton();
            chkEnabled = new CheckBox();
            btnSavePrinter = new Button();
            btnTestPrint = new Button();
            grpAssignedPrinters = new GroupBox();
            lblMutfak = new Label();
            listBoxMutfak = new ListBox();
            lblBar = new Label();
            listBoxBar = new ListBox();
            btnSaveAll = new Button();
            btnLoadSettings = new Button();
            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            notifyIcon1 = new NotifyIcon(components);
            contextMenuStrip1 = new ContextMenuStrip(components);
            toolStripTextBox1 = new ToolStripTextBox();
            toolStripSeparator1 = new ToolStripSeparator();
            toolStripTextBox2 = new ToolStripTextBox();
            grpSettings.SuspendLayout();
            grpAssignedPrinters.SuspendLayout();
            statusStrip.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.DarkBlue;
            lblTitle.Location = new Point(18, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(297, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "🖨️ KufeArt Yazıcı Yöneticisi";
            // 
            // lblPrinters
            // 
            lblPrinters.AutoSize = true;
            lblPrinters.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblPrinters.Location = new Point(18, 60);
            lblPrinters.Name = "lblPrinters";
            lblPrinters.Size = new Size(121, 19);
            lblPrinters.TabIndex = 1;
            lblPrinters.Text = "Sistem Yazıcıları:";
            // 
            // listBoxPrinters
            // 
            listBoxPrinters.FormattingEnabled = true;
            listBoxPrinters.ItemHeight = 15;
            listBoxPrinters.Location = new Point(18, 82);
            listBoxPrinters.Margin = new Padding(3, 2, 3, 2);
            listBoxPrinters.Name = "listBoxPrinters";
            listBoxPrinters.Size = new Size(263, 139);
            listBoxPrinters.TabIndex = 2;
            listBoxPrinters.SelectedIndexChanged += listBoxPrinters_SelectedIndexChanged;
            // 
            // btnRefreshPrinters
            // 
            btnRefreshPrinters.BackColor = Color.LightBlue;
            btnRefreshPrinters.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnRefreshPrinters.Location = new Point(18, 240);
            btnRefreshPrinters.Margin = new Padding(3, 2, 3, 2);
            btnRefreshPrinters.Name = "btnRefreshPrinters";
            btnRefreshPrinters.Size = new Size(122, 26);
            btnRefreshPrinters.TabIndex = 3;
            btnRefreshPrinters.Text = "🔄 Yenile";
            btnRefreshPrinters.UseVisualStyleBackColor = false;
            btnRefreshPrinters.Click += btnRefreshPrinters_Click;
            // 
            // grpSettings
            // 
            grpSettings.Controls.Add(lblSelectedPrinter);
            grpSettings.Controls.Add(lblPrinterName);
            grpSettings.Controls.Add(rbMutfak);
            grpSettings.Controls.Add(rbBar);
            grpSettings.Controls.Add(rbAtanmamis);
            grpSettings.Controls.Add(chkEnabled);
            grpSettings.Controls.Add(btnSavePrinter);
            grpSettings.Controls.Add(btnTestPrint);
            grpSettings.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grpSettings.Location = new Point(306, 60);
            grpSettings.Margin = new Padding(3, 2, 3, 2);
            grpSettings.Name = "grpSettings";
            grpSettings.Padding = new Padding(3, 2, 3, 2);
            grpSettings.Size = new Size(219, 210);
            grpSettings.TabIndex = 4;
            grpSettings.TabStop = false;
            grpSettings.Text = "Yazıcı Ayarları";
            // 
            // lblSelectedPrinter
            // 
            lblSelectedPrinter.AutoSize = true;
            lblSelectedPrinter.Font = new Font("Segoe UI", 9F);
            lblSelectedPrinter.Location = new Point(13, 22);
            lblSelectedPrinter.Name = "lblSelectedPrinter";
            lblSelectedPrinter.Size = new Size(69, 15);
            lblSelectedPrinter.TabIndex = 0;
            lblSelectedPrinter.Text = "Seçili Yazıcı:";
            // 
            // lblPrinterName
            // 
            lblPrinterName.AutoSize = true;
            lblPrinterName.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblPrinterName.ForeColor = Color.DarkGreen;
            lblPrinterName.Location = new Point(13, 41);
            lblPrinterName.Name = "lblPrinterName";
            lblPrinterName.Size = new Size(86, 15);
            lblPrinterName.TabIndex = 1;
            lblPrinterName.Text = "Yazıcı seçiniz...";
            // 
            // rbMutfak
            // 
            rbMutfak.AutoSize = true;
            rbMutfak.Font = new Font("Segoe UI", 9F);
            rbMutfak.Location = new Point(13, 68);
            rbMutfak.Margin = new Padding(3, 2, 3, 2);
            rbMutfak.Name = "rbMutfak";
            rbMutfak.Size = new Size(78, 19);
            rbMutfak.TabIndex = 2;
            rbMutfak.Text = "🍽️ Mutfak";
            rbMutfak.UseVisualStyleBackColor = true;
            // 
            // rbBar
            // 
            rbBar.AutoSize = true;
            rbBar.Font = new Font("Segoe UI", 9F);
            rbBar.Location = new Point(13, 90);
            rbBar.Margin = new Padding(3, 2, 3, 2);
            rbBar.Name = "rbBar";
            rbBar.Size = new Size(57, 19);
            rbBar.TabIndex = 3;
            rbBar.Text = "🍹 Bar";
            rbBar.UseVisualStyleBackColor = true;
            // 
            // rbAtanmamis
            // 
            rbAtanmamis.AutoSize = true;
            rbAtanmamis.Checked = true;
            rbAtanmamis.Font = new Font("Segoe UI", 9F);
            rbAtanmamis.Location = new Point(13, 112);
            rbAtanmamis.Margin = new Padding(3, 2, 3, 2);
            rbAtanmamis.Name = "rbAtanmamis";
            rbAtanmamis.Size = new Size(101, 19);
            rbAtanmamis.TabIndex = 4;
            rbAtanmamis.TabStop = true;
            rbAtanmamis.Text = "❌ Atanmamış";
            rbAtanmamis.UseVisualStyleBackColor = true;
            // 
            // chkEnabled
            // 
            chkEnabled.AutoSize = true;
            chkEnabled.Checked = true;
            chkEnabled.CheckState = CheckState.Checked;
            chkEnabled.Font = new Font("Segoe UI", 9F);
            chkEnabled.Location = new Point(13, 139);
            chkEnabled.Margin = new Padding(3, 2, 3, 2);
            chkEnabled.Name = "chkEnabled";
            chkEnabled.Size = new Size(66, 19);
            chkEnabled.TabIndex = 5;
            chkEnabled.Text = "✅ Aktif";
            chkEnabled.UseVisualStyleBackColor = true;
            // 
            // btnSavePrinter
            // 
            btnSavePrinter.BackColor = Color.LightGreen;
            btnSavePrinter.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSavePrinter.Location = new Point(13, 165);
            btnSavePrinter.Margin = new Padding(3, 2, 3, 2);
            btnSavePrinter.Name = "btnSavePrinter";
            btnSavePrinter.Size = new Size(88, 22);
            btnSavePrinter.TabIndex = 6;
            btnSavePrinter.Text = "💾 Kaydet";
            btnSavePrinter.UseVisualStyleBackColor = false;
            btnSavePrinter.Click += btnSavePrinter_Click;
            // 
            // btnTestPrint
            // 
            btnTestPrint.BackColor = Color.LightYellow;
            btnTestPrint.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnTestPrint.Location = new Point(114, 165);
            btnTestPrint.Margin = new Padding(3, 2, 3, 2);
            btnTestPrint.Name = "btnTestPrint";
            btnTestPrint.Size = new Size(88, 22);
            btnTestPrint.TabIndex = 7;
            btnTestPrint.Text = "🖨️ Test";
            btnTestPrint.UseVisualStyleBackColor = false;
            btnTestPrint.Click += btnTestPrint_Click;
            // 
            // grpAssignedPrinters
            // 
            grpAssignedPrinters.Controls.Add(lblMutfak);
            grpAssignedPrinters.Controls.Add(listBoxMutfak);
            grpAssignedPrinters.Controls.Add(lblBar);
            grpAssignedPrinters.Controls.Add(listBoxBar);
            grpAssignedPrinters.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            grpAssignedPrinters.Location = new Point(542, 60);
            grpAssignedPrinters.Margin = new Padding(3, 2, 3, 2);
            grpAssignedPrinters.Name = "grpAssignedPrinters";
            grpAssignedPrinters.Padding = new Padding(3, 2, 3, 2);
            grpAssignedPrinters.Size = new Size(219, 210);
            grpAssignedPrinters.TabIndex = 5;
            grpAssignedPrinters.TabStop = false;
            grpAssignedPrinters.Text = "Atanmış Yazıcılar";
            // 
            // lblMutfak
            // 
            lblMutfak.AutoSize = true;
            lblMutfak.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblMutfak.ForeColor = Color.Green;
            lblMutfak.Location = new Point(13, 22);
            lblMutfak.Name = "lblMutfak";
            lblMutfak.Size = new Size(67, 15);
            lblMutfak.TabIndex = 0;
            lblMutfak.Text = "🍽️ Mutfak:";
            // 
            // listBoxMutfak
            // 
            listBoxMutfak.FormattingEnabled = true;
            listBoxMutfak.ItemHeight = 17;
            listBoxMutfak.Location = new Point(13, 41);
            listBoxMutfak.Margin = new Padding(3, 2, 3, 2);
            listBoxMutfak.Name = "listBoxMutfak";
            listBoxMutfak.Size = new Size(193, 55);
            listBoxMutfak.TabIndex = 1;
            // 
            // lblBar
            // 
            lblBar.AutoSize = true;
            lblBar.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblBar.ForeColor = Color.Blue;
            lblBar.Location = new Point(13, 112);
            lblBar.Name = "lblBar";
            lblBar.Size = new Size(45, 15);
            lblBar.TabIndex = 2;
            lblBar.Text = "🍹 Bar:";
            // 
            // listBoxBar
            // 
            listBoxBar.FormattingEnabled = true;
            listBoxBar.ItemHeight = 17;
            listBoxBar.Location = new Point(13, 131);
            listBoxBar.Margin = new Padding(3, 2, 3, 2);
            listBoxBar.Name = "listBoxBar";
            listBoxBar.Size = new Size(193, 55);
            listBoxBar.TabIndex = 3;
            // 
            // btnSaveAll
            // 
            btnSaveAll.BackColor = Color.Orange;
            btnSaveAll.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnSaveAll.ForeColor = Color.White;
            btnSaveAll.Location = new Point(306, 285);
            btnSaveAll.Margin = new Padding(3, 2, 3, 2);
            btnSaveAll.Name = "btnSaveAll";
            btnSaveAll.Size = new Size(131, 30);
            btnSaveAll.TabIndex = 6;
            btnSaveAll.Text = "💾 Tümünü Kaydet";
            btnSaveAll.UseVisualStyleBackColor = false;
            btnSaveAll.Click += btnSaveAll_Click;
            // 
            // btnLoadSettings
            // 
            btnLoadSettings.BackColor = Color.LightGray;
            btnLoadSettings.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            btnLoadSettings.Location = new Point(455, 285);
            btnLoadSettings.Margin = new Padding(3, 2, 3, 2);
            btnLoadSettings.Name = "btnLoadSettings";
            btnLoadSettings.Size = new Size(131, 30);
            btnLoadSettings.TabIndex = 7;
            btnLoadSettings.Text = "📂 Ayarları Yükle";
            btnLoadSettings.UseVisualStyleBackColor = false;
            btnLoadSettings.Click += btnLoadSettings_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(20, 20);
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus });
            statusStrip.Location = new Point(0, 428);
            statusStrip.Name = "statusStrip";
            statusStrip.Padding = new Padding(1, 0, 12, 0);
            statusStrip.Size = new Size(788, 22);
            statusStrip.TabIndex = 8;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(302, 17);
            lblStatus.Text = "Hazır - Yazıcıları yüklemek için 'Yenile' butonuna tıklayın";
            // 
            // notifyIcon1
            // 
            notifyIcon1.ContextMenuStrip = contextMenuStrip1;
            notifyIcon1.Text = "KufeArt Yazıcı Yöneticisi";
            notifyIcon1.DoubleClick += notifyIcon1_DoubleClick;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { toolStripTextBox1, toolStripSeparator1, toolStripTextBox2 });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(161, 60);
            contextMenuStrip1.Click += contextMenuStrip1_Click;
            // 
            // toolStripTextBox1
            // 
            toolStripTextBox1.Name = "toolStripTextBox1";
            toolStripTextBox1.Size = new Size(100, 23);
            toolStripTextBox1.Text = "Yazıcı Yönetici Aç";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(157, 6);
            // 
            // toolStripTextBox2
            // 
            toolStripTextBox2.Name = "toolStripTextBox2";
            toolStripTextBox2.Size = new Size(100, 23);
            toolStripTextBox2.Text = "Çıkış";
            toolStripTextBox2.Click += toolStripTextBox2_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(788, 450);
            Controls.Add(lblTitle);
            Controls.Add(lblPrinters);
            Controls.Add(listBoxPrinters);
            Controls.Add(btnRefreshPrinters);
            Controls.Add(grpSettings);
            Controls.Add(grpAssignedPrinters);
            Controls.Add(btnSaveAll);
            Controls.Add(btnLoadSettings);
            Controls.Add(statusStrip);
            Margin = new Padding(3, 2, 3, 2);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "KufeArt Yazıcı Yöneticisi";
            Load += MainForm_Load;
            grpSettings.ResumeLayout(false);
            grpSettings.PerformLayout();
            grpAssignedPrinters.ResumeLayout(false);
            grpAssignedPrinters.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            contextMenuStrip1.ResumeLayout(false);
            contextMenuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private Label lblTitle;
        private Label lblPrinters;
        private ListBox listBoxPrinters;
        private Button btnRefreshPrinters;
        private GroupBox grpSettings;
        private Label lblSelectedPrinter;
        private Label lblPrinterName;
        private RadioButton rbMutfak;
        private RadioButton rbBar;
        private RadioButton rbAtanmamis;
        private CheckBox chkEnabled;
        private Button btnSavePrinter;
        private Button btnTestPrint;
        private GroupBox grpAssignedPrinters;
        private ListBox listBoxMutfak;
        private ListBox listBoxBar;
        private Label lblMutfak;
        private Label lblBar;
        private Button btnSaveAll;
        private Button btnLoadSettings;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripTextBox toolStripTextBox1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripTextBox toolStripTextBox2;
    }
}