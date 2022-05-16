using StackExchange.Redis;

namespace Tgs.Promotion.Domain;

public interface IRedisConnectionFactory
{
	public IDatabase GetDataBase();
}
