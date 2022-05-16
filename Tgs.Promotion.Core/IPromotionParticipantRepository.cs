using System.Linq.Expressions;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Core;

public interface IPromotionParticipantRepository
{
	ValueTask CreatePromotionParticipantAsync(Guid promotionId, string playerCode);

	ValueTask<bool> ExistsAsync(Expression<Func<PromotionParticipant, bool>> predicate);

	ValueTask<PromotionParticipant?> GetPromotionParticipantAsync(
		Expression<Func<PromotionParticipant?, bool>> predicate,
		Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>? preQuery = null);

	IQueryable<PromotionParticipant> GetPromotionParticipantQueries(
		Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>? preQuery = null,
		bool tracking = false);
}
