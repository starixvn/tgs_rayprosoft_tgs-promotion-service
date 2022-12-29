using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetEventDetailQueryService : IGetService<GetEventDetailQuery, EventInfo?>
{
	private readonly IPromotionRepository _promotionRepository;

	public GetEventDetailQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<EventInfo?> GetAsync(
		GetEventDetailQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.GetEventDetailAsync(
			query.PromotionId,
			query.EventId,
			cancellationToken);
}
