namespace Tgs.Promotion.Clients;
public record PromotionMainInfo
{
	public Guid Id { get; init; }

	public Guid BrandId { get; init; }

	public string Name { get; init; } = default!;

	public During EventDuring { get; init; }

	public string PromotionType { get; init; } = default!;

	public decimal? PromotionLimitAmount { get; init; }

	public bool IsAutoSent { get; init; }
}
