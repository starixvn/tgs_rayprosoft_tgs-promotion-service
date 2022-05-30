using Tgs.CashService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class DistributePromotionBonusCommandService : IGetService<DistributePromotionBonusCommand, Guid>
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly IPlayerBucketRepository _playerBucketRepository;
	private readonly IPlayerRepository _playerRepository;
	private readonly IPromotionRepository _promotionRepository;

	public DistributePromotionBonusCommandService(
		IPromotionRepository promotionRepository,
		ICashServiceClient cashServiceClient,
		IPlayerRepository playerRepository,
		IPlayerBucketRepository playerBucketRepository)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_cashServiceClient = cashServiceClient ?? throw new ArgumentNullException(nameof(cashServiceClient));
		_playerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
		_playerBucketRepository = playerBucketRepository ?? throw new ArgumentNullException(nameof(playerBucketRepository));
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

		await _playerBucketRepository.CreatePlayerBucketAsync(
			player.PlayerCode,
			BucketType.Promotion,
			command.NeedTurnover,
			cancellationToken).ConfigureAwait(false);

		await _cashServiceClient.PromotionAsync(
			player.DefaultWalletId,
			command.Amount,
				null,
			distributeId.ToString(),
				null,
			cancellationToken).ConfigureAwait(false);

		return distributeId;
	}
}
