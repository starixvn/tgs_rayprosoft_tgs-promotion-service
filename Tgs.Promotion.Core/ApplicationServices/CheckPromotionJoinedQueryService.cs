using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CheckPromotionJoinedQueryService : IGetService<CheckPromotionJoinedQuery, bool>
{
	private readonly IPromotionParticipantRepository _service;

	public CheckPromotionJoinedQueryService(IPromotionParticipantRepository service)
	{
		_service = service ?? throw new ArgumentNullException(nameof(service));
	}

	public ValueTask<bool> GetAsync(CheckPromotionJoinedQuery query, CancellationToken cancellationToken = default)
		=> _service.ExistsAsync(pp => pp.PromotionId == query.PromotionId && pp.PlayerCode == query.PlayerCode);
}
