using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Models.PromotionBucket;

namespace Tgs.Promotion.Domain.Interfaces
{
    public interface ISettingsRepository
    {
        ValueTask<Setting?> GetSettingAsync(
            Expression<Func<Setting, bool>> predicate,
            Func<IQueryable<Setting>, IQueryable<Setting>>? preQuery = null);
    }
}