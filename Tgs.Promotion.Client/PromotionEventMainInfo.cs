namespace Tgs.Promotion.Clients;

public record PromotionEventMainInfo
{
	public Guid EventId { get; init; }

	public Guid PromotionId { get; init; }

	public PromotionEventAction Action { get; init; } = default!;

	public string? Modifier { get; init; }

	public DateTime CreatedOn { get; init; }
}
