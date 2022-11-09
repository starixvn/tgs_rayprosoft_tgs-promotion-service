using System.Collections.Immutable;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusCommandService : IGetService<DistributePromotionBonusCommand, Guid>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionRepository _promotionRepository;

	public DistributePromotionBonusCommandService(
		IPromotionRepository promotionRepository,
		ICashServiceClient cashServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPlayerBucketClient playerBucketClient)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask<Guid> GetAsync(DistributePromotionBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient.GetPlayerInfoAsync(
			command.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var playerProperties = await _playerServiceClient.GetPlayerPropertiesAsync(
			player.PlayerCode,
			cancellationToken).ConfigureAwait(false);

		if (playerProperties is null)
			throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

		var walletId = Guid.Parse(playerProperties.GetValueOrDefault("DefaultWalletId")!);

		var distributeId = await _promotionRepository
			.CreatePromotionDistributeAsync(
				command.PlanId,
				command.PlayerCode,
				command.Amount,
				command.NeedTurnover,
				command.Distributor,
				cancellationToken)
			.ConfigureAwait(false);

		_ = await _playerBucketClient.CreateBucketAsync(
			player.PlayerCode,
			command.NeedTurnover,
			BucketType.Promotion,
			distributeId.ToString(),
			cancellationToken).ConfigureAwait(false);

		_ = await _cashServiceClient.PromotionAsync(
			walletId,
			command.Amount,
			null,
			distributeId.ToString(),
			null,
			cancellationToken).ConfigureAwait(false);

		return distributeId;
	}
}
