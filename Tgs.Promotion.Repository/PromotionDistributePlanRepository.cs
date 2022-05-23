using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionDistributePlanRepository : IPromotionDistributePlanRepository
{
	private readonly ITgsService<PromotionDistributePlan> _tgsService;

	public PromotionDistributePlanRepository(ITgsService<PromotionDistributePlan> tgsService)
		=> _tgsService = tgsService ?? throw new ArgumentNullException(nameof(tgsService));

	public async ValueTask<Guid> CreatePromotionDistributePlanAsync(Guid brandId, Guid promotionId, int conditionHash,
		string detail)
	{
		var id = Guid.NewGuid();

		var plan = new PromotionDistributePlan
		{
			Id = id,
			BrandId = brandId,
			PromotionId = promotionId,
			Detail = detail,
			ConditionHash = conditionHash
		};

		await _tgsService.CreateEntityAsync(plan).ConfigureAwait(false);

		return id;
	}

	public async ValueTask<bool> ExistsAsync(Guid brandId, Guid promotionId, int conditionHash) => await _tgsService
		.GetQueries().AnyAsync(x
			=> x.BrandId == brandId && x.PromotionId == promotionId && x.ConditionHash == conditionHash)
		.ConfigureAwait(false);

	public async ValueTask<Guid?> GetPromotionDistributePlanIdAsync(Guid brandId, Guid promotionId, int conditionHash)
		=> await _tgsService.GetQueries().Where(x
				=> x.BrandId == brandId && x.PromotionId == promotionId && x.ConditionHash == conditionHash)
			.Select(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);
}