using System.Runtime.CompilerServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class PromotionIdsQueryService : IQueryService<PromotionIdsQuery, Guid>
{
	private readonly IPromotionRepository _promotionRepository;

	public PromotionIdsQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async IAsyncEnumerable<Guid> QueryAsync(
		PromotionIdsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var results = !string.IsNullOrEmpty(query.PromotionType)
			? _promotionRepository.QueryPromotionsByPromotionTypeAsync(
				query.BrandId,
				query.PromotionType,
				cancellationToken)
			: _promotionRepository.QueryPromotionsAsync(query.BrandId, cancellationToken);

		var promotions = await results
			.Where(p => p.Name.StartsWith(query.PromotionName))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var promotion in promotions)
			yield return promotion.Id;
	}
}
