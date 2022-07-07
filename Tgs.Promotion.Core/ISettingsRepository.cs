using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;

namespace Tgs.Promotion;

public interface ISettingsRepository
{
	ValueTask<Setting?> GetSettingAsync(
		Expression<Func<Setting, bool>> predicate,
		Func<IQueryable<Setting>, IQueryable<Setting>>? preQuery = null);

	IAsyncEnumerable<PromotionTypeGroup> GetPromotionTypeGroupsAsync(CancellationToken cancellationToken = default);
}
