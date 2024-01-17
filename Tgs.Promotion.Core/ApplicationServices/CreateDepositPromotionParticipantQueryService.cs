using Microsoft.Extensions.Logging;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class CreateDepositPromotionParticipantQueryService(
	IPromotionRepository promotionRepository,
	IPromotionParticipantRepository promotionParticipantRepository,
	ILogger<CreateDepositPromotionParticipantQueryService> logger) : IGetService<CreateDepositPromotionParticipantQuery, MessageResponse>
{
	private readonly ILogger<CreateDepositPromotionParticipantQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	private readonly IPromotionParticipantRepository _promotionParticipantRepository = promotionParticipantRepository ??
			throw new ArgumentNullException(nameof(promotionParticipantRepository));

	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	public async ValueTask<MessageResponse> GetAsync(
		CreateDepositPromotionParticipantQuery request,
		CancellationToken cancellationToken)
	{
		var promotion = await _promotionRepository
			.GetPromotionByIdAsync(request.PromotionId)
			.ConfigureAwait(false);

		if (promotion == null)
		{
			_logger.LogWarning("Promotion({request.PromotionId}) is null", request.PromotionId);

			return new MessageResponse { Valid = false, Message = "Can't find promotion" };
		}

		var currentDate = DateTimeOffset.Now;

		if (promotion.Status != (short)PromotionStatus.Enabled
			&& promotion.From <= currentDate
			&& promotion.To > currentDate)
		{
			_logger.LogWarning("Promotion({request.PromotionId}) invalid.", request.PromotionId);

			return new MessageResponse { Valid = false, Message = "Promotion invalid" };
		}

		await _promotionParticipantRepository.CreatePromotionParticipantAsync(request.PromotionId,
			request.PlayerCode);

		return new MessageResponse { Valid = true };
	}
}
