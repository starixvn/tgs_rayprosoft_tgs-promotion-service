namespace Tgs.Promotion.Domain.Models
{
    public record PromotionTypeGroup
    {
        public string Name { get; set; }
        public PromotionType[] Types { get; set; }
        public Translation Trans { get; set; }
    }
}