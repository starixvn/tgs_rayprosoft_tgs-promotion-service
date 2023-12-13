using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;

namespace Tgs.Promotion.ApplicationServices;

internal class GetBrandVipLevelMapQueryService(IVipLevelServiceClient vipLevelServiceClient)
	: IGetService<GetBrandVipLevelMapQuery, IReadOnlyDictionary<Guid, VipLevelInfo>>
{
	public async ValueTask<IReadOnlyDictionary<Guid, VipLevelInfo>> GetAsync(GetBrandVipLevelMapQuery query, CancellationToken cancellationToken = default)
	{
		var dict = await vipLevelServiceClient
			.GetBrandAllVipLevelsAsync(query.BrandId, cancellationToken)
			.ToDictionaryAsync(vip => vip.Id, cancellationToken)
			.ConfigureAwait(false);

		return dict.AsReadOnly();
	}
}
