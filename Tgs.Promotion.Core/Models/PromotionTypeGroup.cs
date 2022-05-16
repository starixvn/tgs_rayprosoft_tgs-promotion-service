namespace Tgs.Promotion.Core.Models
{
	public record PromotionTypeGroup
	{
		public string Name { get; set; } = default!;
		public PromotionType[] Types { get; set; } = default!;
		public Translation Trans { get; set; } = default!;
	}
}
