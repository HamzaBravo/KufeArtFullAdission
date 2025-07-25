using System.ComponentModel.DataAnnotations;

namespace KufeArtFullAdission.QrMenuMvc.Models
{
    public class CustomerRegisterDto
    {
        [Required(ErrorMessage = "Ad Soyad zorunludur")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad Soyad 2-100 karakter aras�nda olmal�d�r")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Telefon numaras� zorunludur")]
        [RegularExpression(@"^(\+90|0)?[5][0-9]{9}$", ErrorMessage = "Ge�erli bir telefon numaras� girin (�rn: 05xxxxxxxxx)")]
        public string PhoneNumber { get; set; }

        // �ste�e ba�l� alanlar (gelecekte kullanmak i�in)
        [EmailAddress(ErrorMessage = "Ge�erli bir email adresi girin")]
        public string Email { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "Gizlilik s�zle�mesini kabul etmelisiniz")]
        public bool AcceptPrivacyPolicy { get; set; }

        // Pazarlama izni (opsiyonel)
        public bool AcceptMarketing { get; set; } = false;
    }

    public class CustomerRegistrationDto
    {
        public string Fullname { get; set; }
        public string PhoneNumber { get; set; }
    }

    public class CustomerLoginDto
    {
        public string PhoneNumber { get; set; }
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }

    public class ProductDisplayModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public string CategoryName { get; set; }
        public bool HasCampaign { get; set; }
        public string CampaignCaption { get; set; }
        public string CampaignDetail { get; set; }
        public List<string> Images { get; set; } = new();
    }

    public class CategoryModel
    {
        public string DisplayName { get; set; }
        public string Name { get; set; }
        public int ProductCount { get; set; }
        public string Icon { get; set; }
    }

    public class WeatherSuggestionModel
    {
        public string Message { get; set; }
        public string Icon { get; set; }
        public List<string> RecommendedProducts { get; set; }
        public string WeatherCondition { get; set; }
        public double Temperature { get; set; }
    }
}
