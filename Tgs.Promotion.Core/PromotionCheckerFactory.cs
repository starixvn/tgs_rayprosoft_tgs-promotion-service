namespace Tgs.Promotion;

public class PromotionCheckerFactory(IDictionary<string, Type> checkerTypes, IServiceProvider serviceProvider) : IPromotionCheckerFactory
{
	private readonly IDictionary<string, Type> _checkerTypes = checkerTypes ?? throw new ArgumentNullException(nameof(checkerTypes));
	private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

	public IPromotionChecker GetPromotionChecker(string promotionType)
	{
		return _checkerTypes.TryGetValue(promotionType, out var checkerType)
			? (IPromotionChecker)_serviceProvider.GetService(checkerType!)!
			: new AlwaysFalseChecker();
	}
}
