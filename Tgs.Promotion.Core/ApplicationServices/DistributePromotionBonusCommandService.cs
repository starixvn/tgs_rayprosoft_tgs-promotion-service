using System.Collections.Immutable;
using System.Text.Json;
using Json.Patch;
using Json.Pointer;
using Polly;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.PlayerService.Client;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusCommandService : IGetService<DistributePromotionBonusCommand, Guid>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionRepository _promotionRepository;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IServiceCoordinator _serviceCoordinator;
	private readonly IPlayerBucketForTypesClient _playerBucketForTypesClient;

	public DistributePromotionBonusCommandService(
		IPromotionRepository promotionRepository,
		IServiceCoordinator serviceCoordinator,
		ICashServiceClient cashServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPlayerBucketForTypesClient layerBucketForTypesClient,
		ISerialNumberGenerator serialNumberGenerator
		)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_serviceCoordinator = serviceCoordinator ?? throw new ArgumentNullException(nameof(serviceCoordinator));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_playerBucketForTypesClient = layerBucketForTypesClient;
	}

	public async ValueTask<Guid> GetAsync(DistributePromotionBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient.GetPlayerInfoAsync(
			command.PlayerCode,
			cancellationToken).ConfigureAwait(false) 
			?? throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var playerProperties = await _playerServiceClient.GetPlayerPropertiesAsync(
			player.PlayerCode,
			cancellationToken).ConfigureAwait(false) 
			?? throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);

		var plan = (await _promotionRepository.FindPromotionDistributePlanAsync(
			command.PlanId,
			cancellationToken).ConfigureAwait(false))!;

		var promotion = (await _serviceCoordinator.GetAsync<GetPromotionMainInfoQuery, PromotionMainInfo?>(
			new(plan.PromotionId), cancellationToken).ConfigureAwait(false))!;

		var distributeId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

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

		await _promotionRepository.CreatePromotionDistributeAsync(
			createDistributeInfo,
			command.Distributor,
			cancellationToken).ConfigureAwait(false);

		var jsonCreatePatch = new JsonPatch(PatchOperation.Add(
			JsonPointer.Create<PromotionDistributeInfo>(o => o),
			JsonSerializer.SerializeToNode(createDistributeInfo, PromotionUtility.SerializerOptions)));

		var distributeLog = new PromotionDistributeLog(
			await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false),
			distributeId,
			DistributeAction.Create,
			JsonSerializer.SerializeToDocument(jsonCreatePatch));

		await _promotionRepository.InsertPromotionDistributeLogAsync(
			distributeLog,
			cancellationToken).ConfigureAwait(false);

		if (promotion.IsAutoSent)
		{
			await Policy.Handle<TimeoutException>()
				.RetryAsync(1)
				.ExecuteAsync(async () => await _playerBucketForTypesClient.CreateBucketForBucketTypesAsync(
					player.PlayerCode,
					command.NeedTurnover,
					BucketType.Promotion,
					distributeId.ToString(),
					null,
					command.BucketType == null ? Enumerable.Empty<string>() : new[] { command.BucketType },
					cancellationToken).ConfigureAwait(false))
				.ConfigureAwait(false);

			_ = await _cashServiceClient.PromotionAsync(
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

			await _promotionRepository.UpdatePromotionDistributeAsync(
				distributedInfo.Id,
				distributedInfo.SentOn.Value,
				cancellationToken).ConfigureAwait(false);

			await _promotionRepository.InsertPromotionDistributeLogAsync(
				distributeLog with
				{
					Id = await _serialNumberGenerator
						.GenerateAsync(cancellationToken)
						.ConfigureAwait(false),
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
