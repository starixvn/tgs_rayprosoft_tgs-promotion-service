using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionDistributeTests
{
	[TestMethod]
	public async Task Can_Create_Promotion_Distribute()
	{
		var planId = Guid.NewGuid();

		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.Players.AddAsync(new Player
		{
			PlayerCode = playerCode,
			DefaultWalletCurrencyCode = "CNY",
			Password = Array.Empty<byte>(),
			Salt = Array.Empty<byte>(),
			Username = "player",
			WithdrawPassword = Array.Empty<byte>(),
			WithdrawSalt = Array.Empty<byte>()
		});

		_ = await dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
		{
			Id = planId,
			PromotionId = promotionId,
			BrandId = Guid.NewGuid(),
			ConditionHash = 0L
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.DistributePromotionBonusAsync(
			planId,
			playerCode,
			10M,
			100M,
			null);

		var record = dbContext.PromotionDistributes.First();
		var bucketRecord = dbContext.PlayerBuckets.First();

		Assert.AreNotEqual(Guid.Empty, actual);
		Assert.AreEqual(promotionId, record.PromotionId);
		Assert.AreEqual(10M, record.BonusAmount);
		Assert.AreEqual(100M, record.Bucket);
		Assert.AreEqual(playerCode, record.PlayerCode);
		Assert.AreEqual(planId, record.PlanId);
		Assert.AreEqual(100M, bucketRecord.BucketAmount);
	}

	[TestMethod]
	public async Task Can_Get_Distribute_Plan()
	{
		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var conditionHash = (long)-111;

		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		dbContext.PromotionRows.Add(new PromotionRow
		{
			Id = promotionId,
			BrandId = brandId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now,
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}"
		});

		await dbContext.SaveChangesAsync();

		var actual = await sut.GetPromotionDistributePlanAsync(promotionId, conditionHash);

		Assert.IsNotNull(actual);
		Assert.AreEqual(promotionId, actual.PromotionId);
		Assert.AreEqual(brandId, actual.BrandId);
		Assert.AreEqual(conditionHash, actual.ConditionHash);
	}
}