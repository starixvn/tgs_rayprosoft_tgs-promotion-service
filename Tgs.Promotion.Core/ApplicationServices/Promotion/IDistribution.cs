using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public interface IDistribution
{
	IDistribution SetPromotion(PromotionRow promotion,DateTimeOffset? from,DateTimeOffset? to);
	ValueTask<IDistribution> SetConditionDataAsync(CancellationToken cancellationToken);
	ValueTask<IDistribution> CalculateBonusAmountAsync(CancellationToken cancellationToken);
	ValueTask<IDistribution> DistributeBonusAsync(CancellationToken cancellationToken);
}
