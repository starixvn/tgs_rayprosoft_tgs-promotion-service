using System.Text.Json;
using Json.Patch;
using Tgs.Promotion.Models;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionMainInfoCommandService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository,
	IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> queryPromotionTypeGroupService) : ICommandService<UpdatePromotionMainInfoCommand>
{
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> _queryPromotionTypeGroupService = queryPromotionTypeGroupService;

	public async ValueTask ExecuteAsync(UpdatePromotionMainInfoCommand command, CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository.GetCurrentPromotionSnapshotAsync(command.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null)
			return;

		var promotionType = await _queryPromotionTypeGroupService
			.QueryAsync(new PromotionTypeGroupsQuery(), cancellationToken)
			.SelectMany(group => group.Types.ToAsyncEnumerable())
			.FirstOrDefaultAsync(type => type.Name == command.PromotionType, cancellationToken)
			.ConfigureAwait(false);

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			Name = command.Name,
			From = command.EventDuring.From,
			To = command.EventDuring.To,
			PromotionType = command.PromotionType,
			SettlementIntervalType = promotionType?.SettlementIntervalType,
			AutoSentPlayerBonus = command.IsAutoSent,
			Unlimited = command.PromotionLimitAmount is null ? (short)1 : (short)0,
			TotalAmountLimit = command.PromotionLimitAmount ?? 0M,
			UpdatedByUsername = command.Modifier,
			UpdatedOn = currentTime,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);
		
		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateMainInfo,
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
