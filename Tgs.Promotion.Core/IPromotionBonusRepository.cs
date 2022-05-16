using Tgs.Promotion.Core.Models.PromotionBonus;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Core
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
