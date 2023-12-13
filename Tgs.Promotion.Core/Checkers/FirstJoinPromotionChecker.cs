using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class FirstJoinPromotionChecker(IPlayerRepository playerService, IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	private readonly IPlayerDataRepository _playerDataRepository = playerDataRepository ?? throw new ArgumentNullException(nameof(playerDataRepository));
	private readonly IPlayerRepository _playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion, JsonDocument? extend, CancellationToken cancellationToken = default)
	{
		var isFirstDepositing = await _playerDataRepository
			.CheckPlayerFirstJoinPromotionAsync(playerCode)
			.ConfigureAwait(false);

		if (isFirstDepositing)
			return false;

		var player = await _playerService
			.GetPlayerByPlayerCodeAsync(playerCode)
			.ConfigureAwait(false);

		return player is not null && player.FirstPromotionOn == null;
	}

	public async ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		if (promotionId is null)
			return;

		await _playerService
			.SetFirstPromotionTimeAsync(playerCode, DateTimeOffset.UtcNow)
			.ConfigureAwait(false);

		await _playerDataRepository
			.RemovePlayerFirstJoinPromotionAsync(playerCode)
			.ConfigureAwait(false);
	}

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		return promotionId is null
			? ValueTask.CompletedTask
			: _playerDataRepository
			.RemovePlayerFirstJoinPromotionAsync(playerCode);
	}

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		return promotionId == null
			? ValueTask.CompletedTask
			: _playerDataRepository
			.SetPlayerFirstJoinPromotionAsync(playerCode);
	}
}
