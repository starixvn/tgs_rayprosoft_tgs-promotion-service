using Microsoft.Extensions.Logging;
using Tgs.Promotion.Core.Models;
using Tgs.Promotion.Core.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.ApplicationServices
{
	public class
		CreateDepositPromotionParticipantHandler : IGetService<CreateDepositPromotionParticipantRequest, MessageResponse>
	{
		private readonly ILogger<CreatePromotionParticipantHandler> _logger;
		private readonly IPromotionParticipantRepository _promotionParticipantRepository;
		private readonly IPromotionRepository _promotionRepository;

		public CreateDepositPromotionParticipantHandler(
			IPromotionRepository promotionRepository,
			IPromotionParticipantRepository promotionParticipantRepository,
			ILogger<CreatePromotionParticipantHandler> logger)
		{
			_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

			_promotionParticipantRepository = promotionParticipantRepository ??
				throw new ArgumentNullException(nameof(promotionParticipantRepository));

			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async ValueTask<MessageResponse> GetAsync(
			CreateDepositPromotionParticipantRequest request,
			CancellationToken cancellationToken)
		{
			var promotion = await _promotionRepository
				.GetPromotionByIdAsync(request.PromotionId)
				.ConfigureAwait(false);

			if (promotion == null)
			{
				_logger.LogWarning($"Promotion({request.PromotionId}) is null");

				return new MessageResponse { Valid = false, Message = "Can't find promotion" };
			}

			var currentDate = DateTimeOffset.Now;

			if (promotion.Status != (short)PromotionStatus.Enabled
				&& promotion.From <= currentDate
				&& promotion.To > currentDate)
			{
				_logger.LogWarning($"Promotion({request.PromotionId}) invalid.");

				return new MessageResponse { Valid = false, Message = "Promotion invalid" };
			}

			await _promotionParticipantRepository.CreatePromotionParticipantAsync(request.PromotionId,
				request.PlayerCode);

			return new MessageResponse { Valid = true };
		}
	}
}
