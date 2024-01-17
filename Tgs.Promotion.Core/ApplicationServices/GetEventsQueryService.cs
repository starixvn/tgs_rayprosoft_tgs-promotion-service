using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetEventsQueryService(
	IPromotionRepository promotionRepository) : IQueryService<GetEventsQuery, EventInfo>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public IAsyncEnumerable<EventInfo> QueryAsync(
		GetEventsQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.QueryPromotionEventsAsync(
			query.PromotionId,
			cancellationToken);
}
