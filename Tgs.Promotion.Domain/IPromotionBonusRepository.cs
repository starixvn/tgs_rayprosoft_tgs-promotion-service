using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Models.PromotionBonus;

namespace Tgs.Promotion.Domain
{
	public interface IPromotionBonusRepository
	{
		IQueryable<PromotionPlayer> GetPromotionBonusPlayersQueries(
			Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery = null,
			bool tracking = false);

		ValueTask<PromotionBonus[]> CreatePromotionBonusAsync(PromotionBonusModel[] model);

		ValueTask UpdatePromotionBonusPlayersAsync(PromotionPlayer[] players);
	}
}
