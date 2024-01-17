using Tgs.ServiceUtility;
using Tgs.VipLevelService.Clients;
using Tgs.VipLevelService.Common.ViewModels;

namespace Tgs.Promotion.ApplicationServices;

internal class GetBrandVipLevelMapQueryService(IVipLevelServiceClient vipLevelServiceClient)
	: IGetService<GetBrandVipLevelMapQuery, IReadOnlyDictionary<Guid, VipLevelInfoViewModel>>
{
	public async ValueTask<IReadOnlyDictionary<Guid, VipLevelInfoViewModel>> GetAsync(
		GetBrandVipLevelMapQuery query,
		CancellationToken cancellationToken = default)
	{
		var dict = await vipLevelServiceClient
			.VipLevelQueryAsync(query.BrandId, cancellationToken)
			.ToDictionaryAsync(vip => vip.Id, cancellationToken)
			.ConfigureAwait(false);

		return dict.AsReadOnly();
	}
}
