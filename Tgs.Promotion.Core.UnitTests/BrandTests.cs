using NSubstitute;
using Tgs.Promotion.ApplicationServices;
using Tgs.VipLevelService.Clients;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class BrandTests
{
	[TestMethod]
	public async Task Get_VipLevel_Dictionary_By_Brand()
	{
		var fakeVipLevelServiceClient = Substitute.For<IVipLevelServiceClient>();

		var sut = new GetBrandVipLevelMapQueryService(fakeVipLevelServiceClient);

		var query = new GetBrandVipLevelMapQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakeVipLevelServiceClient.Received(1)
			.GetBrandAllVipLevelsAsync(Arg.Is(query.BrandId));
	}
}
