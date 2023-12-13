using System.Collections.Immutable;
using System.Text.Json;
using Json.Patch;
using Json.Pointer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.PlayerService.Client;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusManuallyCommandService(
	IPromotionRepository promotionRepository,
	ICashServiceClient cashServiceClient,
	IPlayerServiceClient playerServiceClient,
	IPlayerBucketForTypesClient playerBucketForTypesClient,
	ISerialNumberGenerator serialNumberGenerator,
	IPromotionDistributionRepository promotionDistributionRepository,
	ILogger<DistributePromotionBonusManuallyCommandService> logger) : ICommandService<DistributePromotionBonusManuallyCommand>
{
	private readonly ICashServiceClient _cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
	private readonly IPlayerServiceClient _playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
	private readonly IPromotionRepository _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
	private readonly ISerialNumberGenerator _serialNumberGenerator =
			serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));

	public async ValueTask ExecuteAsync(DistributePromotionBonusManuallyCommand command,
		CancellationToken cancellationToken = default)
	{
		var promotionDistribution = await promotionDistributionRepository.GetPromotionDistributionQueries()
			.FirstOrDefaultAsync(x => x.Id == command.PromotionDistributionId && x.PlayerCode == command.PlayerCode,
				cancellationToken);

		if (promotionDistribution is not { SentOn: null })
		{
			return;
		}

		var player = await _playerServiceClient.GetPlayerInfoAsync(
				command.PlayerCode,
				cancellationToken)
			.ConfigureAwait(false);

		if (player == null)
		{
			logger.LogWarning("Player with player code {PlayerCode} does not exist", command.PlayerCode);

			return;
		}

		var playerProperties = await _playerServiceClient.GetPlayerPropertiesAsync(
				player.PlayerCode,
				cancellationToken)
			.ConfigureAwait(false);

		var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);

		var plan = await _promotionRepository.FindPromotionDistributePlanDetailAsync(
			promotionDistribution.PlanId!.Value,
			cancellationToken);

		var bucketTypes = plan.Detail == null
			? Enumerable.Empty<string>()
			: JsonSerializer.Deserialize<IEnumerable<string>>(plan.Detail);

		var now = DateTime.UtcNow;

		await _promotionRepository.UpdatePromotionDistributeAsync(
				promotionDistribution.Id,
				now,
				cancellationToken)
			.ConfigureAwait(false);

		await Policy.Handle<TimeoutException>()
			.RetryAsync(1)
			.ExecuteAsync(async () => await playerBucketForTypesClient.CreateBucketForBucketTypesAsync(
					promotionDistribution.PlayerCode,
					promotionDistribution.Bucket,
					BucketType.Promotion,
					promotionDistribution.Id.ToString(),
					null,
					bucketTypes,
					cancellationToken)
				.ConfigureAwait(false))
			.ConfigureAwait(false);

		_ = await _cashServiceClient.PromotionAsync(
				walletId,
				promotionDistribution.BonusAmount,
				null,
				promotionDistribution.Id.ToString(),
				null,
				cancellationToken)
			.ConfigureAwait(false);

		await LogAsync(new PromotionDistributeInfo(
				promotionDistribution.Id,
				player.BrandId,
				promotionDistribution.PlanId,
				player.PlayerCode,
				promotionDistribution.PromotionId,
				promotionDistribution.BonusAmount,
				promotionDistribution.Bucket,
				promotionDistribution.ReferenceId,
				now),
			cancellationToken);
	}

	private async Task LogAsync(PromotionDistributeInfo distributeInfo, CancellationToken cancellationToken)
	{
		var jsonCreatePatch = new JsonPatch(PatchOperation.Add(
			JsonPointer.Create<PromotionDistributeInfo>(o => o),
			JsonSerializer.SerializeToNode(distributeInfo, PromotionUtility.SerializerOptions)));

		var distributeLog = new PromotionDistributeLog(
			await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			distributeInfo.Id,
			DistributeAction.Create,
			JsonSerializer.SerializeToDocument(jsonCreatePatch));

		await _promotionRepository.InsertPromotionDistributeLogAsync(
				distributeLog with
				{
					Id = await _serialNumberGenerator
						.GenerateAsync(cancellationToken)
						.ConfigureAwait(false),
					Action = DistributeAction.Distribute,
					ChangeSets = JsonSerializer.SerializeToDocument(distributeInfo.CreatePatch(
						distributeInfo.Id,
						PromotionUtility.SerializerOptions))
				},
				cancellationToken)
			.ConfigureAwait(false);
	}
}
