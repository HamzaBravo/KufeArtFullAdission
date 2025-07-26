using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KufeArtFullAdission.PrinterSettingsForm.Models
{
    public class PrinterConfig
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Atanmamış"; // "Mutfak", "Bar", "Atanmamış"
        public bool IsEnabled { get; set; } = true;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class PrinterSettings
    {
        public List<PrinterConfig> Printers { get; set; } = new List<PrinterConfig>();
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
    }
}
