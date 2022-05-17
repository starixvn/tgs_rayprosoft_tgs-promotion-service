using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.Client;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionTests
{
	[TestMethod]
	public async Task Create_Promotion()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var brandId = Guid.NewGuid();
		var createInfo = new CreatePromotionInfo<object>
		{
			Name = "Test Promotion",
			EventDuring = new During(DateTime.Now.AddDays(-10), DateTime.Now),
			PromotionType = "TestType",
			IsAutoSent = true,
			CampaignDetail = new object()
		};

		await sut.CreatePromotionAsync(brandId, createInfo, "ModifyUser");

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var actual = dbContext.Promotions.First();

		Assert.AreEqual(brandId, actual.BrandId);
		Assert.AreEqual(createInfo.EventDuring.From.ToUniversalTime(), actual.From.ToUniversalTime());
		Assert.AreEqual(createInfo.EventDuring.To.ToUniversalTime(), actual.To.ToUniversalTime());
		Assert.AreEqual(createInfo.Name, actual.Name);
		Assert.AreEqual(createInfo.PromotionType, actual.PromotionType);
		Assert.AreEqual(createInfo.IsAutoSent, actual.AutoSentPlayerBonus);
	}
}
