using System.Text.Json;
using Json.Patch;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ChangePromotionOrdersCommandService : ICommandService<ChangePromotionOrdersCommand>
{
	private readonly ISystemClock _systemClock;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IPromotionRepository _promotionRepository;

	public ChangePromotionOrdersCommandService(
		ISystemClock systemClock,
		ISerialNumberGenerator serialNumberGenerator,
		IPromotionRepository promotionRepository)
	{
		_systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	}

	public async ValueTask ExecuteAsync(ChangePromotionOrdersCommand command, CancellationToken cancellationToken = default)
	{
		var currentTime = _systemClock.UtcNow.UtcDateTime;

		foreach (var order in command.Orders)
		{
			var snapshot = await _promotionRepository.GetCurrentPromotionSnapshotAsync(
					order.PromotionId, 
					cancellationToken)
				.ConfigureAwait(false);

			if (snapshot is null)
				continue;

			var eventId = await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false);

			var updateData = snapshot with
			{
				DisplayOrder = order.Order,
				UpdatedByUsername = command.Modifier,
				UpdatedOn = currentTime,
				SnapshotEventId = eventId
			};

			var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

			await _promotionRepository.CreatePromotionEventAsync(
					eventId,
					snapshot.Id,
					PromotionEventAction.ChangeDisplayOrder,
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
}
