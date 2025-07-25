using AppDbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;

namespace KufeArtFullAdission.QrMenuMvc.Controllers
{
    [Route("api/qr-menu")]
    [ApiController]
    public class QrMenuApiController : ControllerBase
    {
        private readonly DBContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public QrMenuApiController(DBContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        // 📱 ANA MENÜ VERİLERİ
        // 🎯 YARDIMCI METODLAR bölümünü şöyle değiştirin:


        // QrMenuApiController.cs'e güvenli login endpoint'i ekleyin:

        [HttpPost("customer/login")]
        public async Task<IActionResult> LoginCustomer([FromBody] CustomerLoginDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.PhoneNumber) || string.IsNullOrEmpty(dto.Password))
                {
                    return Ok(new { success = false, message = "Telefon numarası ve şifre gereklidir." });
                }

                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.PhoneNumber == dto.PhoneNumber && c.IsActive&&c.Password==dto.Password);

                if (customer == null || customer.Password != dto.Password)
                {
                    return Ok(new { success = false, message = "Hatalı telefon numarası yada şifre" });
                }


                // Müşteri puan bilgileri
                var customerPoints = await _context.CustomerPoints
                    .FirstOrDefaultAsync(cp => cp.CustomerId == customer.Id);

                return Ok(new
                {
                    success = true,
                    message = $"Merhaba {customer.Fullname}! 👋",
                    customer = new
                    {
                        id = customer.Id,
                        name = customer.Fullname,
                        phone = customer.PhoneNumber,
                        points = customerPoints?.TotalPoints ?? 0,
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Login error: {ex.Message}");
                return Ok(new { success = false, message = "Giriş sırasında hata oluştu." });
            }
        }

        // Puan geçmişi API'si
        [HttpGet("customer/point-history/{customerId}")]
        public async Task<IActionResult> GetPointHistory(Guid customerId)
        {
            try
            {
                var transactions = await _context.KufePointTransactions
                    .AsNoTracking()
                    .Where(t => t.CustomerId == customerId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(20) // Son 20 işlem
                    .Select(t => new
                    {
                        id = t.Id,
                        type = t.Type.ToString(),
                        points = t.Points,
                        description = t.Description,
                        date = t.CreatedAt,
                        productId = t.ProductId
                    })
                    .ToListAsync();

                return Ok(new { success = true, transactions = transactions });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Point history error: {ex.Message}");
                return Ok(new { success = false, message = "Puan geçmişi yüklenemedi." });
            }
        }

        // Şifre değiştirme API'si
        [HttpPost("customer/change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == dto.CustomerId && c.IsActive);

                if (customer == null || customer.Password != dto.CurrentPassword)
                {
                    return Ok(new { success = false, message = "Mevcut şifre hatalı!" });
                }

                if (dto.NewPassword.Length < 4)
                {
                    return Ok(new { success = false, message = "Şifre en az 4 karakter olmalıdır!" });
                }

                customer.Password = dto.NewPassword;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Şifre başarıyla değiştirildi!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Change password error: {ex.Message}");
                return Ok(new { success = false, message = "Şifre değiştirme sırasında hata oluştu." });
            }
        }

        // DTO
        public class CustomerLoginDto
        {
            public string PhoneNumber { get; set; }
            public string Password { get; set; }
        }

        public class ChangePasswordDto
        {
            public Guid CustomerId { get; set; }
            public string CurrentPassword { get; set; }
            public string NewPassword { get; set; }
        }

        private string GetRandomCategoryImage(IEnumerable<object> categoryProducts)
        {
            try
            {
                var productList = categoryProducts.ToList();
                var productsWithImages = new List<object>();

                foreach (var product in productList)
                {
                    // Reflection ile Images property'sine erişim
                    var productType = product.GetType();
                    var imagesProperty = productType.GetProperty("Images");
                    var images = imagesProperty?.GetValue(product) as IEnumerable<object>;

                    if (images?.Any() == true)
                    {
                        productsWithImages.Add(product);
                    }
                }

                if (!productsWithImages.Any()) return null;

                // Rastgele ürün seç
                var randomProduct = productsWithImages[Random.Shared.Next(productsWithImages.Count)];
                var randomType = randomProduct.GetType();
                var randomImagesProperty = randomType.GetProperty("Images");
                var randomImages = randomImagesProperty?.GetValue(randomProduct) as IEnumerable<object>;

                var firstImage = randomImages?.FirstOrDefault();
                if (firstImage == null) return null;

                // Thumbnail property'sine erişim
                var imageType = firstImage.GetType();
                var thumbnailProperty = imageType.GetProperty("Thumbnail");
                return thumbnailProperty?.GetValue(firstImage) as string;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Random image error: {ex.Message}");
                return null;
            }
        }

        // 🌤️ HAVA DURUMU + ZAMAN BAZLI ÖNERİLER
        [HttpGet("smart-suggestions")]
        public async Task<IActionResult> GetSmartSuggestions()
        {
            try
            {
                Console.WriteLine("🧠 Smart suggestions loading...");

                // Hava durumu bilgisini al
                var weatherData = await GetWeatherData();
                var currentHour = DateTime.Now.Hour;
                var temperature = weatherData?.Temperature ?? 20;
                var condition = weatherData?.Condition ?? "clear";

                // Akıllı öneri oluştur
                var suggestion = await GenerateSmartSuggestion(temperature, condition, currentHour);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        weather = weatherData,
                        suggestion = suggestion,
                        timestamp = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Smart suggestions error: {ex.Message}");
                return Ok(new
                {
                    success = false,
                    message = "Öneriler yüklenirken hata oluştu"
                });
            }
        }

        // 👤 MÜŞTERİ PUAN SİSTEMİ
        [HttpPost("customer/quick-check")]
        public async Task<IActionResult> QuickCustomerCheck([FromBody] CustomerCheckDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.PhoneNumber))
                    return Ok(new { success = false, message = "Telefon numarası gerekli" });

                //var cleanPhone = CleanPhoneNumber(dto.PhoneNumber);

                var customer = await _context.Customers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.PhoneNumber == dto.PhoneNumber && c.IsActive);

                if (customer == null)
                {
                    return Ok(new
                    {
                        success = false,
                        isRegistered = false,
                        message = "Bu numaraya kayıtlı üyelik bulunamadı"
                    });
                }

                var customerPoints = await _context.CustomerPoints
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cp => cp.CustomerId == customer.Id);

                return Ok(new
                {
                    success = true,
                    isRegistered = true,
                    customer = new
                    {
                        id = customer.Id,
                        name = customer.Fullname,
                        phone = customer.PhoneNumber,
                        points = customerPoints?.TotalPoints ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { success = false, message = "Kontrol sırasında hata oluştu" });
            }
        }

        [HttpGet("menu-data")]
        public async Task<IActionResult> GetMenuData()
        {
            try
            {
                Console.WriteLine("🚀 QR Menu data loading started...");

                // Önce basic data'yı çek
                var rawProducts = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu)
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        CategoryName = p.CategoryName,
                        HasCampaign = p.HasCampaign,
                        CampaignCaption = p.CampaignCaption,
                        CampaignDetail = p.CampaignDetail,
                        HasKufePoints = p.HasKufePoints,
                        KufePoints = p.KufePoints,
                        // 🖼️ Raw image paths - thumbnail generation memory'de yapacağız
                        ImagePaths = _context.ProductImages
                            .Where(pi => pi.ProductId == p.Id)
                            .Take(3)
                            .Select(pi => pi.ImagePath)
                            .ToList()
                    })
                    .OrderBy(p => p.CategoryName)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // 🔄 Memory'de thumbnail paths oluştur
                var products = rawProducts.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.CategoryName,
                    p.HasCampaign,
                    p.CampaignCaption,
                    p.CampaignDetail,
                    p.HasKufePoints,
                    p.KufePoints,
                    Images = p.ImagePaths.Select(imgPath => new
                    {
                        Original = imgPath,
                        Thumbnail = GenerateThumbnailPath(imgPath) // Artık memory'de çalışıyor
                    }).ToList()
                }).ToList();

                // 📂 Kategorileri otomatik oluştur - DÜZELTİLEN VERSİYON
                var categories = new List<object>();
                var categoryGroups = products.GroupBy(p => p.CategoryName);

                foreach (var group in categoryGroups)
                {
                    var productsInCategory = group.ToList();

                    // Rastgele resim seç
                    string randomImage = null;
                    var productsWithImages = productsInCategory.Where(p => p.Images.Any()).ToList();
                    if (productsWithImages.Any())
                    {
                        var randomProduct = productsWithImages[Random.Shared.Next(productsWithImages.Count)];
                        randomImage = randomProduct.Images.FirstOrDefault()?.Thumbnail;
                    }

                    categories.Add(new
                    {
                        Name = group.Key,
                        DisplayName = group.Key,
                        ProductCount = group.Count(),
                        Icon = GetCategoryIcon(group.Key),
                        RandomImage = randomImage
                    });
                }

                categories = categories.OrderBy(c => ((dynamic)c).Name).ToList();

                Console.WriteLine($"✅ Loaded {products.Count} products, {categories.Count} categories");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        products = products,
                        categories = categories,
                        loadTime = DateTime.Now
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Menu data error: {ex.Message}");
                return Ok(new
                {
                    success = false,
                    message = "Menü yüklenirken hata oluştu",
                    error = ex.Message
                });
            }
        }

        // 🎯 YARDIMCI METODLAR
        private string GenerateThumbnailPath(string originalPath)
        {
            // 🔧 Geçici: Thumbnail yerine orijinal resmi kullan
            return originalPath;

            /* 
            // İleride thumbnail sistemi için:
            if (string.IsNullOrEmpty(originalPath)) return originalPath;

            var extension = Path.GetExtension(originalPath);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
            var directory = Path.GetDirectoryName(originalPath);

            return Path.Combine(directory ?? "", $"{nameWithoutExt}_thumb{extension}")
                       .Replace("\\", "/");
            */
        }

        private string GetCategoryIcon(string categoryName)
        {
            var name = categoryName.ToLowerInvariant();
            return name switch
            {
                var n when n.Contains("kahve") => "☕",
                var n when n.Contains("çay") || n.Contains("cay") => "🍃",
                var n when n.Contains("tatlı") || n.Contains("tatli") => "🧁",
                var n when n.Contains("soğuk") || n.Contains("soguk") => "🧊",
                var n when n.Contains("sıcak") || n.Contains("sicak") => "🔥",
                var n when n.Contains("yemek") => "🍽️",
                var n when n.Contains("atıştırmalık") => "🍪",
                var n when n.Contains("salata") => "🥗",
                var n when n.Contains("sandviç") => "🥪",
                var n when n.Contains("pasta") => "🎂",
                _ => "🍴"
            };
        }

        // 🎯 YARDIMCI METODLAR bölümünü şöyle değiştirin:

        private async Task<WeatherDataDto> GetWeatherData()
        {
            try
            {
                var apiKey = _configuration["WeatherApi:ApiKey"];
                var baseUrl = _configuration["WeatherApi:BaseUrl"];
                var lat = _configuration.GetValue<double>("RestaurantInfo:Latitude");
                var lon = _configuration.GetValue<double>("RestaurantInfo:Longitude");

                if (string.IsNullOrEmpty(apiKey)) return null;

                var url = $"{baseUrl}/weather?lat={lat}&lon={lon}&appid={apiKey}&units=metric&lang=tr";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                var content = await response.Content.ReadAsStringAsync();
                var weatherData = JsonConvert.DeserializeObject<dynamic>(content);

                return new WeatherDataDto
                {
                    Temperature = (double)weatherData.main.temp,
                    FeelsLike = (double)weatherData.main.feels_like,
                    Condition = (string)weatherData.weather[0].main,
                    Description = (string)weatherData.weather[0].description,
                    Icon = (string)weatherData.weather[0].icon
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<SmartSuggestionDto> GenerateSmartSuggestion(double temperature, string condition, int hour)
        {
            try
            {
                List<string> categoryFilters = new();
                string message = "";
                string icon = "🌤️";

                // 🕐 ZAMAN BAZLI FİLTRELEME
                if (hour >= 6 && hour <= 10)
                {
                    // Sabah
                    categoryFilters.AddRange(new[] { "Kahve", "Çay", "Kahvaltı" });
                    message = "Günaydın! Güne enerjik başlamak için";
                    icon = "🌅";
                }
                else if (hour >= 11 && hour <= 14)
                {
                    // Öğle
                    categoryFilters.AddRange(new[] { "Yemek", "Salata", "Ana Yemek" });
                    message = "Öğle arası için";
                    icon = "🍽️";
                }
                else if (hour >= 15 && hour <= 17)
                {
                    // İkindi
                    categoryFilters.AddRange(new[] { "Kahve", "Tatlı", "Atıştırmalık" });
                    message = "İkindi keyfi için";
                    icon = "☕";
                }
                else
                {
                    // Akşam
                    categoryFilters.AddRange(new[] { "Sıcak İçecek", "Tatlı" });
                    message = "Akşam rahatlığı için";
                    icon = "🌆";
                }

                // 🌤️ HAVA DURUMU FİLTRELEMESİ
                if (temperature < 15)
                {
                    categoryFilters.AddRange(new[] { "Sıcak", "Çorba" });
                    message += " sıcacık lezzetler";
                    icon = "🥶";
                }
                else if (temperature > 25)
                {
                    categoryFilters.AddRange(new[] { "Soğuk", "Buzlu", "Dondurma" });
                    message += " serinletici tatlar";
                    icon = "☀️";
                }

                // 🎲 Önce basic product data'yı al
                var rawProduct = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.IsQrMenu &&
                               categoryFilters.Any(cf => p.CategoryName.Contains(cf)))
                    .OrderBy(x => Guid.NewGuid()) // Rastgele sıralama
                    .Select(p => new
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Category = p.CategoryName,
                        HasKufePoints = p.HasKufePoints,
                        KufePoints = p.KufePoints,
                        FirstImagePath = _context.ProductImages
                            .Where(pi => pi.ProductId == p.Id)
                            .Select(pi => pi.ImagePath)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (rawProduct == null) return null;

                // 🔄 Memory'de thumbnail oluştur
                var suggestedProduct = new
                {
                    rawProduct.Id,
                    rawProduct.Name,
                    rawProduct.Description,
                    rawProduct.Price,
                    rawProduct.Category,
                    rawProduct.HasKufePoints,
                    rawProduct.KufePoints,
                    Image = !string.IsNullOrEmpty(rawProduct.FirstImagePath) ?
                           GenerateThumbnailPath(rawProduct.FirstImagePath) :
                           null
                };

                return new SmartSuggestionDto
                {
                    Icon = icon,
                    Message = message,
                    Product = suggestedProduct
                };
            }
            catch
            {
                return null;
            }
        }

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber)) return phoneNumber;

            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

            if (digitsOnly.StartsWith("90") && digitsOnly.Length == 12)
                digitsOnly = digitsOnly.Substring(2);

            if (digitsOnly.StartsWith("0") && digitsOnly.Length == 11)
                digitsOnly = digitsOnly.Substring(1);

            return digitsOnly;
        }
    }

    // 📦 DTO SINIFLAR
    public class CustomerCheckDto
    {
        public string PhoneNumber { get; set; }
    }

    public class WeatherDataDto
    {
        public double Temperature { get; set; }
        public double FeelsLike { get; set; }
        public string Condition { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
    }

    public class SmartSuggestionDto
    {
        public string Icon { get; set; }
        public string Message { get; set; }
        public object Product { get; set; }
    }
}