using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class DailyFirstDepositChecker(string promotionType, IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
		=> playerDataRepository.CheckPlayerIsDailyFirstDepositAsync(playerCode);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
			=> ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> promotionId is null
			? ValueTask.CompletedTask
			: playerDataRepository.RemovePlayerDailyDepositFlagAsync(playerCode);

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
		=> promotionId is null
			? ValueTask.CompletedTask
			: playerDataRepository.SetPlayerDailyDepositFlagAsync(playerCode);
}
