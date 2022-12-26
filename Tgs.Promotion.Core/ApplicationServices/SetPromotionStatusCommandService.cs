using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class SetPromotionStatusCommandService : ICommandService<SetPromotionStatusCommand>
{
	private readonly ISystemClock _systemClock;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IPromotionRepository _promotionRepository;

	public SetPromotionStatusCommandService(
		ISystemClock systemClock,
		ISerialNumberGenerator serialNumberGenerator,
		IPromotionRepository promotionRepository)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async ValueTask ExecuteAsync(SetPromotionStatusCommand command, CancellationToken cancellationToken = default)
	{
		var snapshot =
			await _promotionRepository.GetCurrentPromotionSnapshotAsync(
					command.PromotionId, 
					cancellationToken)
				.ConfigureAwait(false);

		if (snapshot is null)
			return;

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			Status = command.Status, 
			UpdatedByUsername = command.Modifier,
			UpdatedOn = currentTime,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionUtility.SerializerOptions);

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.ChangeStatus,
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
