using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Repository;

namespace Tgs.Promotion.Core;

public class PlayerRepository : IPlayerRepository
{
    private readonly ITgsService<Player> _playerService;

    public PlayerRepository(
        ITgsService<Player> playerService)
    {
        _playerService = playerService;
    }

    public async ValueTask<bool> ExistsAsync(Expression<Func<Player?, bool>> predicate) =>
        await _playerService.GetQueries().AnyAsync(predicate).ConfigureAwait(false);

    public async ValueTask<Player?> GetPlayerAsync(
        Expression<Func<Player, bool>> predicate,
        Func<IQueryable<Player>, IQueryable<Player>>? preQuery = null)
    {
        preQuery ??= c => c;

        return await preQuery(_playerService.GetQueries()).FirstOrDefaultAsync(predicate);
    }

    public async ValueTask<Player?> GetPlayerByPlayerCodeAsync(string playerCode)
        => await _playerService.GetQueries()
            .Where(player => player.PlayerCode == playerCode)
            .FirstOrDefaultAsync();

    public IQueryable<Player> GetPlayerQueries(
        Func<IQueryable<Player>, IQueryable<Player>>? preQuery,
        bool tracking)
    {
        preQuery ??= c => c;
        return tracking
            ? preQuery(_playerService.GetQueries())
            : preQuery(_playerService.GetQueries()).AsNoTracking();
    }

    public async ValueTask SetFirstDepositTimeAsync(string playerCode, DateTimeOffset time)
    {
        var player = await _playerService.GetQueries()
            .FirstOrDefaultAsync(x => x.PlayerCode == playerCode)
            .ConfigureAwait(false);

        if (player!.FirstDepositOn is null)
        {
            player.FirstDepositOn = time.UtcDateTime;

            await _playerService.UpdateEntityAsync(player).ConfigureAwait(false);
        }
    }

    public async ValueTask SetFirstPromotionTimeAsync(string playerCode, DateTimeOffset time)
    {
        var player = await _playerService.GetQueries()
            .FirstOrDefaultAsync(x => x.PlayerCode == playerCode)
            .ConfigureAwait(false);

        if (player!.FirstPromotionOn is null)
        {
            player.FirstPromotionOn = time.UtcDateTime;

            await _playerService.UpdateEntityAsync(player).ConfigureAwait(false);
        }
    }
}