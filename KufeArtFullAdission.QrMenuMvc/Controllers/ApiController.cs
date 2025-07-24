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
                var products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryName = p.CategoryName,
                        CategoryNameNormalized = NormalizeCategoryName(p.CategoryName),
                        HasCampaign = p.HasCampaign,
                        CampaignCaption = p.CampaignCaption,
                        CampaignDetail = p.CampaignDetail,
                        Type = p.Type.ToString(),
                        // 🎯 YENİ: Küfe Point Bilgileri
                        HasKufePoints = p.HasKufePoints,
                        KufePoints = p.KufePoints,
                        Images = _context.ProductImages
                            .Where(pi => pi.ProductId == p.Id)
                            .Select(pi => pi.ImagePath)
                            .ToList()
                    })
                    .OrderBy(p => p.CategoryName)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                var distinctCategories = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu)
                    .Select(p => p.CategoryName)
                    .Distinct()
                    .ToListAsync();

                var categories = distinctCategories.Select(categoryName => new
                {
                    Name = NormalizeCategoryName(categoryName),
                    DisplayName = categoryName,
                    ProductCount = products.Count(p => p.CategoryName == categoryName),
                    Icon = GetCategoryIcon(categoryName)
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
        private static string GetCategoryIcon(string categoryName)
        {
            var normalized = categoryName.ToLowerInvariant();

            return normalized switch
            {
                var name when name.Contains("kahve") => "fas fa-coffee",
                var name when name.Contains("çay") || name.Contains("cay") => "fas fa-leaf",
                var name when name.Contains("tatlı") || name.Contains("tatli") => "fas fa-birthday-cake",
                var name when name.Contains("içecek") || name.Contains("icecek") => "fas fa-glass-water",
                var name when name.Contains("soğuk") || name.Contains("soguk") => "fas fa-snowflake",
                var name when name.Contains("sıcak") || name.Contains("sicak") => "fas fa-fire",
                var name when name.Contains("yemek") => "fas fa-utensils",
                var name when name.Contains("atıştırmalık") || name.Contains("atistirmalik") => "fas fa-cookie-bite",
                var name when name.Contains("salata") => "fas fa-seedling",
                var name when name.Contains("sandviç") || name.Contains("sandvic") => "fas fa-hamburger",
                var name when name.Contains("pasta") => "fas fa-birthday-cake",
                var name when name.Contains("börek") || name.Contains("borek") => "fas fa-bread-slice",
                _ => "fas fa-utensils"
            };
        }

        // ✅ Kategori adını normalize etmek için yardımcı static metod
        private static string NormalizeCategoryName(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return "";

            return categoryName
                .ToUpperInvariant() // ✅ ToUpper yerine ToUpperInvariant kullan
                .Replace("Ç", "C")
                .Replace("Ğ", "G")
                .Replace("İ", "I")
                .Replace("Ö", "O")
                .Replace("Ş", "S")
                .Replace("Ü", "U")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "");
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



        #region Customer Management APIs

        [HttpPost("customer/register")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegistrationDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.PhoneNumber) || string.IsNullOrEmpty(dto.Fullname))
                    return BadRequest(new { success = false, message = "Telefon numarası ve ad soyad gerekli!" });

                // Telefon formatını kontrol et
                if (dto.PhoneNumber.Length != 11 || !dto.PhoneNumber.StartsWith("0"))
                    return BadRequest(new { success = false, message = "Geçerli bir telefon numarası girin! (05XX XXX XX XX)" });

                // Müşteri zaten kayıtlı mı?
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == dto.PhoneNumber);

                if (existingCustomer != null)
                {
                    if (!existingCustomer.IsActive)
                    {
                        // Pasif müşteriyi aktif et
                        existingCustomer.IsActive = true;
                        existingCustomer.Fullname = dto.Fullname; // Adını güncelleyebilir
                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Hesabınız yeniden aktif edildi!",
                            customer = new
                            {
                                id = existingCustomer.Id,
                                fullname = existingCustomer.Fullname,
                                phoneNumber = existingCustomer.PhoneNumber
                            }
                        });
                    }

                    return BadRequest(new { success = false, message = "Bu telefon numarası zaten kayıtlı!" });
                }

                // Yeni müşteri oluştur
                var newCustomer = new CustomerDbEntity
                {
                    Fullname = dto.Fullname.Trim(),
                    PhoneNumber = dto.PhoneNumber.Trim(),
                    IsActive = true
                };

                await _context.Customers.AddAsync(newCustomer);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Kayıt başarılı! Artık puan kazanabilirsiniz.",
                    customer = new
                    {
                        id = newCustomer.Id,
                        fullname = newCustomer.Fullname,
                        phoneNumber = newCustomer.PhoneNumber
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Kayıt sırasında hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpPost("customer/login")]
        public async Task<IActionResult> LoginCustomer([FromBody] CustomerLoginDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.PhoneNumber))
                    return BadRequest(new { success = false, message = "Telefon numarası gerekli!" });

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == dto.PhoneNumber && c.IsActive);

                if (customer == null)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Bu telefon numarasına kayıtlı aktif müşteri bulunamadı!",
                        shouldRegister = true
                    });
                }

                // Müşteri puan bakiyesini al
                var customerPoints = await _context.CustomerPoints
                    .FirstOrDefaultAsync(cp => cp.CustomerId == customer.Id);

                return Ok(new
                {
                    success = true,
                    message = "Giriş başarılı!",
                    customer = new
                    {
                        id = customer.Id,
                        fullname = customer.Fullname,
                        phoneNumber = customer.PhoneNumber,
                        totalPoints = customerPoints?.TotalPoints ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Giriş sırasında hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpGet("customer/{customerId}/points")]
        public async Task<IActionResult> GetCustomerPoints(Guid customerId)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

                if (customer == null)
                    return NotFound(new { success = false, message = "Müşteri bulunamadı!" });

                var customerPoints = await _context.CustomerPoints
                    .FirstOrDefaultAsync(cp => cp.CustomerId == customerId);

                return Ok(new
                {
                    success = true,
                    customer = new
                    {
                        id = customer.Id,
                        fullname = customer.Fullname,
                        phoneNumber = customer.PhoneNumber,
                        totalPoints = customerPoints?.TotalPoints ?? 0,
                        canUseDiscount = (customerPoints?.TotalPoints ?? 0) >= 5000 // 5000 puan = 50TL indirim
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Puan bilgisi alınırken hata oluştu",
                    error = ex.Message
                });
            }
        }

        [HttpGet("customer/{customerId}/points-history")]
        public async Task<IActionResult> GetCustomerPointsHistory(Guid customerId, int limit = 20)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);

                if (customer == null)
                    return NotFound(new { success = false, message = "Müşteri bulunamadı!" });

                var pointsHistory = await _context.KufePointTransactions
                    .Where(t => t.CustomerId == customerId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(limit)
                    .Select(t => new
                    {
                        id = t.Id,
                        type = t.Type.ToString(),
                        points = t.Points,
                        description = t.Description,
                        date = t.CreatedAt.ToString("dd.MM.yyyy HH:mm")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    history = pointsHistory,
                    totalRecords = pointsHistory.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Puan geçmişi alınırken hata oluştu",
                    error = ex.Message
                });
            }
        }

        #endregion
    }
}
