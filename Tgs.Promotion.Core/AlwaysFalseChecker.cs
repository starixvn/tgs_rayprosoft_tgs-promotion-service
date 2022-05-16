using Tgs.Promotion.Domain;

namespace Tgs.Promotion.Core
{
	internal class AlwaysFalseChecker : IPromotionChecker
	{
		public ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
			=> ValueTask.FromResult(false);

		public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

		public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

		public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
	}
}
