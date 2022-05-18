using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.Promotion;

namespace Tgs.Promotion.Repository;

public class PromotionBonusRepository : IPromotionBonusRepository
{
	private readonly ITgsService<PromotionBonus> _promotionBonusService;
	private readonly ITgsService<PromotionPlayer> _promotionPlayerService;

	public PromotionBonusRepository(ITgsService<PromotionBonus> promotionBonusService,
		ITgsService<PromotionPlayer> promotionPlayerService)
	{
		_promotionBonusService = promotionBonusService;
		_promotionPlayerService = promotionPlayerService;
	}

	public IQueryable<PromotionPlayer> GetPromotionBonusPlayersQueries(
		Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>? preQuery, bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionPlayerService.GetQueries())
			: preQuery(_promotionPlayerService.GetQueries()).AsNoTracking();
	}

	public async ValueTask<PromotionBonus[]> CreatePromotionBonusAsync(PromotionBonusModel[] models)
	{
		var bonus = models.Select(model => new PromotionBonus
		{
			Id = model.Id,
			BrandId = model.BrandId,
			PromotionId = model.PromotionId,
			From = model.From.UtcDateTime,
			To = model.To.UtcDateTime,
			PromotionType = model.PromotionType,
			TotalAmountLimit = model.TotalAmountLimit,
			GameType = (short?)model.GameType,
			Percentage = model.Percentage,
			BonusAmountLimit = model.BonusAmountLimit,
			TurnoverMultiple = model.TurnoverMultiple,
			CreatedOn = model.CreatedOn.UtcDateTime,
			PromotionPlayers = model.Players.Select(x => new PromotionPlayer
			{
				PlayerCode = x.PlayerCode,
				PromotionBonusId = model.Id,
				BonusAmount = x.BonusAmount,
				SentOn = model.SentOn?.UtcDateTime
			}).ToHashSet()
		}).ToArray();

		await _promotionBonusService.CreateEntitiesAsync(bonus);

		return bonus;
	}

	public ValueTask UpdatePromotionBonusPlayersAsync(PromotionPlayer[] players) =>
		_promotionPlayerService.UpdateEntitiesAsync(players);
}
