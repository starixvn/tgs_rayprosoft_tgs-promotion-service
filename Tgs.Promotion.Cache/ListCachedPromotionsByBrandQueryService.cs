using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Cache;

internal class ListCachedPromotionsByBrandQueryService : IQueryService<ListPromotionsByBrandQuery, PromotionView>
{
	private readonly IMemoryCache _memoryCache;
	private readonly IQueryService<ListPromotionsByBrandQuery, PromotionView> _sourceService;

	public ListCachedPromotionsByBrandQueryService(
		IQueryService<ListPromotionsByBrandQuery, PromotionView> sourceService,
		IMemoryCache memoryCache)
	{
		_sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
		_memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	}

	public async IAsyncEnumerable<PromotionView> QueryAsync(
		ListPromotionsByBrandQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var key = $"Brand:Promotions:{query.BrandId}";

		var items = await _memoryCache.GetOrCreateAsync(key, async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(10D);

			return await _sourceService
				.QueryAsync(query, cancellationToken)
				.ToArrayAsync()
				.ConfigureAwait(false);
		}).ConfigureAwait(false);

		foreach (var item in items!)
			yield return item;
	}
}
