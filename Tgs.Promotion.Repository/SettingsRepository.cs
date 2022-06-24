using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;

namespace Tgs.Promotion.Repository;

internal class SettingsRepository : ISettingsRepository
{
	private readonly TgsContext _tgsContext;

	public SettingsRepository(TgsContext tgsContext)
	{
		_tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));
	}

	public async IAsyncEnumerable<PromotionTypeGroup> GetPromotionTypeGroupsAsync(CancellationToken cancellationToken = default)
	{
		var promotionSetting = await _tgsContext.Settings
			.FindAsync(new object?[] { "PromotionTypes" }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var promotionTypeGroups = JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionSetting!.Value);

		foreach (var group in promotionTypeGroups!)
			yield return group;
	}

	public async ValueTask<Setting?> GetSettingAsync(
			Expression<Func<Setting, bool>> predicate,
		Func<IQueryable<Setting>, IQueryable<Setting>>? preQuery)
	{
		preQuery ??= c => c;

		return await preQuery(_tgsContext.Settings).FirstOrDefaultAsync(predicate);
	}
}
