namespace Tgs.Promotion;

internal class AlwaysTrueChecker : IPromotionChecker
{
	public ValueTask<bool> CheckAsync(string playerCode, PromotionMainInfo promotion)
		=> ValueTask.FromResult(true);

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
}
