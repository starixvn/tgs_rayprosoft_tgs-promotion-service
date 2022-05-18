using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class
	GetPlayerOperationsQueryHandler : IGetService<GetJoinedPromotionsRequest, PagedResponse<JoinedPromotionModel>>
{
	private readonly IPromotionParticipantRepository _service;

	public GetPlayerOperationsQueryHandler(IPromotionParticipantRepository service)
	{
		_service = service;
	}

	public async ValueTask<PagedResponse<JoinedPromotionModel>> GetAsync(GetJoinedPromotionsRequest request,
		CancellationToken cancellationToken) =>
		await _service.GetPromotionParticipantQueries(q => q.Where(p =>
				p.CreatedOn >= request.From && p.CreatedOn <= request.To && p.PlayerCode == request.PlayerCode))
			.Select(x => new JoinedPromotionModel
			{
				CreatedOn = x.CreatedOn,
				PromotionName = x.Promotion.Name
			}).CreatePagedResponseAsync(request);
}
