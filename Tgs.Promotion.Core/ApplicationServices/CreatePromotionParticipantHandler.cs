using Microsoft.Extensions.Logging;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

public class CreatePromotionParticipantHandler : IGetService<CreatePromotionParticipantRequest, MessageResponse>
{
	private readonly ILogger<CreatePromotionParticipantHandler> _logger;
	private readonly IPromotionParticipantRepository _promotionParticipantRepository;
	private readonly IPromotionRepository _promotionRepository;
	private readonly IPromotionTypeRepository _promotionType;
	private readonly IPromotionJoinLockRepository _promotionJoinLockRepository;

	public CreatePromotionParticipantHandler(
		IPromotionRepository promotionRepository,
		IPromotionParticipantRepository promotionParticipantRepository,
		IPromotionTypeRepository promotionType,
		IPromotionJoinLockRepository promotionJoinLockRepository,
		ILogger<CreatePromotionParticipantHandler> logger)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

		_promotionParticipantRepository = promotionParticipantRepository ??
			throw new ArgumentNullException(nameof(promotionParticipantRepository));

		_promotionType = promotionType ??
			throw new ArgumentNullException(nameof(promotionType));
		_promotionJoinLockRepository = promotionJoinLockRepository ??
			throw new ArgumentNullException(nameof(promotionJoinLockRepository));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionParticipantRequest request,
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

		if (await _promotionType.IsDepositTypeAsync(promotion.PromotionType))
		{
			_logger.LogWarning(
				"Promotion({request.PromotionId}) Type(Deposit) invalid.",
				request.PromotionId);

			return new MessageResponse { Valid = false, Message = "Deposit invalid" };
		}

		if (!await _promotionJoinLockRepository.SetWhenNotExistsAsync(request.PlayerCode, request.PromotionId)
				.ConfigureAwait(false) || await _promotionParticipantRepository.ExistsAsync(x
				=> x.PlayerCode == request.PlayerCode && x.PromotionId == request.PromotionId))
		{
			return new MessageResponse { Valid = false, Message = "Promotion already joined." };
		}

		await _promotionParticipantRepository.CreatePromotionParticipantAsync(request.PromotionId,
			request.PlayerCode);

		return new MessageResponse { Valid = true };
	}
}
