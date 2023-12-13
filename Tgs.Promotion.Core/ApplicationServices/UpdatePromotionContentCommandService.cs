using System.Text.Json.Serialization;
using System.Text.Json;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;
using Json.Patch;

namespace Tgs.Promotion.ApplicationServices;

internal class UpdatePromotionContentCommandService(
	ISystemClock systemClock,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionRepository promotionRepository) : ICommandService<UpdatePromotionContentCommand>
{
	private readonly ISystemClock _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
	private readonly ISerialNumberGenerator _serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));

	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};

	public async ValueTask ExecuteAsync(UpdatePromotionContentCommand command, CancellationToken cancellationToken = default)
	{
		var snapshot = await _promotionRepository.GetCurrentPromotionSnapshotAsync(command.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		if (snapshot is null)
			return;

		var eventId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var currentTime = _systemClock.UtcNow.UtcDateTime;

		var updateData = snapshot with
		{
			Content = command.Content,
			Localization = JsonSerializer.Serialize(command.Localization, _SerializerOptions),
			SubSubject = command.SubSubject,
			Terms = command.Convention,
			DesktopBanner = command.PersonalComputerBanner,
			MobileBanner = command.MobileBanner,
			UpdatedByUsername = command.Modifier,
			UpdatedOn = currentTime,
			SnapshotEventId = eventId
		};

		var changeSets = snapshot.CreatePatch(updateData, PromotionEventSourceUtility.SerializerOptions);

		await _promotionRepository.CreatePromotionEventAsync(
				eventId,
				snapshot.Id,
				PromotionEventAction.UpdateContent,
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
