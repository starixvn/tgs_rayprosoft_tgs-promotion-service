using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class RevertPromotionAmountCommandService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository) : ICommandService<RevertPromotionAmountCommand>
{
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));

	public async ValueTask ExecuteAsync(
		RevertPromotionAmountCommand command,
		CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository
			.GetCurrentPromotionSnapshotAsync(command.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null)
			return;

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var amount = snapshot.Percentage * command.Amount / 100M;
		amount = amount > snapshot.BonusAmountLimit ? snapshot.BonusAmountLimit : amount;

		var updateData = snapshot with
		{
			AmountUsed = snapshot.AmountUsed - amount,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateTotalUsedAmount,
				JsonSerializer.SerializeToDocument(changeSets),
				null,
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
