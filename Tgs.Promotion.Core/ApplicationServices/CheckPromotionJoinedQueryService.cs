using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CheckPromotionJoinedQueryService(IPromotionParticipantRepository service) : IGetService<CheckPromotionJoinedQuery, bool>
{
	private readonly IPromotionParticipantRepository _service = service ?? throw new ArgumentNullException(nameof(service));

	public ValueTask<bool> GetAsync(CheckPromotionJoinedQuery query, CancellationToken cancellationToken = default)
		=> _service.ExistsAsync(pp => pp.PromotionId == query.PromotionId && pp.PlayerCode == query.PlayerCode);
}
