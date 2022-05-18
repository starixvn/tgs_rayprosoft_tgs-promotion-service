using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		var createInfo = new CreatePromotionMainInfo<object>
		{
			Name = "Test Promotion",
			EventDuring = new During(DateTime.Now.AddDays(-10), DateTime.Now),
			PromotionType = "TestType",
			IsAutoSent = true
		};

		var resultId = await sut.CreatePromotionAsync(brandId, createInfo, "ModifyUser");

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var actual = dbContext.PromotionRows.First();

		Assert.AreEqual(brandId, actual.BrandId);
		Assert.AreEqual(createInfo.EventDuring.From.ToUniversalTime(), actual.From.ToUniversalTime());
		Assert.AreEqual(createInfo.EventDuring.To.ToUniversalTime(), actual.To.ToUniversalTime());
		Assert.AreEqual(createInfo.Name, actual.Name);
		Assert.AreEqual(createInfo.PromotionType, actual.PromotionType);
		Assert.AreEqual(createInfo.IsAutoSent, actual.AutoSentPlayerBonus);
		Assert.AreEqual(1, actual.Status);
		Assert.AreEqual(resultId, actual.Id);
	}

	[TestMethod]
	public async Task Set_Promotion_VipLevels()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			BrandId = brandId,
			From = DateTime.Now.AddDays(-10),
			To = DateTime.Now,
			Name = "Test Promotion",
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			Status = 1,
			CampaignDetail = "{}"
		});

		await dbContext.VipLevels.AddRangeAsync(
			new VipLevel
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Level = 1,
				CreatedByUsername = "ModifyUser",
				UpdatedByUsername = "ModifyUser"
			},
			new VipLevel
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Level = 2,
				CreatedByUsername = "ModifyUser",
				UpdatedByUsername = "ModifyUser"
			}
		);

		_ = await dbContext.SaveChangesAsync();

		await sut.SetPromotionVipLevelsAsync(promotionId, new[] { 1, 2 }, "ModifyUser");

		var vipRecords = dbContext.PromotionVipLevels.ToArray();

		Assert.AreEqual(2, vipRecords.Length);
	}
}
