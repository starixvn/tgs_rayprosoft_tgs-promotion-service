using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models.PromotionBonus;

namespace Tgs.Promotion;

public interface IPromotionBonusRepository
{
	IQueryable<PromotionPlayer> GetPromotionBonusPlayersQueries(
		Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery = null,
		bool tracking = false);

	ValueTask<PromotionBonus[]> CreatePromotionBonusAsync(PromotionBonusModel[] model);

	ValueTask UpdatePromotionBonusPlayersAsync(PromotionPlayer[] players);
}
