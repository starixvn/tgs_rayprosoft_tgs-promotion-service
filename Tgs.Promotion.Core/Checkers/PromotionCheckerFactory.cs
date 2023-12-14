using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Promotion.Checkers;

public class PromotionCheckerFactory(
	IDictionary<string, Type> checkerTypes,
	IServiceProvider serviceProvider) : IPromotionCheckerFactory
{
	public IPromotionChecker GetPromotionChecker(string promotionType)
		=> checkerTypes.TryGetValue(promotionType, out var checkerType)
			? (IPromotionChecker)ActivatorUtilities.CreateInstance(serviceProvider, checkerType, promotionType)
			: new AlwaysFalseChecker(promotionType);
}
