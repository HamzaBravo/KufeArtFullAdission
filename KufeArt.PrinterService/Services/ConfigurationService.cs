using KufeArt.PrinterService.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace KufeArt.PrinterService.Services
{
    public class ConfigurationService
    {
        private readonly string _configPath;
        private PrinterManagerConfig _currentConfig;

        public ConfigurationService()
        {

            // Forms uygulaması ile aynı path kullan
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var configDir = Path.Combine(appDataPath, "KufeArt", "PrinterService");
            Directory.CreateDirectory(configDir);
            _configPath = Path.Combine(configDir, "printer-settings.json");
        }

        public PrinterManagerConfig LoadConfigurationAsync()
        {
            try
            {
                if (File.Exists(_configPath))
                {

                    var json =  File.ReadAllText(_configPath);
                    var config = JsonConvert.DeserializeObject<PrinterManagerConfig>(json);

                    if (config != null)
                    {
                        _currentConfig = config;
                        return config;
                    }
                }
                else
                {
                }
            }
            catch (Exception)
            {
            }

            _currentConfig = new PrinterManagerConfig();
            return _currentConfig;
        }

        public PrinterManagerConfig GetCurrentConfig()
        {
            return _currentConfig ?? new PrinterManagerConfig();
        }

        public bool ReloadConfigurationAsync()
        {
            try
            {
                LoadConfigurationAsync();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string GetConfigPath() => _configPath;

        public bool HasValidConfiguration()
        {
            var config = GetCurrentConfig();
            return config.Printers.Any(p => p.IsEnabled && (p.Type == "Mutfak" || p.Type == "Bar"));
        }
    }
}

