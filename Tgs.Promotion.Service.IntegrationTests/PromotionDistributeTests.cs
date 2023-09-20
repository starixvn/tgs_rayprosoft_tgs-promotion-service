using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.PlayerBucket.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionDistributeTests
{
	[TestMethod]
	public async Task Can_Create_Promotion_Distribute()
	{
		var planId = Guid.NewGuid();

		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		await using var web = new TestPromotionApplication(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton(fakePlayerBucketForTypesClient)
				.AddSingleton(fakePlayerServiceClient)));

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = Guid.NewGuid()
			});

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode))
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = Guid.NewGuid().ToString()
			}.ToImmutableDictionary());

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
		{
			Id = planId,
			PromotionId = promotionId,
			BrandId = Guid.NewGuid(),
			ConditionHash = 0L
		});

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			BrandId = Guid.NewGuid(),
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}",
			PromotionType = "Test",
			Name = "TTT"
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.DistributePromotionBonusAsync(
			planId,
			playerCode,
			10M,
			100M,
			null,
			null,
			Array.Empty<string>());

		var record = dbContext.PromotionDistributes.First();

		Assert.AreNotEqual(Guid.Empty, actual);
		Assert.AreEqual(promotionId, record.PromotionId);
		Assert.AreEqual(10M, record.BonusAmount);
		Assert.AreEqual(100M, record.Bucket);
		Assert.AreEqual(playerCode, record.PlayerCode);
		Assert.AreEqual(planId, record.PlanId);
		Assert.IsNull(record.ReferenceId);

		_ = fakePlayerBucketForTypesClient.Received(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Is(playerCode),
				Arg.Is(100M),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Can_Create_A_Promotion_Distribute_And_Create_A_Bucket_By_Bucket_Type()
	{
		var planId = Guid.NewGuid();

		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		await using var web = new TestPromotionApplication(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton(fakePlayerBucketForTypesClient)
				.AddSingleton(fakePlayerServiceClient)));

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = Guid.NewGuid()
			});

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode))
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = Guid.NewGuid().ToString()
			}.ToImmutableDictionary());

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
		{
			Id = planId,
			PromotionId = promotionId,
			BrandId = Guid.NewGuid(),
			ConditionHash = 0L
		});

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			BrandId = Guid.NewGuid(),
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}",
			PromotionType = "Test",
			Name = "TTT"
		});

		_ = await dbContext.SaveChangesAsync();

		var bucketTypes = new[] { "SportsBook" };

		var actual = await sut.DistributePromotionBonusAsync(
			planId,
			playerCode,
			10M,
			100M,
			null,
			null,
			bucketTypes);

		var record = dbContext.PromotionDistributes.First();

		Assert.AreNotEqual(Guid.Empty, actual);
		Assert.AreEqual(promotionId, record.PromotionId);
		Assert.AreEqual(10M, record.BonusAmount);
		Assert.AreEqual(100M, record.Bucket);
		Assert.AreEqual(playerCode, record.PlayerCode);
		Assert.AreEqual(planId, record.PlanId);
		Assert.IsNull(record.ReferenceId);

		_ = fakePlayerBucketForTypesClient.Received(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Is(playerCode),
				Arg.Is(100M),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Is<IEnumerable<string>>(x => x.SequenceEqual(bucketTypes)),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Can_Create_Promotion_Distribute_With_ReferenceId()
	{
		var planId = Guid.NewGuid();

		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		await using var web = new TestPromotionApplication(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton(fakePlayerBucketForTypesClient)
				.AddSingleton(fakePlayerServiceClient)));

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";
		var referenceId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = Guid.NewGuid()
			});

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode))
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = Guid.NewGuid().ToString()
			}.ToImmutableDictionary());

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
		{
			Id = planId,
			PromotionId = promotionId,
			BrandId = Guid.NewGuid(),
			ConditionHash = 0L
		});

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			BrandId = Guid.NewGuid(),
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}",
			PromotionType = "Test",
			Name = "TTT"
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.DistributePromotionBonusAsync(
			planId,
			playerCode,
			10M,
			100M,
			null,
			referenceId,
			Array.Empty<string>());

		var record = dbContext.PromotionDistributes.First();

		Assert.AreNotEqual(Guid.Empty, actual);
		Assert.AreEqual(promotionId, record.PromotionId);
		Assert.AreEqual(10M, record.BonusAmount);
		Assert.AreEqual(100M, record.Bucket);
		Assert.AreEqual(playerCode, record.PlayerCode);
		Assert.AreEqual(planId, record.PlanId);
		Assert.AreEqual(referenceId, record.ReferenceId);

		_ = fakePlayerBucketForTypesClient.Received(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Is(playerCode),
				Arg.Is(100M),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());
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

		_ = dbContext.PromotionRows.Add(new PromotionRow
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

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.GetPromotionDistributePlanAsync(promotionId, conditionHash);

		Assert.IsNotNull(actual);
		Assert.AreEqual(promotionId, actual.PromotionId);
		Assert.AreEqual(brandId, actual.BrandId);
		Assert.AreEqual(conditionHash, actual.ConditionHash);
	}
}
