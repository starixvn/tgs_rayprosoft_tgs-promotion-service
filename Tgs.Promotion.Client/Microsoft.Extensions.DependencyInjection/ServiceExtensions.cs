using Microsoft.Extensions.Options;
using Tgs.Promotion.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceExtensions
{
	/// <summary>
	///     Dependency Injection of Operations, need to add the following dependencies beforehand:
	///     1. AddOperations() in Infrastructures.Operations
	///     2. AddApiClient() in Infrastructure.ApiClient
	/// </summary>
	public static IServiceCollection AddPromotionClientServices(
		this IServiceCollection services,
		Action<PromotionOptions, IServiceProvider> configure)
	{
		services
			.AddOptions<PromotionOptions>()
			.Configure(configure);
		services
			.AddHttpClient<PromotionClient>((sp, c) =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionOptions>>().Value;

				c.BaseAddress = options.Url;
			})
			.Services
			.AddHttpClient<PromotionBonusClient>((sp, c) =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionOptions>>().Value;

				c.BaseAddress = options.Url;
			})
			.Services
			.AddHttpClient<PromotionDispatchClient>((sp, c) =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionOptions>>().Value;

				c.BaseAddress = options.Url;
			})
			.Services
			.AddTransient<IPromotionClient>(sp => sp.GetRequiredService<PromotionClient>())
			.AddTransient<IPromotionBonusClient>(sp => sp.GetRequiredService<PromotionBonusClient>())
			.AddTransient<IPromotionDispatchClient>(sp => sp.GetRequiredService<PromotionDispatchClient>());

		return services;
	}
}
