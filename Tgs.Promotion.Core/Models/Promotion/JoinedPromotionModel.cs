namespace Tgs.Promotion.Core.Models.Promotion;

public class JoinedPromotionModel
{
	public DateTimeOffset CreatedOn { get; set; }

	public string PromotionName { get; set; } = default!;
}
