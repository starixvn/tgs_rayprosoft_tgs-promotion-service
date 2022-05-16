using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Services;

internal class
	GetPromotionBonusOfDepositQueryService : IQueryService<GetPromotionBonusOfDepositQuery,
		PromotionBonusOfDepositModel>
{
	private readonly IPromotionPlayerRepository _promotionPlayerRepository;

	public GetPromotionBonusOfDepositQueryService(
		IPromotionPlayerRepository promotionPlayerRepository)
	{
		_promotionPlayerRepository = promotionPlayerRepository ??
			throw new ArgumentNullException(nameof(promotionPlayerRepository));
	}

	public IAsyncEnumerable<PromotionBonusOfDepositModel> QueryAsync(
		GetPromotionBonusOfDepositQuery query, CancellationToken cancellationToken = default)
		=> _promotionPlayerRepository.GetPromotionPlayerQueries()
			.Where(x => query.PromotionPlayerIds.Contains(x.Id))
			.Select(x => new PromotionBonusOfDepositModel(x.Id, x.BonusAmount)).AsAsyncEnumerable();
}
