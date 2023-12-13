using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionNamesQueryService(
	IPromotionRepository promotionRepository) : IQueryService<GetPromotionNamesQuery, PromotionNameModel>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public IAsyncEnumerable<PromotionNameModel> QueryAsync(
		GetPromotionNamesQuery query, CancellationToken cancellationToken = default)
		=> _promotionRepository.GetPromotionQueries().Where(x => query.PromotionIds.Contains(x.Id))
			.Select(x => new PromotionNameModel
			{
				Id = x.Id,
				Name = x.Name
			})
			.AsAsyncEnumerable();
}
