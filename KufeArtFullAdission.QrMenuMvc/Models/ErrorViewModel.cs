namespace KufeArtFullAdission.QrMenuMvc.Models
{
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
