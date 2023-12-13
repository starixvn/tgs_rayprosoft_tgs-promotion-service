using System.Text.Json;

namespace Tgs.Promotion.Checkers;

internal class AlwaysFalseChecker : IPromotionChecker
{
	public ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
		=> ValueTask.FromResult(false);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
