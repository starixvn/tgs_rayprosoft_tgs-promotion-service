using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class PromotionTypeGroupsQueryService : IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup>
{
	private readonly ISettingsRepository _settingsRepository;

	public PromotionTypeGroupsQueryService(ISettingsRepository settingsRepository)
	{
		_settingsRepository = settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));
	}

	public IAsyncEnumerable<PromotionTypeGroup> QueryAsync(
		PromotionTypeGroupsQuery query,
		CancellationToken cancellationToken = default)
		=> _settingsRepository.GetPromotionTypeGroupsAsync(cancellationToken);
}
