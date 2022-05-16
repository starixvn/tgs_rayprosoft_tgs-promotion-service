using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Core.Models;
using Tgs.Promotion.Core.Models.Promotion;
using Tgs.Promotion.Domain;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.ApplicationServices
{
	public class ChangePromotionStatusHandler : IGetService<ChangePromotionStatusRequest, MessageResponse>
	{
		private readonly ITgsService<Domain.Entities.Promotion> _promotionService;
		private readonly IPromotionRepository _promotionFactory;

		public ChangePromotionStatusHandler(
			ITgsService<Domain.Entities.Promotion> promotionService,
			IPromotionRepository promotionFactory)
		{
			_promotionService =
				promotionService ?? throw new ArgumentNullException(nameof(promotionService));
			_promotionFactory =
				promotionFactory ?? throw new ArgumentNullException(nameof(promotionFactory));
		}

		public async ValueTask<MessageResponse> GetAsync(
			ChangePromotionStatusRequest request,
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
}
