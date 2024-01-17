using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionRuleCommandService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository,
	PromotionSerializer promotionSerializer) : ICommandService<UpdatePromotionRuleCommand>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly PromotionSerializer _promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

	public async ValueTask ExecuteAsync(UpdatePromotionRuleCommand command, CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository.GetCurrentPromotionSnapshotAsync(
				command.PromotionId,
				cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null)
			return;

		if (command.GameType is not null)
		{
			var settings = _promotionSerializer.Deserialize<GameProviderSettings>(command.CampaignDetail);

			if (settings is not null)
				await _promotionRepository
					.SetPromotionGameProvidersAsync(command.PromotionId, settings.GameProviderIds, cancellationToken)
					.ConfigureAwait(false);
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
				cancellationToken)
			.ConfigureAwait(false);

		await _promotionRepository.UpdatePromotionSnapshotAsync(
				updateData,
				eventId,
				currentTime,
				cancellationToken)
			.ConfigureAwait(false);
	}
}
