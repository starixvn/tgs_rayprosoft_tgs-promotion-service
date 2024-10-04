using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Promotion.PromotionDetailMessages;

public class PromotionDetailFactory(
	IDictionary<string, Type> checkerTypes,
	IServiceProvider serviceProvider) : IPromotionDetailFactory
{
	private static readonly string[] _AllowDispatchesListPromotionTypes = ["Milestone"];
	public IPromotionDetailMessage GetPromotionDetail(string promotionType)
		=> checkerTypes.TryGetValue(promotionType, out var checkerType)
			? (IPromotionDetailMessage)ActivatorUtilities.CreateInstance(serviceProvider, checkerType, promotionType)
			: new NullDetailMessageReply(promotionType);

	public IPromotionDetailMessage GetPromotionDetail(string promotionType, Func<bool> allowTypes)
		=> checkerTypes.TryGetValue(promotionType, out var checkerType) && allowTypes()
			? (IPromotionDetailMessage)ActivatorUtilities.CreateInstance(serviceProvider, checkerType,
				promotionType)
			: new NullDetailMessageReply(promotionType);

	public IPromotionDetailMessage GetDispatchesPromotionDetail(string promotionType)
	=> checkerTypes.TryGetValue(promotionType, out var checkerType) &&
	   _AllowDispatchesListPromotionTypes.Contains(promotionType)
		? (IPromotionDetailMessage)ActivatorUtilities.CreateInstance(serviceProvider, checkerType,
			promotionType)
		: new NullDetailMessageReply(promotionType);
}
