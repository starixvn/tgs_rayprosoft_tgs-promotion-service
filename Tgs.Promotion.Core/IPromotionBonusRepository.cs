using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models.PromotionBonus;

namespace Tgs.Promotion;

public interface IPromotionBonusRepository
{
	ValueTask<PromotionBonus[]> CreatePromotionBonusAsync(PromotionBonusModel[] model);

	ValueTask<PromotionBonus> CreatePromotionBonusAsync(PromotionBonusModel model);

	IQueryable<PromotionPlayer> GetPromotionBonusPlayersQueries(
				Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery = null,
		bool tracking = false);

	ValueTask UpdatePromotionBonusPlayersAsync(IEnumerable<int> playerIds);
}
