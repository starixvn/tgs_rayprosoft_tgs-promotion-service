namespace Tgs.Promotion.Clients.Models.Promotion;

public record JoinedPromotionModel
{
	public string PromotionName { get; set; } = default!;
	public DateTimeOffset CreatedOn { get; set; }
}
