using System.Collections.Immutable;
using System.Text.Json;
using Json.Patch;
using Json.Pointer;
using Polly;
using Polly.Retry;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.PlayerService.Client;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusCommandService(
	IPromotionRepository promotionRepository,
	IServiceCoordinator serviceCoordinator,
	ICashServiceClient cashServiceClient,
	IPlayerServiceClient playerServiceClient,
	IPlayerBucketForTypesClient layerBucketForTypesClient,
	ISerialNumberGenerator serialNumberGenerator)
	: IGetService<DistributePromotionBonusCommand, Guid>
{
	private static readonly ResiliencePipeline _CreateBucketPipeline = new ResiliencePipelineBuilder()
		.AddRetry(new RetryStrategyOptions
		{
			ShouldHandle = new PredicateBuilder().Handle<TimeoutException>(),
			MaxRetryAttempts = 1
		})
		.Build();

	public async ValueTask<Guid> GetAsync(DistributePromotionBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await playerServiceClient.GetPlayerInfoAsync(
			command.PlayerCode,
			cancellationToken).ConfigureAwait(false)
			?? throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var playerProperties = await playerServiceClient.GetPlayerPropertiesAsync(
			player.PlayerCode,
			cancellationToken).ConfigureAwait(false)
			?? throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);

		var plan = (await promotionRepository.FindPromotionDistributePlanAsync(
			command.PlanId,
			cancellationToken).ConfigureAwait(false))!;

		var promotion = (await serviceCoordinator.GetAsync<GetPromotionMainInfoQuery, PromotionMainInfo?>(
			new(plan.PromotionId), cancellationToken).ConfigureAwait(false))!;

		var distributeId = await serialNumberGenerator.GenerateAsync(
			cancellationToken).ConfigureAwait(false);

		var createDistributeInfo = new PromotionDistributeInfo(
			distributeId,
			player.BrandId,
			command.PlanId,
			player.PlayerCode,
			plan.PromotionId,
			command.Amount,
			command.NeedTurnover,
			command.ReferenceId,
			null);

		await promotionRepository.CreatePromotionDistributeAsync(
			createDistributeInfo,
			command.Distributor,
			cancellationToken).ConfigureAwait(false);

		var jsonCreatePatch = new JsonPatch(PatchOperation.Add(
			JsonPointer.Create<PromotionDistributeInfo>(o => o),
			JsonSerializer.SerializeToNode(createDistributeInfo, PromotionUtility.SerializerOptions)));

		var distributeLog = new PromotionDistributeLog(
			await serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			distributeId,
			DistributeAction.Create,
			JsonSerializer.SerializeToDocument(jsonCreatePatch));

		await promotionRepository.InsertPromotionDistributeLogAsync(
			distributeLog,
			cancellationToken).ConfigureAwait(false);

		if (promotion.IsAutoSent)
		{
			if (command.NeedTurnover > 0M)
				await _CreateBucketPipeline.ExecuteAsync(
					ct => layerBucketForTypesClient.CreateBucketForBucketTypesAsync(
						player.PlayerCode,
						command.NeedTurnover,
						BucketType.Promotion,
						distributeId.ToString(),
						null,
						command.BucketTypes,
						ct),
					cancellationToken).ConfigureAwait(false);

			if (command.Amount > 0M)
				_ = await cashServiceClient.PromotionAsync(
					walletId,
					command.Amount,
					null,
					distributeId.ToString(),
					null,
					cancellationToken).ConfigureAwait(false);

			var distributedInfo = createDistributeInfo with
			{
				SentOn = DateTime.UtcNow
			};

			await promotionRepository.UpdatePromotionDistributeAsync(
				distributedInfo.Id,
				distributedInfo.SentOn.Value,
				command.Distributor,
				cancellationToken).ConfigureAwait(false);

			await promotionRepository.InsertPromotionDistributeLogAsync(
				distributeLog with
				{
					Id = await serialNumberGenerator.GenerateAsync(
						cancellationToken).ConfigureAwait(false),
					Action = DistributeAction.Distribute,
					ChangeSets = JsonSerializer.SerializeToDocument(createDistributeInfo.CreatePatch(
						distributedInfo,
						PromotionUtility.SerializerOptions))
				},
				cancellationToken).ConfigureAwait(false);
		}

		return distributeId;
	}
}
