using Tgs.Promotion.Core.Extensions;
using Tgs.Promotion.Domain;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers
{
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
}
