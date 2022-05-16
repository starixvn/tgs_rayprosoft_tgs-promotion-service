using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Domain
{
	public interface ISettingsRepository
	{
		ValueTask<Setting?> GetSettingAsync(
			Expression<Func<Setting, bool>> predicate,
			Func<IQueryable<Setting>, IQueryable<Setting>>? preQuery = null);
	}
}
