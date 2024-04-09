using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Tgs.Promotion.Repository;

internal class RedisPlayerDataRepository : IPlayerDataRepository
{
	private const string KeyPrefix = "TGS:Promotion";
	private static readonly TimeSpan _TimeZone = TimeSpan.FromHours(8);
	private readonly IDatabase _redisDatabase;
	private readonly ILogger<RedisPlayerDataRepository> _logger;

	public RedisPlayerDataRepository(IRedisConnectionFactory redisConnectionFactory, ILogger<RedisPlayerDataRepository> logger)
	{
		_logger = logger;
		var connection = redisConnectionFactory ?? throw new ArgumentNullException(nameof(redisConnectionFactory));
		_redisDatabase = connection.GetDataBase();
	}

	public async ValueTask<bool> CheckPlayerFirstDepositingAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}";

		return await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> CheckPlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}:Join";

		return await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> CheckPlayerIsDailyFirstDepositAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{DateTimeOffset.Now.ToOffset(_TimeZone):yyyyMMdd}:{playerCode}";

		return !await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask RemovePlayerDailyDepositFlagAsync(string playerCode)
	{
		var currentDate = DateTimeOffset.Now.ToOffset(_TimeZone);
		var dataKey = $"{KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		_logger.LogInformation("Remove player daily deposit flag: {playerCode} {count}", playerCode, count);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey)
				.ConfigureAwait(false);

			_logger.LogInformation("Already removed player daily deposit flag: {playerCode}", playerCode);
		}
	}

	public async ValueTask RemovePlayerFirstDepositFlagAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		_logger.LogInformation("Remove player first deposit flag: {playerCode} {count}", playerCode, count);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey)
				.ConfigureAwait(false);

			_logger.LogInformation("Already removed player daily deposit flag: {playerCode}", playerCode);
		}
	}

	public async ValueTask RemovePlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}:Join";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		_logger.LogInformation("Remove player first join deposit flag: {playerCode} {count}", playerCode, count);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey)
				.ConfigureAwait(false);

			_logger.LogInformation("Already removed player join deposit flag: {playerCode}", playerCode);
		}
	}

	public async ValueTask SetPlayerDailyDepositFlagAsync(string playerCode)
	{
		var currentDate = DateTimeOffset.Now.ToOffset(_TimeZone);
		var key = $"{KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}";

		_ = await _redisDatabase
			.StringSetAsync(
				$"{KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}",
				"0",
				TimeSpan.FromDays(1D),
				When.NotExists)
			.ConfigureAwait(false);

		_ = await _redisDatabase
			.StringIncrementAsync(key)
			.ConfigureAwait(false);
	}

	public async ValueTask SetPlayerFirstDepositFlagAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}";

		_ = await _redisDatabase
			.StringIncrementAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask SetPlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{KeyPrefix}:{playerCode}:Join";

		_ = await _redisDatabase
			.StringIncrementAsync(dataKey)
			.ConfigureAwait(false);
	}
}
