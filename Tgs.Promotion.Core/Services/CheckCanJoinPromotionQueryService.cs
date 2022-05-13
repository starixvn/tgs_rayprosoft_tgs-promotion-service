using Tgs.Promotion.Domain.Interfaces;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Services
{
    internal class CheckCanJoinPromotionQueryService : IGetService<CheckCanJoinPromotionQuery, bool>
    {
        private readonly IPromotionCheckerFactory _promotionCheckerFactory;
        private readonly IPromotionRepository _promotionRepository;

        public CheckCanJoinPromotionQueryService(
            IPromotionRepository promotionRepository,
            IPromotionCheckerFactory promotionCheckerFactory)
        {
            _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
            _promotionCheckerFactory = promotionCheckerFactory ??
                throw new ArgumentNullException(nameof(promotionCheckerFactory));
        }

        public async ValueTask<bool> GetAsync(
            CheckCanJoinPromotionQuery query,
            CancellationToken cancellationToken = default)
        {
            var promotionData = await _promotionRepository.GetPromotionByIdAsync(query.PromotionId);

            if (promotionData?.BrandId != query.BrandId)
            {
                return false;
            }

            var checker = _promotionCheckerFactory.GetPromotionChecker(promotionData.PromotionType);

            var isAllowed = await checker
                .CheckAsync(query.PlayerCode)
                .ConfigureAwait(false);
            if (!isAllowed)
            {
                return false;
            }

            var amount = promotionData.Percentage * query.Amount / 100M;
            amount = amount > promotionData.BonusAmountLimit ? promotionData.BonusAmountLimit : amount;

            return await _promotionRepository.UpdatePromotionAmountAsync(query.PromotionId, amount)
                .ConfigureAwait(false);
        }
    }
}
