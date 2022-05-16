namespace Tgs.Promotion.Core.Models.PromotionBonus;

public class PromotionBonusPlayerModel
{
	public decimal BonusAmount { get; set; }

	public int Id { get; set; }

	public string PlayerCode { get; set; } = default!;

	public Guid PromotionBonusId { get; set; }

	public string PromotionType { get; set; } = default!;
}
