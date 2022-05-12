using StackExchange.Redis;

namespace Tgs.Promotion.Domain.Interfaces;

public interface IRedisConnectionFactory
{
	public IDatabase GetDataBase();
}
