using Tgs.Promotion.ApplicationServices;
using Tgs.ServiceUtility;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionServiceCache(this IServiceCollection services)
		=> services
			.Decorate<IQueryService<ListPromotionsByBrandQuery, PromotionView>, ListCachedPromotionsByBrandQueryService>();
}
