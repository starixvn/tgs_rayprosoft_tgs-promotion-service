using System.Text.Json;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class EventDetailQueryService : IGetService<EventDetailQuery, EventInfo?>
{
	private readonly IPromotionRepository _promotionRepository;

	public EventDetailQueryService(
		IPromotionRepository promotionRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public ValueTask<EventInfo?> GetAsync(
		EventDetailQuery query,
		CancellationToken cancellationToken = default)
		=> _promotionRepository.GetEventDetailAsync(
			query.PromotionId,
			query.EventId,
			cancellationToken);
}
