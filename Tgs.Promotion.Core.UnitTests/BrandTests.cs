using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.ApplicationServices;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class BrandTests
{
	[TestMethod]
	public async Task Get_VipLevel_Dictionary_By_Brand()
	{
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new GetBrandVipLevelMapQueryService(fakeBrandServiceClient);

		var query = new GetBrandVipLevelMapQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeBrandServiceClient.Received(1)
			.GetVipLevelInfosAsync(Arg.Is(query.BrandId));
	}
}
