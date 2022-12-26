using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class ChangePromotionStatusQueryService : IGetService<ChangePromotionStatusQuery, MessageResponse>
{
	private readonly IPromotionRepository _promotionFactory;
	private readonly ITgsService<Domain.Entities.PromotionRow> _promotionService;

	public ChangePromotionStatusQueryService(
		ITgsService<Domain.Entities.PromotionRow> promotionService,
		IPromotionRepository promotionFactory)
	{
		_promotionService =
			promotionService ?? throw new ArgumentNullException(nameof(promotionService));
		_promotionFactory =
			promotionFactory ?? throw new ArgumentNullException(nameof(promotionFactory));
	}

	public async ValueTask<MessageResponse> GetAsync(
		ChangePromotionStatusQuery request,
		CancellationToken cancellationToken)
	{
		var promotion = await _promotionService.GetQueries().FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

		if (promotion == null)
			return new MessageResponse { Valid = false, Message = "Player not found." };

		promotion.Status = (short)request.Status;

		await _promotionFactory.ChangePromotionStatusAsync(promotion);

		return new MessageResponse { Valid = true };
	}
}
