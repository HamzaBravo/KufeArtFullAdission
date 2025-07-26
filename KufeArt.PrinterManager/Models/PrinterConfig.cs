using Newtonsoft.Json;

namespace KufeArt.PrinterManager.Models
{
    public class PrinterConfig
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "Atanmamış"; // "Mutfak", "Bar", "Atanmamış"
        public bool IsEnabled { get; set; } = true;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class PrinterManagerConfig
    {
        public List<PrinterConfig> Printers { get; set; } = new List<PrinterConfig>();
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public string Version { get; set; } = "1.0";
        public string ConfigPath { get; set; } = "";
    }
}