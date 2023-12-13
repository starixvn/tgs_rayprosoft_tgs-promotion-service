using System.Collections.Immutable;
using Tgs.BrandCertificate.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetBrandVipLevelMapQueryService(IBrandServiceClient brandServiceClient) : IGetService<GetBrandVipLevelMapQuery, IImmutableDictionary<Guid, VipLevelInfo>>
{
	private readonly IBrandServiceClient _brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));

	public async ValueTask<IImmutableDictionary<Guid, VipLevelInfo>> GetAsync(GetBrandVipLevelMapQuery query, CancellationToken cancellationToken = default)
	{
		var dict = await _brandServiceClient
			.GetVipLevelInfosAsync(query.BrandId, cancellationToken)
			.ToDictionaryAsync(info => info.Id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return dict.ToImmutableDictionary();
	}
}
