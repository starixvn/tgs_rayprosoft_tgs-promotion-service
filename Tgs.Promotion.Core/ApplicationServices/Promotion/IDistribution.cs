using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public interface IDistribution
{
	ValueTask<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken);

	ValueTask<IDistribution> DistributeBonusAsync(CancellationToken cancellationToken);

	ValueTask<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken);

	IDistribution SetPromotion(PromotionRow promotion, DateTimeOffset from, DateTimeOffset to);
}
