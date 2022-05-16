namespace Tgs.Promotion.Core.Models.PromotionBonus
{
	public record CreatePromotionBonusResponse : MessageResponse
	{
		public PromotionBonusModel[] PromotionBonus { get; set; } = default!;
	}
}
