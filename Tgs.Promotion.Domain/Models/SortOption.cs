namespace Tgs.Promotion.Domain.Models
{
    public record SortOption
    {
        public string? PropertyName { get; set; }

        public bool IsDescending { get; set; }

        public SortOption(string? propertyName, bool isDescending)
        {
            IsDescending = isDescending;
            PropertyName = propertyName;
        }
    }
}
