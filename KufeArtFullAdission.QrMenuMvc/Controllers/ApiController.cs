using AppDbContext;
using KufeArtFullAdission.Entity;
using KufeArtFullAdission.QrMenuMvc.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace KufeArtFullAdission.QrMenuMvc.Controllers
{
    [Route("api")]
    [ApiController]
    public class ApiController (DBContext _context, IConfiguration _configuration, HttpClient _httpClient) : ControllerBase
    {

        [HttpGet("menu/products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                // QR menüde gösterilecek aktif ürünleri getir
                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu)
                    .Select(p => new ProductDisplayModel
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryName = p.CategoryName,
                        HasCampaign = p.HasCampaign,
                        CampaignCaption = p.CampaignCaption,
                        CampaignDetail = p.CampaignDetail
                    })
                    .OrderBy(p => p.CategoryName)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // Her ürün için resimleri getir
                foreach (var product in products)
                {
                    var images = await _context.ProductImages
                        .AsNoTracking()
                        .Where(pi => pi.ProductId == product.Id)
                        .Select(pi => pi.ImagePath)
                        .ToListAsync();

                    product.Images = images;
                }

                // Kategorileri oluştur
                var categories = products
                    .GroupBy(p => p.CategoryName)
                    .Select(g => new CategoryModel
                    {
                        Name = g.Key.ToLower().Replace(" ", ""),
                        DisplayName = g.Key,
                        ProductCount = g.Count(),
                        Icon = GetCategoryIcon(g.Key)
                    })
                    .OrderBy(c => c.DisplayName)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    products = products,
                    categories = categories,
                    totalProducts = products.Count,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ürünler yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpGet("menu/categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu)
                    .GroupBy(p => p.CategoryName)
                    .Select(g => new CategoryModel
                    {
                        Name = g.Key.ToLower().Replace(" ", ""),
                        DisplayName = g.Key,
                        ProductCount = g.Count(),
                        Icon = GetCategoryIcon(g.Key)
                    })
                    .OrderBy(c => c.DisplayName)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    categories = categories
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Kategoriler yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpPost("menu/track-view")]
        public async Task<IActionResult> TrackView([FromBody] object data)
        {
            try
            {
                // QR menü görüntülenme kaydı
                var qrView = new QrMenuVisibleDbEntity
                {
                    IpAdress = GetClientIpAddress(),
                };

                _context.QrMenuVisibles.Add(qrView);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Görüntülenme kaydedildi"
                });
            }
            catch (Exception ex)
            {
                // Tracking hata verirse bile devam etsin
                return Ok(new
                {
                    success = false,
                    message = "Tracking hatası",
                    error = ex.Message
                });
            }
        }

        [HttpGet("weather/current")]
        public async Task<IActionResult> GetCurrentWeather()
        {
            try
            {
                var apiKey = _configuration["WeatherApi:ApiKey"];
                var baseUrl = _configuration["WeatherApi:BaseUrl"];
                var lat = _configuration.GetValue<double>("RestaurantInfo:Latitude");
                var lon = _configuration.GetValue<double>("RestaurantInfo:Longitude");

                if (string.IsNullOrEmpty(apiKey))
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Weather API key not configured"
                    });
                }

                // OpenWeatherMap API çağrısı
                var url = $"{baseUrl}/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric&lang=tr";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Weather API request failed"
                    });
                }

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonConvert.DeserializeObject<dynamic>(content);

                // Response'u temizle ve sadece ihtiyacımız olanları gönder
                var result = new
                {
                    success = true,
                    temperature = (double)weatherData.main.temp,
                    feelsLike = (double)weatherData.main.feels_like,
                    humidity = (int)weatherData.main.humidity,
                    condition = (string)weatherData.weather[0].main,
                    description = (string)weatherData.weather[0].description,
                    icon = (string)weatherData.weather[0].icon,
                    city = (string)weatherData.name,
                    timestamp = DateTime.Now
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    message = "Hava durumu bilgisi alınamadı",
                    error = ex.Message
                });
            }
        }

        // Yardımcı metodlar
        private string GetCategoryIcon(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                var c when c.Contains("kahve") => "fas fa-coffee",
                var c when c.Contains("çay") => "fas fa-leaf",
                var c when c.Contains("tatlı") => "fas fa-birthday-cake",
                var c when c.Contains("yemek") => "fas fa-utensils",
                var c when c.Contains("içecek") => "fas fa-glass-cheers",
                var c when c.Contains("atıştırmalık") => "fas fa-cookie-bite",
                var c when c.Contains("soğuk") => "fas fa-snowflake",
                var c when c.Contains("sıcak") => "fas fa-fire",
                _ => "fas fa-tag"
            };
        }

        private string GetClientIpAddress()
        {
            // IP adresini al (proxy arkasında olabilir)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Proxy headers kontrol et
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            }
            else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
            {
                ipAddress = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            }

            return ipAddress ?? "unknown";
        }
    }
}
