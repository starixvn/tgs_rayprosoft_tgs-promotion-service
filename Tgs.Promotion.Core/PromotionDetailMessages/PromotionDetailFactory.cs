using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Promotion.PromotionDetailMessages;

public class PromotionDetailFactory(
	IDictionary<string, Type> checkerTypes,
	IServiceProvider serviceProvider) : IPromotionDetailFactory
{
	public IPromotionDetailMessage GetPromotionDetail(string promotionType)
		=> checkerTypes.TryGetValue(promotionType, out var checkerType)
			? (IPromotionDetailMessage)ActivatorUtilities.CreateInstance(serviceProvider, checkerType, promotionType)
			: new NullDetailMessageReply(promotionType);
}
