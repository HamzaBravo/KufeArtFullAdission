using KufeArt.PrinterService.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KufeArt.PrinterService.Services
{
    public class PrinterManagerService
    {
        private readonly ConfigurationService _configService;

        public PrinterManagerService( ConfigurationService configService)
        {
            _configService = configService;
        }

        public async Task<bool> PrintOrderAsync(OrderPrintData orderData)
        {
            try
            {
                var config = _configService.GetCurrentConfig();
                bool anyPrinted = false;

                // Ürünleri türüne göre grupla
                var kitchenItems = orderData.Items.Where(i =>
                    i.ProductType.Equals("Kitchen", StringComparison.OrdinalIgnoreCase) ||
                    i.ProductType.Equals("Mutfak", StringComparison.OrdinalIgnoreCase)).ToList();

                var barItems = orderData.Items.Where(i =>
                    i.ProductType.Equals("Bar", StringComparison.OrdinalIgnoreCase)).ToList();

                // Mutfak yazıcılarına bas
                if (kitchenItems.Any())
                {
                    var kitchenPrinters = config.Printers.Where(p => p.Type == "Mutfak" && p.IsEnabled).ToList();

                    foreach (var printer in kitchenPrinters)
                    {
                        if (await PrintToDevice(printer.Name, orderData, kitchenItems, "MUTFAK"))
                        {
                            anyPrinted = true;
                        }
                    }
                }

                // Bar yazıcılarına bas
                if (barItems.Any())
                {
                    var barPrinters = config.Printers.Where(p => p.Type == "Bar" && p.IsEnabled).ToList();

                    foreach (var printer in barPrinters)
                    {
                        if (await PrintToDevice(printer.Name, orderData, barItems, "BAR"))
                        {
                            anyPrinted = true;
                        }
                    }
                }

                return anyPrinted;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private async Task<bool> PrintToDevice(string printerName, OrderPrintData orderData,
                                             List<OrderItem> items, string section)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using (var printDoc = new PrintDocument())
                    {
                        printDoc.PrinterSettings.PrinterName = printerName;

                        // Yazıcı mevcut mu kontrol et
                        if (!printDoc.PrinterSettings.IsValid)
                        {
                            return false;
                        }

                        var printContent = GeneratePrintContent(orderData, items, section);

                        printDoc.PrintPage += (sender, e) =>
                        {
                            if (e.Graphics == null) return;

                            var font = new Font("Courier New", 9, FontStyle.Regular);
                            var boldFont = new Font("Courier New", 9, FontStyle.Bold);
                            var brush = Brushes.Black;
                            float y = 10;
                            float lineHeight = font.GetHeight();

                            var lines = printContent.Split('\n');

                            for (int i = 0; i < lines.Length; i++)
                            {
                                var line = lines[i];
                                var currentFont = line.Contains("KUFE ART") || line.Contains(section) ||
                                                line.StartsWith("Sipariş") || line.StartsWith("Masa") ? boldFont : font;

                                e.Graphics.DrawString(line, currentFont, brush, 5, y + (i * lineHeight));
                            }

                            font.Dispose();
                            boldFont.Dispose();
                        };

                        printDoc.Print();
                        return true;
                    }


                    
                });
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private string GeneratePrintContent(OrderPrintData orderData, List<OrderItem> items, string section)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("================================");
            sb.AppendLine($"        KUFE ART - {section}");
            sb.AppendLine("================================");
            sb.AppendLine();

            // Order info
            sb.AppendLine($"Sip No:{orderData.OrderId}");
            sb.AppendLine($"Masa:  {orderData.TableName}");
            sb.AppendLine($"Garson:{orderData.WaiterName}");
            sb.AppendLine($"Saat:  {orderData.OrderTime:HH:mm:ss}");
            sb.AppendLine($"Tarih: {orderData.OrderTime:dd/MM/yyyy}");
            sb.AppendLine();
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"             {section} SİPARİŞİ");
            sb.AppendLine("--------------------------------");

            // Items
            int totalQty = 0;
            foreach (var item in items)
            {
                sb.AppendLine($"{item.Quantity}x {item.ProductName}");
                totalQty += item.Quantity;

                if (!string.IsNullOrEmpty(item.Notes))
                {
                    sb.AppendLine($"   >>> {item.Notes}");
                }
                sb.AppendLine();
            }

            // Footer
            sb.AppendLine("--------------------------------");
            sb.AppendLine($"Toplam Adet: {totalQty}");

            if (!string.IsNullOrEmpty(orderData.Notes))
            {
                sb.AppendLine();
                sb.AppendLine("SİPARİŞ NOTU:");
                sb.AppendLine(orderData.Notes);
            }

            sb.AppendLine();
            sb.AppendLine("================================");
            sb.AppendLine($"{DateTime.Now:dd/MM/yyyy HH:mm}");
            sb.AppendLine("================================");

            // Kesim için boş satırlar
            sb.AppendLine();
            sb.AppendLine();

            return sb.ToString();
        }

        public ServiceStatus GetStatus()
        {
            var config = _configService.GetCurrentConfig();

            return new ServiceStatus
            {
                IsRunning = true,
                LastHeartbeat = DateTime.Now,
                TotalPrinters = config.Printers.Count,
                KitchenPrinters = config.Printers.Count(p => p.Type == "Mutfak" && p.IsEnabled),
                BarPrinters = config.Printers.Count(p => p.Type == "Bar" && p.IsEnabled),
                ActivePrinters = config.Printers.Where(p => p.IsEnabled).Select(p => p.Name).ToList()
            };
        }

        public async Task<bool> TestPrinterAsync(string printerName, string printerType)
        {
            try
            {
                var testOrder = new OrderPrintData
                {
                    OrderId = "TEST-001",
                    TableName = "Test Masası",
                    WaiterName = "Sistem",
                    OrderTime = DateTime.Now,
                    Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductName = "Test Ürünü",
                        ProductType = printerType,
                        Quantity = 1,
                        Notes = "Bu bir test siparişidir"
                    }
                },
                    Notes = "Test yazdırma işlemi"
                };

                return await PrintToDevice(printerName, testOrder, testOrder.Items, printerType.ToUpper());
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}

