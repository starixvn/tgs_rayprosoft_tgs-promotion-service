namespace Tgs.Promotion.Models.PromotionDispatch;

public class PromotionDispatchConcatModel
{
	public string BonusAmount { get; set; }

	public DateTimeOffset CreatedOn { get; set; }

	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }

	public Guid PromotionId { get; set; }

	public string PromotionName { get; set; } = default!;

	public string PromotionType { get; set; } = default!;

	public DateTimeOffset? SentOn { get; set; }

	public string Username { get; set; } = default!;
}
