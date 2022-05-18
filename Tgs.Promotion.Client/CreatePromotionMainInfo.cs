namespace Tgs.Promotion.Clients;

public record CreatePromotionMainInfo
{
	public string Name { get; init; } = default!;

	public During EventDuring { get; init; }

	public string PromotionType { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }
}
