namespace Tgs.Promotion.Client.Models.PromotionDispatch;

public class PromotionDispatchModel
{
	public string PlayerCode { get; set; } = default!;

	public string Username { get; set; } = default!;

	public Guid PromotionId { get; set; }

	public Guid PromotionBonusId { get; set; }

	public string PromotionName { get; set; } = default!;

	public string PromotionGroup { get; set; } = default!;

	public string PromotionType { get; set; } = default!;

	public decimal BonusAmount { get; set; }

	public DateTimeOffset CreatedOn { get; set; }

	public DateTimeOffset? SentOn { get; set; }
}
