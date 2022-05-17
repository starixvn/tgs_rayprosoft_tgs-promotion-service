using StackExchange.Redis;
using Tgs.Promotion.Core;

namespace Tgs.Promotion.Repository;

internal class RedisPlayerDataRepository : IPlayerDataRepository
{
	private static readonly string _KeyPrefix = "TGS:Promotion";
	private static readonly TimeSpan _TimeZone = TimeSpan.FromHours(8);
	private readonly IRedisConnectionFactory _redisConnectionFactory;
	private readonly IDatabase _redisDatabase;

	public RedisPlayerDataRepository(IRedisConnectionFactory redisConnectionFactory)
	{
		_redisConnectionFactory =
			redisConnectionFactory ?? throw new ArgumentNullException(nameof(redisConnectionFactory));
		_redisDatabase = _redisConnectionFactory.GetDataBase();
	}

	public async ValueTask<bool> CheckPlayerFirstDepositingAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}";

		return await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> CheckPlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}:Join";

		return await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask<bool> CheckPlayerIsDailyFirstDepositAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{DateTimeOffset.Now.ToOffset(_TimeZone):yyyyMMdd}:{playerCode}";

		return !await _redisDatabase
			.KeyExistsAsync(dataKey)
			.ConfigureAwait(false);
	}

	public async ValueTask RemovePlayerDailyDepositFlagAsync(string playerCode)
	{
		var currentDate = DateTimeOffset.Now.ToOffset(_TimeZone);
		var dataKey = $"{_KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey, CommandFlags.FireAndForget)
				.ConfigureAwait(false);
		}
	}

	public async ValueTask RemovePlayerFirstDepositFlagAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey, CommandFlags.FireAndForget)
				.ConfigureAwait(false);
		}
	}

	public async ValueTask RemovePlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}:Join";

		var count = await _redisDatabase
			.StringDecrementAsync(dataKey)
			.ConfigureAwait(false);

		if (count <= 0)
		{
			_ = await _redisDatabase
				.KeyDeleteAsync(dataKey, CommandFlags.FireAndForget)
				.ConfigureAwait(false);
		}
	}

	public async ValueTask SetPlayerDailyDepositFlagAsync(string playerCode)
	{
		var currentDate = DateTimeOffset.Now.ToOffset(_TimeZone);
		var key = $"{_KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}";

		_ = await _redisDatabase
			.StringSetAsync(
				$"{_KeyPrefix}:{currentDate:yyyyMMdd}:{playerCode}",
				"0",
				TimeSpan.FromDays(1D),
				When.NotExists,
				CommandFlags.FireAndForget)
			.ConfigureAwait(false);

		_ = await _redisDatabase
			.StringIncrementAsync(key, flags: CommandFlags.FireAndForget)
			.ConfigureAwait(false);
	}

	public async ValueTask SetPlayerFirstDepositFlagAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}";

		_ = await _redisDatabase
			.StringIncrementAsync(dataKey, flags: CommandFlags.FireAndForget)
			.ConfigureAwait(false);
	}

	public async ValueTask SetPlayerFirstJoinPromotionAsync(string playerCode)
	{
		var dataKey = $"{_KeyPrefix}:{playerCode}:Join";

		_ = await _redisDatabase
			.StringIncrementAsync(dataKey, flags: CommandFlags.FireAndForget)
			.ConfigureAwait(false);
	}
}
