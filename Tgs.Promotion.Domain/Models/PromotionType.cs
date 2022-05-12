namespace Tgs.Promotion.Domain.Models
{
    public record PromotionType
    {
        public string Name { get; set; }
        public string SettlementIntervalType { get; set; }
        public Translation Trans { get; set; }
    }
}