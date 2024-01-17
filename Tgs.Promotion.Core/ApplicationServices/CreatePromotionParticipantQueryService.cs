using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.ApplicationServices;

public class CreatePromotionParticipantQueryService(
	IPromotionRepository promotionRepository,
	IPromotionParticipantRepository promotionParticipantRepository,
	IPromotionTypeRepository promotionType,
	IPromotionJoinLockRepository promotionJoinLockRepository,
	IMessageSender messageSender,
	ILogger<CreatePromotionParticipantQueryService> logger) : IGetService<CreatePromotionParticipantQuery, MessageResponse>
{
	private readonly ILogger<CreatePromotionParticipantQueryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

	private readonly IMessageSender _messageSender = messageSender ??
			throw new ArgumentNullException(nameof(messageSender));

	private readonly IPromotionJoinLockRepository _promotionJoinLockRepository = promotionJoinLockRepository ??
			throw new ArgumentNullException(nameof(promotionJoinLockRepository));

	private readonly IPromotionParticipantRepository _promotionParticipantRepository = promotionParticipantRepository ??
			throw new ArgumentNullException(nameof(promotionParticipantRepository));

	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	private readonly IPromotionTypeRepository _promotionType = promotionType ??
			throw new ArgumentNullException(nameof(promotionType));

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionParticipantQuery request,
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

		if (promotion.Status != (short)PromotionStatus.Enabled ||
			promotion.From > currentDate ||
			promotion.To < currentDate)
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

		var alreadyJoined = !await _promotionJoinLockRepository
			.SetWhenNotExistsAsync(request.PlayerCode, request.PromotionId)
			.ConfigureAwait(false) || await _promotionParticipantRepository.ExistsAsync(x
			=> x.PlayerCode == request.PlayerCode && x.PromotionId == request.PromotionId);

		var messageData = new
		{
			request.PromotionId,
			request.PlayerCode
		};

		switch (promotion.PromotionType.ToLower())
		{
			case "register":
				_ = await _messageSender!.RequestAsync<ReadOnlyMemory<byte>, ReadOnlyMemory<byte>>(
						"tgs.promotion.register.distribute",
						Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageData)),
						cancellationToken: cancellationToken).ConfigureAwait(false);
				break;
		}

		if (!alreadyJoined)
		{
			await _promotionParticipantRepository.CreatePromotionParticipantAsync(
				request.PromotionId,
				request.PlayerCode).ConfigureAwait(false);
		}

		return new MessageResponse { Valid = true };
	}
}
