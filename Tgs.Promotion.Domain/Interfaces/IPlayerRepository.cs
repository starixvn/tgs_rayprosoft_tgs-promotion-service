using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Domain.Interfaces;

public interface IPlayerRepository
{
	ValueTask<bool> ExistsAsync(Expression<Func<Player?, bool>> predicate);

	ValueTask<Player?> GetPlayerAsync(
		Expression<Func<Player, bool>> predicate,
		Func<IQueryable<Player>, IQueryable<Player>>? preQuery = null);

	ValueTask<Player?> GetPlayerByPlayerCodeAsync(string playerCode);

	IQueryable<Player> GetPlayerQueries(
		Func<IQueryable<Player>, IQueryable<Player>>? preQuery = null,
		bool tracking = false);

	ValueTask SetFirstDepositTimeAsync(string playerCode, DateTimeOffset time);

	ValueTask SetFirstPromotionTimeAsync(string playerCode, DateTimeOffset time);
}
