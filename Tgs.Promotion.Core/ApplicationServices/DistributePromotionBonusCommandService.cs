using Tgs.CashService.Client;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusCommandService : IGetService<DistributePromotionBonusCommand, Guid>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerRepository _playerRepository;
	private readonly IPromotionRepository _promotionRepository;

	public DistributePromotionBonusCommandService(
		IPromotionRepository promotionRepository,
		ICashServiceClient cashServiceClient,
		IPlayerRepository playerRepository,
		IPlayerBucketClient playerBucketClient)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
		_playerBucketClient = playerBucketClient ?? throw new ArgumentNullException(nameof(playerBucketClient));
	}

	public async ValueTask<Guid> GetAsync(DistributePromotionBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var player = await _playerRepository
			.GetPlayerByPlayerCodeAsync(command.PlayerCode)
			.ConfigureAwait(false);

		if (player is null)
			throw new InvalidOperationException($"Player with player code {command.PlayerCode} does not exist");

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
			player.DefaultWalletId,
			command.Amount,
			null,
			distributeId.ToString(),
			null,
			cancellationToken).ConfigureAwait(false);

		return distributeId;
	}
}
