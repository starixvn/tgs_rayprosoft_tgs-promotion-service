using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Core
{
	public class SettingsRepository : ISettingsRepository
	{
		private readonly ITgsService<Setting> _settingService;

		public SettingsRepository(ITgsService<Setting> settingService)
		{
			_settingService = settingService;
		}

		public async ValueTask<Setting?> GetSettingAsync(
			Expression<Func<Setting, bool>> predicate,
			Func<IQueryable<Setting>, IQueryable<Setting>>? preQuery)
		{
			preQuery ??= c => c;

			return await preQuery(_settingService.GetQueries()).FirstOrDefaultAsync(predicate);
		}
	}
}
