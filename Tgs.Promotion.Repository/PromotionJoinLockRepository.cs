using System.Text.Json;
using StackExchange.Redis;

namespace Tgs.Promotion.Repository;

internal class PromotionJoinLockRepository : IPromotionJoinLockRepository
{
	private const string RedisKey = "promotion:join:{0}:{1}";
	private readonly IRedisConnectionFactory _redisConnectionFactory;

	public PromotionJoinLockRepository(IRedisConnectionFactory redisConnectionFactory)
		=> _redisConnectionFactory = redisConnectionFactory ?? throw new ArgumentNullException(nameof(redisConnectionFactory));

	public async ValueTask<bool> SetWhenNotExistsAsync(string playerCode, Guid promotionId)
	{
		var key = string.Format(RedisKey, promotionId, playerCode);

		return await _redisConnectionFactory.GetDataBase().StringSetAsync(key, JsonSerializer.Serialize(new
		{
			promotionId,
			playerCode
		}), TimeSpan.FromSeconds(10), when: When.NotExists)
			.ConfigureAwait(false);
	}
}
