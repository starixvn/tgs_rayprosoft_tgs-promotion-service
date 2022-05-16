using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository
{
	public class TgsService<T> : ITgsService<T> where T : class
	{
		private TgsContext Repository { get; }

		public TgsService(TgsContext dbContext)
		{
			Repository = dbContext;
		}

		public async ValueTask CreateEntityAsync(T entity)
		{
			await Repository.Set<T>().AddAsync(entity);
			await Repository.SaveChangesAsync();
		}

		public async ValueTask CreateEntitiesAsync(T[] entities)
		{
			await Repository.Set<T>().AddRangeAsync(entities);
			await Repository.SaveChangesAsync();
		}

		public IQueryable<T> GetQueries(Func<IQueryable<T>, IQueryable<T>>? preQuery, bool tracking)
		{
			preQuery ??= c => c;
			return tracking ? preQuery(Repository.Set<T>()) : preQuery(Repository.Set<T>()).AsNoTracking();
		}

		public async ValueTask UpdateEntitiesAsync(T[] entities)
		{
			Repository.Set<T>().UpdateRange(entities);
			await Repository.SaveChangesAsync();
		}

		public async ValueTask DeleteEntitiesAsync(IEnumerable<T> entities)
		{
			Repository.Set<T>().RemoveRange(entities);
			await Repository.SaveChangesAsync();
		}

		public Task<int> SaveChangesAsync() => Repository.SaveChangesAsync();

		public async ValueTask UpdateEntityAsync(T entity)
		{
			Repository.Set<T>().Update(entity);
			await Repository.SaveChangesAsync();
		}
	}
}
