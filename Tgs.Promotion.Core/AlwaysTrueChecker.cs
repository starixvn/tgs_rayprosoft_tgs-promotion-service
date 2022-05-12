using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Core
{
    internal class AlwaysTrueChecker : IPromotionChecker
    {
        public ValueTask<bool> CheckAsync(string playerCode, Guid? promotionId)
            => ValueTask.FromResult(true);

        public ValueTask CommitTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

        public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;

        public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId) => ValueTask.CompletedTask;
    }
}
