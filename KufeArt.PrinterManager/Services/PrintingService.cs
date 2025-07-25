﻿using System.Drawing;
using System.Drawing.Printing;
using System.Text;
using KufeArt.PrinterManager.Models;
using Newtonsoft.Json;

namespace KufeArt.PrinterManager.Services
{
    public class PrintingService
    {
        private readonly string _configPath;
        private PrinterManagerConfig? _config;
        private readonly List<PrintLogModel> _printLogs = new();

        // Events
        public event Action<string>? LogMessageReceived;
        public event Action<PrintLogModel>? PrintCompleted;

        public PrintingService()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "KufeArt", "PrinterManager");

            _configPath = Path.Combine(configDir, "printer-settings.json");
        }

        public async Task ProcessOrderAsync(OrderNotificationModel orderNotification)
        {
            try
            {
                LogMessage($"🔍 Sipariş işleniyor: {orderNotification.TableName}");

                // Detaylı sipariş bilgilerini al (API'den)
                var detailedOrder = await GetOrderDetailsAsync(orderNotification);
                if (detailedOrder == null)
                {
                    LogMessage("❌ Sipariş detayları alınamadı");
                    return;
                }

                // Yazıcı konfigürasyonunu yükle
                LoadPrinterConfig();
                if (_config == null)
                {
                    LogMessage("❌ Yazıcı konfigürasyonu bulunamadı");
                    return;
                }

                // Mutfak ve Bar ürünlerini ayır
                var kitchenItems = detailedOrder.Items.Where(i => i.ProductType == "Kitchen").ToList();
                var barItems = detailedOrder.Items.Where(i => i.ProductType == "Bar").ToList();

                // Mutfak yazıcılarına yazdır
                if (kitchenItems.Any())
                {
                    await PrintToKitchenAsync(detailedOrder, kitchenItems);
                }

                // Bar yazıcılarına yazdır
                if (barItems.Any())
                {
                    await PrintToBarAsync(detailedOrder, barItems);
                }

                LogMessage($"✅ Sipariş yazdırma tamamlandı: {orderNotification.TableName}");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Sipariş işleme hatası: {ex.Message}");
            }
        }

        #region Yazdırma İşlemleri
        private async Task PrintToKitchenAsync(DetailedOrderModel order, List<OrderItemModel> items)
        {
            var kitchenPrinters = _config!.Printers
                .Where(p => p.Type == "Mutfak" && p.IsEnabled)
                .ToList();

            if (!kitchenPrinters.Any())
            {
                LogMessage("⚠️ Aktif mutfak yazıcısı bulunamadı");
                return;
            }

            foreach (var printer in kitchenPrinters)
            {
                await PrintReceiptAsync(printer, order, items, "🍽️ MUTFAK SİPARİŞİ");
            }
        }

        private async Task PrintToBarAsync(DetailedOrderModel order, List<OrderItemModel> items)
        {
            var barPrinters = _config!.Printers
                .Where(p => p.Type == "Bar" && p.IsEnabled)
                .ToList();

            if (!barPrinters.Any())
            {
                LogMessage("⚠️ Aktif bar yazıcısı bulunamadı");
                return;
            }

            foreach (var printer in barPrinters)
            {
                await PrintReceiptAsync(printer, order, items, "🍹 BAR SİPARİŞİ");
            }
        }

        private async Task PrintReceiptAsync(PrinterConfig printer, DetailedOrderModel order, List<OrderItemModel> items, string header)
        {
            try
            {
                LogMessage($"🖨️ Yazdırılıyor: {printer.Name} - {order.TableName}");

                var printDocument = CreatePrintDocument(printer, order, items, header);

                // Asenkron yazdırma
                await Task.Run(() =>
                {
                    printDocument.Print();
                });

                // Log kaydı
                var printLog = new PrintLogModel
                {
                    TableName = order.TableName,
                    PrinterName = printer.Name,
                    PrinterType = printer.Type,
                    Status = "Success",
                    ItemCount = items.Count
                };

                _printLogs.Add(printLog);
                PrintCompleted?.Invoke(printLog);
                LogMessage($"✅ Yazdırma başarılı: {printer.Name}");
            }
            catch (Exception ex)
            {
                var printLog = new PrintLogModel
                {
                    TableName = order.TableName,
                    PrinterName = printer.Name,
                    PrinterType = printer.Type,
                    Status = "Failed",
                    ErrorMessage = ex.Message,
                    ItemCount = items.Count
                };

                _printLogs.Add(printLog);
                PrintCompleted?.Invoke(printLog);
                LogMessage($"❌ Yazdırma hatası ({printer.Name}): {ex.Message}");
            }
        }
        #endregion

        #region Print Document Oluşturma
        private PrintDocument CreatePrintDocument(PrinterConfig printer, DetailedOrderModel order, List<OrderItemModel> items, string header)
        {
            var printDocument = new PrintDocument();
            printDocument.PrinterSettings.PrinterName = printer.Name;

            // 58mm termal yazıcı için ayarlar
            printDocument.DefaultPageSettings.PaperSize = new PaperSize("Custom", 220, 0); // 58mm genişlik
            printDocument.DefaultPageSettings.Margins = new Margins(5, 5, 5, 5);

            string receiptContent = GenerateReceiptContent(order, items, header);

            printDocument.PrintPage += (sender, e) =>
            {
                if (e.Graphics == null) return;

                var font = new Font("Courier New", 8, FontStyle.Regular);
                var boldFont = new Font("Courier New", 8, FontStyle.Bold);
                var brush = Brushes.Black;

                float yPosition = 10;
                float lineHeight = font.GetHeight();

                var lines = receiptContent.Split('\n');
                foreach (var line in lines)
                {
                    var currentFont = line.StartsWith("**") && line.EndsWith("**") ? boldFont : font;
                    var text = line.Replace("**", "").Trim();

                    e.Graphics.DrawString(text, currentFont, brush, 5, yPosition);
                    yPosition += lineHeight + 2;
                }
            };

            return printDocument;
        }

        private string GenerateReceiptContent(DetailedOrderModel order, List<OrderItemModel> items, string header)
        {
            var receipt = new StringBuilder();

            // Header
            receipt.AppendLine("================================");
            receipt.AppendLine($"**{header}**");
            receipt.AppendLine("================================");
            receipt.AppendLine($"Masa: {order.TableName}");
            receipt.AppendLine($"Garson: {order.WaiterName}");
            receipt.AppendLine($"Tarih: {order.OrderTime:dd.MM.yyyy HH:mm}");
            receipt.AppendLine("--------------------------------");

            // Ürünler
            foreach (var item in items)
            {
                receipt.AppendLine($"{item.Quantity}x {item.ProductName}");
                if (!string.IsNullOrEmpty(item.CategoryName))
                {
                    receipt.AppendLine($"   ({item.CategoryName})");
                }
                receipt.AppendLine($"   {item.Price:C2} x {item.Quantity} = {item.TotalPrice:C2}");
                receipt.AppendLine("");
            }

            receipt.AppendLine("--------------------------------");
            receipt.AppendLine($"**Toplam: {items.Sum(i => i.TotalPrice):C2}**");
            receipt.AppendLine("================================");
            receipt.AppendLine($"Yazdırma: {DateTime.Now:HH:mm:ss}");
            receipt.AppendLine("");
            receipt.AppendLine("");
            receipt.AppendLine("");

            return receipt.ToString();
        }
        #endregion

        #region API Integration (Geçici - Mockup)
        private async Task<DetailedOrderModel?> GetOrderDetailsAsync(OrderNotificationModel notification)
        {
            // TODO: Burası Admin Panel API'sinden gerçek veriyi çekecek
            // Şimdilik mock data dönelim

            await Task.Delay(100); // Simüle network delay

            // Mock sipariş detayları
            return new DetailedOrderModel
            {
                TableId = notification.TableId,
                TableName = notification.TableName,
                WaiterName = notification.WaiterName,
                OrderTime = notification.Timestamp,
                TotalAmount = notification.TotalAmount,
                Items = new List<OrderItemModel>
                {
                    new OrderItemModel
                    {
                        ProductName = "Türk Kahvesi",
                        Quantity = 2,
                        Price = 15.00,
                        TotalPrice = 30.00,
                        ProductType = "Bar", // ProductOrderType.Bar
                        CategoryName = "Sıcak İçecekler"
                    },
                    new OrderItemModel
                    {
                        ProductName = "Karışık Tost",
                        Quantity = 1,
                        Price = 25.00,
                        TotalPrice = 25.00,
                        ProductType = "Kitchen", // ProductOrderType.Kitchen
                        CategoryName = "Tostlar"
                    }
                }
            };
        }
        #endregion

        #region Config Management
        public void LoadPrinterConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var json = File.ReadAllText(_configPath);
                    _config = JsonConvert.DeserializeObject<PrinterManagerConfig>(json);
                    LogMessage("📋 Yazıcı konfigürasyonu yüklendi");
                }
                else
                {
                    _config = new PrinterManagerConfig();
                    LogMessage("⚠️ Konfigürasyon dosyası bulunamadı, varsayılan ayarlar yüklendi");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Konfigürasyon yükleme hatası: {ex.Message}");
                _config = new PrinterManagerConfig();
            }
        }

        public List<PrintLogModel> GetPrintLogs()
        {
            return _printLogs.OrderByDescending(l => l.PrintTime).Take(50).ToList();
        }
        #endregion

        private void LogMessage(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogMessageReceived?.Invoke(logEntry);
            Console.WriteLine(logEntry);
        }
    }
}