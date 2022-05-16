namespace Tgs.Promotion.Domain.Interfaces
{
	public interface ITgsService<T>
	{
		IQueryable<T> GetQueries(
			Func<IQueryable<T>, IQueryable<T>>? preQuery = null,
			bool tracking = false);

		ValueTask CreateEntityAsync(T entity);

		ValueTask UpdateEntityAsync(T entity);

		ValueTask CreateEntitiesAsync(T[] entities);

		ValueTask UpdateEntitiesAsync(T[] entities);

		ValueTask DeleteEntitiesAsync(IEnumerable<T> entities);

		Task<int> SaveChangesAsync();
	}
}
