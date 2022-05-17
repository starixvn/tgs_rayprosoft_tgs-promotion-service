namespace Tgs.Promotion.Core;

public class PromotionCheckerFactory : IPromotionCheckerFactory
{
	private readonly IDictionary<string, Type> _checkerTypes;
	private readonly IServiceProvider _serviceProvider;

	public PromotionCheckerFactory(IDictionary<string, Type> checkerTypes, IServiceProvider serviceProvider)
	{
		_checkerTypes = checkerTypes ?? throw new ArgumentNullException(nameof(checkerTypes));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public IPromotionChecker GetPromotionChecker(string promotionType)
	{
		return _checkerTypes.TryGetValue(promotionType, out var checkerType)
			? (IPromotionChecker)_serviceProvider.GetService(checkerType!)!
			: new AlwaysFalseChecker();
	}
}
