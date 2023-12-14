using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class DailySecondDepositChecker(string promotionType, IPlayerDataRepository playerDataRepository) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public async ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
		=> !await playerDataRepository
			.CheckPlayerIsDailyFirstDepositAsync(playerCode)
			.ConfigureAwait(false);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
