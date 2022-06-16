using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Cache.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionServiceCache(this IServiceCollection services)
		=> services
			.Decorate<IQueryService<ListPromotionsByBrandQuery, PromotionView>, ListCachedPromotionsByBrandQueryService>();
}
