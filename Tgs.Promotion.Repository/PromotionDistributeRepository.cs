using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

public class PromotionDistributeRepository : IPromotionDistributeRepository
{
	private readonly ITgsService<PromotionDistribute> _tgsService;

	public PromotionDistributeRepository(ITgsService<PromotionDistribute> tgsService)
		=> _tgsService = tgsService ?? throw new ArgumentNullException(nameof(tgsService));

	public async ValueTask<Guid> CreatePromotionDistributeAsync(Guid brandId, Guid promotionId, Guid? planId,
		decimal bonusAmount, string promotionType, string playerCode, string? distributor)
	{
		var id = Guid.NewGuid();

		var distribute = new PromotionDistribute
		{
			Id = id,
			BrandId = brandId,
			PromotionId = promotionId,
			PlanId = planId,
			PlayerCode = playerCode,
			PromotionType = promotionType,
			BonusAmount = bonusAmount,
			Distributor = distributor,
			CreatedOn = DateTime.UtcNow,
			SentOn = DateTime.UtcNow
		};

		await _tgsService.CreateEntityAsync(distribute).ConfigureAwait(false);

		return id;
	}

	public ValueTask<bool> ExistsAsync(Guid promotionId, string conditionHash, string playerCode)
		=> throw new NotImplementedException();
}