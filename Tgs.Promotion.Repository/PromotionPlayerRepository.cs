using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionPlayerRepository : IPromotionPlayerRepository
{
	private readonly ITgsService<PromotionPlayer> _promotionPlayerService;

	public PromotionPlayerRepository(ITgsService<PromotionPlayer> promotionPlayerService)
	{
		_promotionPlayerService = promotionPlayerService;
	}

	public async ValueTask<PromotionPlayer?> GetPromotionPlayerAsync(
		Expression<Func<PromotionPlayer?, bool>> predicate,
		Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery)
	{
		preQuery ??= c => c;

		return await preQuery(_promotionPlayerService.GetQueries()).FirstOrDefaultAsync(predicate);
	}

	public IQueryable<PromotionPlayer> GetPromotionPlayerQueries(
			Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery,
		bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionPlayerService.GetQueries())
			: preQuery(_promotionPlayerService.GetQueries()).AsNoTracking();
	}

	public async ValueTask SetSentOnAsync(int promotionPlayerId)
	{
		var promotionPlayer =  await _promotionPlayerService.GetQueries().SingleAsync(x => x.Id == promotionPlayerId);

		promotionPlayer.SentOn = DateTime.UtcNow;

		await _promotionPlayerService.UpdateEntityAsync(promotionPlayer);
	}
}
