using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class CreatePromotionMainInfoCommandService : IGetService<CreatePromotionMainInfoCommand, Guid>
{
	private readonly IPromotionRepository _promotionRepository;
	private readonly IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> _queryPromotionTypeGroupService;

	public CreatePromotionMainInfoCommandService(
		IPromotionRepository promotionRepository,
		IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup> queryPromotionTypeGroupService)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_queryPromotionTypeGroupService = queryPromotionTypeGroupService ?? throw new ArgumentNullException(nameof(queryPromotionTypeGroupService));
	}

	public async ValueTask<Guid> GetAsync(CreatePromotionMainInfoCommand command, CancellationToken cancellationToken = default)
	{
		var promotionType = await _queryPromotionTypeGroupService
			.QueryAsync(new PromotionTypeGroupsQuery(), cancellationToken)
			.SelectMany(group => group.Types.ToAsyncEnumerable())
			.FirstOrDefaultAsync(type => type.Name == command.PromotionType, cancellationToken)
			.ConfigureAwait(false);

		var createInfo = new CreatePromotionMainInfo(
			command.BrandId,
			command.Name,
			command.EventDuring,
			command.PromotionType,
			promotionType?.SettlementIntervalType,
			command.PromotionLimitAmount,
			command.IsAutoSent);

		return await _promotionRepository
			.CreatePromotionAsync(createInfo, command.Modifier, cancellationToken)
			.ConfigureAwait(false);
	}
}
