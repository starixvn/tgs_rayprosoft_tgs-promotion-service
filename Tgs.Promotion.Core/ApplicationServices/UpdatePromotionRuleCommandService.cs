using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionRuleCommandService : ICommandService<UpdatePromotionRuleCommand>
{
	private readonly ISystemClock _systemClock;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IPromotionRepository _promotionRepository;
	private readonly PromotionSerializer _promotionSerializer;

	public UpdatePromotionRuleCommandService(
		ISystemClock systemClock,
		ISerialNumberGenerator serialNumberGenerator,
		IPromotionRepository promotionRepository, 
		PromotionSerializer promotionSerializer)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
	}

	public async ValueTask ExecuteAsync(UpdatePromotionRuleCommand command, CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository.GetCurrentPromotionSnapshotAsync(
				command.PromotionId, 
				cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null)
			return;

		var settings = _promotionSerializer.Deserialize<CampaignDetailSettings>(command.CampaignDetail);

		if (settings is not null)
		{
			if (command.GameType is not null)
			{
				await _promotionRepository.SetPromotionGameProvidersAsync(
					command.PromotionId, 
					settings.GameProviderIds!, 
					cancellationToken).ConfigureAwait(false);
			}

			if (settings.VipLevels is not null)
			{
				await _promotionRepository.SetPromotionVipLevelsAsync(
					command.PromotionId, 
					settings.VipLevels!,
					command.Modifier,
					cancellationToken).ConfigureAwait(false);
			}
		}

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			CampaignDetail = command.CampaignDetail,
			BonusAmountLimit = command.BonusAmountLimit,
			Percentage = command.Percentage,
			TurnoverMultiple = command.TurnoverMultiple,
			GameType = command.GameType,
			UpdatedByUsername = command.Modifier,
			UpdatedOn = currentTime,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateRule,
				JsonSerializer.SerializeToDocument(changeSets),
				command.Modifier,
				cancellationToken).ConfigureAwait(false);

		await _promotionRepository.UpdatePromotionSnapshotAsync(
				updateData,
				eventId,
				currentTime,
				cancellationToken).ConfigureAwait(false);
	}
}
