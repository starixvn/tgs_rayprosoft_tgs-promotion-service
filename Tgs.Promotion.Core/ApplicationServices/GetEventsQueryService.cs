using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetEventsQueryService : IQueryService<GetEventsQuery, EventInfo>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetEventsQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public IAsyncEnumerable<EventInfo> QueryAsync(
		GetEventsQuery query, 
		CancellationToken cancellationToken = default)
		=> _promotionRepository.QueryPromotionEventsAsync(
			query.PromotionId,
			cancellationToken);
}
