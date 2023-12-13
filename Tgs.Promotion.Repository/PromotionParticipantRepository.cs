using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionParticipantRepository(
	ITgsService<PromotionParticipant> promotionParticipantService) : IPromotionParticipantRepository
{
	private readonly ITgsService<PromotionParticipant> _promotionParticipantService = promotionParticipantService ?? throw new ArgumentNullException(nameof(promotionParticipantService));

	public async ValueTask CreatePromotionParticipantAsync(Guid promotionId, string playerCode)
	{
		var promotionParticipant =
			new PromotionParticipant
			{
				PlayerCode = playerCode,
				PromotionId = promotionId,
				CreatedOn = DateTime.UtcNow
			};

		await _promotionParticipantService.CreateEntityAsync(promotionParticipant);
	}

	public async ValueTask<bool> ExistsAsync(
		Expression<Func<PromotionParticipant, bool>> predicate)
		=> await _promotionParticipantService.GetQueries().AnyAsync(predicate);

	public async ValueTask<PromotionParticipant?> GetPromotionParticipantAsync(
		Expression<Func<PromotionParticipant?, bool>> predicate,
		Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>? preQuery)
	{
		preQuery ??= c => c;

		return await preQuery(_promotionParticipantService.GetQueries()).FirstOrDefaultAsync(predicate);
	}

	public IQueryable<PromotionParticipant> GetPromotionParticipantQueries(
					Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>? preQuery,
		bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionParticipantService.GetQueries())
			: preQuery(_promotionParticipantService.GetQueries()).AsNoTracking();
	}
}
