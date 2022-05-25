namespace Tgs.Promotion.Models;

public record PromotionType
{
	public string Name { get; set; } = default!;
	public string SettlementIntervalType { get; set; } = default!;
	public Translation Trans { get; set; } = default!;
}
