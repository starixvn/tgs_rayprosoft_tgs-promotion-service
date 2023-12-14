using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class FirstJoinPromotionChecker(
	string promotionType,
	IPlayerRepository playerService,
	IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion, JsonDocument? extend, CancellationToken cancellationToken = default)
	{
		var isFirstDepositing = await playerDataRepository
			.CheckPlayerFirstJoinPromotionAsync(playerCode)
			.ConfigureAwait(false);

		if (isFirstDepositing)
			return false;

		var player = await playerService
			.GetPlayerByPlayerCodeAsync(playerCode)
			.ConfigureAwait(false);

		return player is not null && player.FirstPromotionOn == null;
	}

	public async ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		if (promotionId is null)
			return;

		await playerService
			.SetFirstPromotionTimeAsync(playerCode, DateTimeOffset.UtcNow)
			.ConfigureAwait(false);

		await playerDataRepository.RemovePlayerFirstJoinPromotionAsync(playerCode)
			.ConfigureAwait(false);
	}

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		return promotionId is null
			? ValueTask.CompletedTask
			: playerDataRepository.RemovePlayerFirstJoinPromotionAsync(playerCode);
	}

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		return promotionId == null
			? ValueTask.CompletedTask
			: playerDataRepository.SetPlayerFirstJoinPromotionAsync(playerCode);
	}
}
