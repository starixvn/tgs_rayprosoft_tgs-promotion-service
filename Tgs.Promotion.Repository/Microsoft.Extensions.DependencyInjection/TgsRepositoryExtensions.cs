using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Repository;
using Tgs.Promotion.Repository.Repositories;

namespace Microsoft.Extensions.DependencyInjection;

public static class TgsRepositoryExtensions
{
	public static IServiceCollection AddTgsRepository(this IServiceCollection services, string connectionString)
	{
		_ = services.AddDbContext<TgsRepository>(options =>
		  {
			  _ = options.UseNpgsql(connectionString);
		  });

		_ = services.AddScoped<ITgsRepository>(c => c.GetRequiredService<TgsRepository>());
		_ = services.AddTransient(typeof(ITgsService<>), typeof(TgsService<>));

		return services;
	}

	public static IServiceCollection AddRedis(
		this IServiceCollection service,
		Action<RedisOptions, IServiceProvider> configure)
	{
		_ = service
			.AddOptions<RedisOptions>()
			.Configure(configure)
			.Services
			.AddSingleton<IRedisConnectionFactory, RedisConnectionFactory>();

		return service;
	}
}
