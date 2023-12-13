using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Cache;

internal class CachedPromotionTypeGroupsQueryService(
	IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> sourceService,
	IMemoryCache memoryCache) : IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup>
{
	private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
	private readonly IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));

	public async IAsyncEnumerable<PromotionTypeGroup> QueryAsync(
		PromotionTypeGroupsQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var key = $"PromotionTypeGroup:Settings";

		var items = await _memoryCache.GetOrCreateAsync(key, async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1D);

			return await _sourceService
				.QueryAsync(query, cancellationToken)
				.ToArrayAsync()
				.ConfigureAwait(false);
		}).ConfigureAwait(false);

		foreach (var item in items!)
			yield return item;
	}
}
