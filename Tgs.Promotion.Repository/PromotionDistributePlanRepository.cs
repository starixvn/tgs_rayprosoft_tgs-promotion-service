using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionDistributePlanRepository : IPromotionDistributePlanRepository
{
	private readonly TgsContext _tgsContext;

	public PromotionDistributePlanRepository(TgsContext tgsContext)
		=> _tgsContext = tgsContext ?? throw new ArgumentNullException(nameof(tgsContext));

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

		_ = await _tgsContext.AddAsync(plan).ConfigureAwait(false);
		_ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);
		return id;
	}

	public async ValueTask<bool> ExistsAsync(Guid brandId, Guid promotionId, int conditionHash) => await _tgsContext
		.PromotionDistributePlans.AnyAsync(x
			=> x.BrandId == brandId && x.PromotionId == promotionId && x.ConditionHash == conditionHash)
		.ConfigureAwait(false);

	public async ValueTask<Guid?> GetPromotionDistributePlanIdAsync(Guid brandId, Guid promotionId, int conditionHash)
		=> await _tgsContext.PromotionDistributePlans.Where(x
				=> x.BrandId == brandId && x.PromotionId == promotionId && x.ConditionHash == conditionHash)
			.Select(x => x.Id).FirstOrDefaultAsync().ConfigureAwait(false);

	public async ValueTask<PromotionDistributePlanInfo?> GetPromotionDistributePlanInfoAsync(Guid brandId, Guid promotionId,
		long conditionHash)
	{
		var plan = await _tgsContext.PromotionDistributePlans.FirstOrDefaultAsync(x
				=> x.BrandId == brandId && x.PromotionId == promotionId && x.ConditionHash == conditionHash)
			.ConfigureAwait(false);

		if (plan is not null)
		{
			return new PromotionDistributePlanInfo(plan.Id, plan.BrandId, plan.PromotionId, plan.ConditionHash);
		}

		var newPlan = new PromotionDistributePlan
		{
			BrandId = brandId,
			PromotionId = promotionId,
			ConditionHash = conditionHash,
		};

		_ = await _tgsContext.PromotionDistributePlans.AddAsync(newPlan).ConfigureAwait(false);
		_ = await _tgsContext.SaveChangesAsync().ConfigureAwait(false);

		return new PromotionDistributePlanInfo(newPlan.Id, newPlan.BrandId, newPlan.PromotionId, newPlan.ConditionHash);
	}
}