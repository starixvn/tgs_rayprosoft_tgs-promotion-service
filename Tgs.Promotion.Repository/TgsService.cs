using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

internal class TgsService<T> : ITgsService<T> where T : class
{
	private TgsContext Repository { get; }

	public TgsService(TgsContext dbContext)
	{
		Repository = dbContext;
	}

	public async ValueTask CreateEntitiesAsync(T[] entities)
	{
		await Repository.Set<T>().AddRangeAsync(entities);
		_ = await Repository.SaveChangesAsync();
	}

	public async ValueTask CreateEntityAsync(T entity)
	{
		_ = await Repository.Set<T>().AddAsync(entity);
		_ = await Repository.SaveChangesAsync();
	}

	public async ValueTask DeleteEntitiesAsync(IEnumerable<T> entities)
	{
		Repository.Set<T>().RemoveRange(entities);
		_ = await Repository.SaveChangesAsync();
	}

	public IQueryable<T> GetQueries(Func<IQueryable<T>, IQueryable<T>>? preQuery, bool tracking)
	{
		preQuery ??= c => c;
		return tracking ? preQuery(Repository.Set<T>()) : preQuery(Repository.Set<T>()).AsNoTracking();
	}

	public Task<int> SaveChangesAsync() => Repository.SaveChangesAsync();

	public async ValueTask UpdateEntitiesAsync(T[] entities)
	{
		Repository.Set<T>().UpdateRange(entities);
		_ = await Repository.SaveChangesAsync();
	}

	public async ValueTask UpdateEntityAsync(T entity)
	{
		_ = Repository.Set<T>().Update(entity);
		_ = await Repository.SaveChangesAsync();
	}
}
