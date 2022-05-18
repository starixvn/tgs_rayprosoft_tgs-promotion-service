using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion;

namespace Tgs.Promotion.Repository;

internal class PlayerRepository : IPlayerRepository
{
	private readonly TgsContext _tgsContext;

	public PlayerRepository(TgsContext tgsContext)
	{
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
	}

	public async ValueTask<bool> ExistsAsync(Expression<Func<Player?, bool>> predicate) =>
		await _tgsContext.Players.AnyAsync(predicate).ConfigureAwait(false);

	public async ValueTask<Player?> GetPlayerAsync(
		Expression<Func<Player, bool>> predicate,
		Func<IQueryable<Player>, IQueryable<Player>>? preQuery = null)
	{
		preQuery ??= c => c;

		return await preQuery(_tgsContext.Players).FirstOrDefaultAsync(predicate);
	}

	public async ValueTask<Player?> GetPlayerByPlayerCodeAsync(string playerCode)
		=> await _tgsContext.Players
			.Where(player => player.PlayerCode == playerCode)
			.FirstOrDefaultAsync();

	public IQueryable<Player> GetPlayerQueries(
		Func<IQueryable<Player>, IQueryable<Player>>? preQuery,
		bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_tgsContext.Players)
			: preQuery(_tgsContext.Players).AsNoTracking();
	}

	public async ValueTask SetFirstDepositTimeAsync(string playerCode, DateTimeOffset time)
	{
		var player = await _tgsContext.Players
			.FirstOrDefaultAsync(x => x.PlayerCode == playerCode)
			.ConfigureAwait(false);

		if (player!.FirstDepositOn is null)
		{
			player.FirstDepositOn = time.UtcDateTime;

			_ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);
		}
	}

	public async ValueTask SetFirstPromotionTimeAsync(string playerCode, DateTimeOffset time)
	{
		var player = await _tgsContext.Players
			.FirstOrDefaultAsync(x => x.PlayerCode == playerCode)
			.ConfigureAwait(false);

		if (player!.FirstPromotionOn is null)
		{
			player.FirstPromotionOn = time.UtcDateTime;

			_ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);
		}
	}
}
