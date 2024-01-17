namespace Tgs.Promotion;

public interface ITgsService<T>
{
	ValueTask CreateEntitiesAsync(T[] entities);

	ValueTask CreateEntityAsync(T entity);

	ValueTask DeleteEntitiesAsync(IEnumerable<T> entities);

	IQueryable<T> GetQueries(
					Func<IQueryable<T>, IQueryable<T>>? preQuery = null,
		bool tracking = false);

	Task<int> SaveChangesAsync();

	ValueTask UpdateEntitiesAsync(T[] entities);

	ValueTask UpdateEntityAsync(T entity);
}
