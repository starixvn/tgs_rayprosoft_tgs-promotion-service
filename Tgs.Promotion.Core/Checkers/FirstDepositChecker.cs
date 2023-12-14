using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class FirstDepositChecker(
	string promotionType,
	IPlayerRepository playerService,
	IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public async ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion, JsonDocument? extend, CancellationToken cancellationToken = default)
	{
		var isFirstDepositing = await playerDataRepository
			.CheckPlayerFirstDepositingAsync(playerCode)
			.ConfigureAwait(false);

		if (isFirstDepositing)
			return false;

		var player = await playerService
			.GetPlayerByPlayerCodeAsync(playerCode)
			.ConfigureAwait(false);

		return player != null && player.FirstDepositOn == null;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> playerDataRepository
			.RemovePlayerFirstDepositFlagAsync(playerCode);

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> playerDataRepository
			.RemovePlayerFirstDepositFlagAsync(playerCode);

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> playerDataRepository.SetPlayerFirstDepositFlagAsync(playerCode);
}
