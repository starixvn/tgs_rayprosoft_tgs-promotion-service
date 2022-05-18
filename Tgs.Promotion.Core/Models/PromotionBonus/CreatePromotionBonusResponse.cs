using Tgs.Promotion.Models.PromotionBonus;
namespace Tgs.Promotion.Models.PromotionBonus;

public record CreatePromotionBonusResponse : MessageResponse
{
	public PromotionBonusModel[] PromotionBonus { get; set; } = default!;
}
