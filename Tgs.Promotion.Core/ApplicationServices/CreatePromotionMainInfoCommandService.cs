using System.Text.Json;
using Json.More;
using Json.Patch;
using Json.Pointer;
using Tgs.Promotion.Models;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CreatePromotionMainInfoCommandService(
	ISerialNumberGenerator serialNumberGenerator,
	ISystemClock systemClock,
	IPromotionRepository promotionRepository,
	IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> queryPromotionTypeGroupService) : IGetService<CreatePromotionMainInfoCommand, Guid>
{
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> _queryPromotionTypeGroupService = queryPromotionTypeGroupService ?? throw new ArgumentNullException(nameof(queryPromotionTypeGroupService));

	public async ValueTask<Guid> GetAsync(CreatePromotionMainInfoCommand command, CancellationToken cancellationToken = default)
	{
		var promotionType = await _queryPromotionTypeGroupService
			.QueryAsync(new PromotionTypeGroupsQuery(), cancellationToken)
			.SelectMany(group => group.Types.ToAsyncEnumerable())
			.FirstOrDefaultAsync(type => type.Name == command.PromotionType, cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;
		
		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var createPromotionId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotionSnapshot = new PromotionSnapshot(
			Id: createPromotionId,
			Name: command.Name,
			BrandId: command.BrandId,
			From: command.EventDuring.From.ToUniversalTime(),
			To: command.EventDuring.To.ToUniversalTime(),
			Status: PromotionStatus.Disabled,
			PromotionType: command.PromotionType,
			SettlementIntervalType: promotionType?.SettlementIntervalType,
			AutoSentPlayerBonus: command.IsAutoSent,
			BonusAmountLimit: 0M,
			Unlimited: command.PromotionLimitAmount is null ? (short)1 : (short)0,
			GameType: null,
			Percentage: 0M,
			TotalAmountLimit: command.PromotionLimitAmount ?? 0M,
			TurnoverMultiple: 0M,
			CampaignDetail: JsonDocument.Parse("{}"),
			Content: null,
			Localization: null,
			DesktopBanner: null,
			MobileBanner: null,
			SubSubject: null,
			Terms: null,
			CreatedOn: currentTime,
			CreatedByUsername: command.Modifier,
			UpdatedOn: currentTime,
			UpdatedByUsername: null,
			AmountUsed: 0M,
			AmountUsedSnapshotId:null,
			DisplayOrder: 1,
			VipLevelId:null,
			SnapshotEventId: eventId);

		var changeSets = new JsonPatch(
				PatchOperation.Add(
				JsonPointer.Create<PromotionSnapshot>(o => o),
				JsonSerializer.SerializeToNode(promotionSnapshot, PromotionEventSourceUtility.SerializerOptions)))
			.ToJsonDocument();

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				createPromotionId,
				PromotionEventAction.CreateMainInfo,
				changeSets,
				command.Modifier,
				cancellationToken)
			.ConfigureAwait(false);

		await _promotionRepository
			.UpdatePromotionSnapshotAsync(promotionSnapshot, eventId, currentTime, cancellationToken)
			.ConfigureAwait(false);

		return createPromotionId;
	}
}
