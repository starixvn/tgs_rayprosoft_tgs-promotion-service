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
	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

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

		var player = await playerServiceClient.GetPlayerInfoAsync(
				command.PlayerCode,
				cancellationToken)
			.ConfigureAwait(false);

		if (player == null)
		{
			logger.LogWarning("Player with player code {PlayerCode} does not exist", command.PlayerCode);

			return;
		}

		var playerProperties = await playerServiceClient.GetPlayerPropertiesAsync(
				player.PlayerCode,
				cancellationToken)
			.ConfigureAwait(false);

		var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);

		var plan = await promotionRepository.FindPromotionDistributePlanDetailAsync(
			promotionDistribution.PlanId!.Value,
			cancellationToken);

		var promotionRule = plan.Detail == null
			? null
			: JsonSerializer.Deserialize<PromotionCampaignDetailRule>(plan.Detail,
				_SerializerOptions);

		var detail = promotionRule == null
			? null
			: JsonSerializer.Deserialize<DetailBucketType?>(promotionRule.CampaignDetail,
				_SerializerOptions);

		var now = DateTime.UtcNow;

		await promotionRepository.UpdatePromotionDistributeAsync(
				promotionDistribution.Id,
				now,
				command.Distributor,
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
					detail?.BucketTypes,
					cancellationToken)
				.ConfigureAwait(false))
			.ConfigureAwait(false);

		_ = await cashServiceClient.PromotionAsync(
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
			await serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			distributeInfo.Id,
			DistributeAction.Create,
			JsonSerializer.SerializeToDocument(jsonCreatePatch));

		await promotionRepository.InsertPromotionDistributeLogAsync(
				distributeLog with
				{
					Id = await serialNumberGenerator
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
