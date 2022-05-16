namespace Tgs.Promotion.Domain.Models.PromotionBonus
{
	public record CreatePromotionBonusResponse : MessageResponse
	{
		public PromotionBonusModel[] PromotionBonus { get; set; } = default!;
	}
}
