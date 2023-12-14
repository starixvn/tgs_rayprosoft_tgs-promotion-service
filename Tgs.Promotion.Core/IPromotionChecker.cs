using System.Text.Json;

namespace Tgs.Promotion;

public interface IPromotionChecker
{
	ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion, JsonDocument? extend, CancellationToken cancellationToken = default);

	ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default);

	ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default);

	ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default);
}
