using StackExchange.Redis;

namespace Tgs.Promotion.Repository;

public interface IRedisConnectionFactory
{
	public IDatabase GetDataBase();
}
