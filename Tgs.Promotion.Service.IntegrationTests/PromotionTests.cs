using System.Text.Json;
using ExpectedObjects;
using Google.Protobuf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.MessageQueue;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.SerialNumberGenerator;

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
		var createInfo = new Clients.CreatePromotionMainInfo
		{
			Name = "Test Promotion",
			EventDuring = new During(DateTime.Now.AddDays(-10), DateTime.Now),
			PromotionType = "TestType",
			IsAutoSent = true
		};

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionTypes = new PromotionTypeGroup[]
		{
			new PromotionTypeGroup
			{
				Name = "Turnover",
				Types = new PromotionType[]
				{
					new PromotionType
					{
						Name = "TestType",
						SettlementIntervalType = "Daily"
					}
				}
			}
		};

		_ = await dbContext.Settings.AddAsync(new Setting
		{
			Key = "PromotionTypes",
			Value = JsonSerializer.Serialize(promotionTypes),
			CreatedByUsername = "test",
			UpdatedByUsername = "test"
		});

		_ = await dbContext.SaveChangesAsync();

		var resultId = await sut.CreatePromotionAsync(brandId, createInfo, "ModifyUser");

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
	public async Task Get_Promotion_Content()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now,
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}",
			Content = "Test Promotion Content",
			SubSubject = "Test Promotion Subject",
			Terms = "Test Promotion Terms",
			DesktopBanner = "http://www.google.com/test.png",
			MobileBanner = null,
		});

		_ = await dbContext.SaveChangesAsync();

		var expected = new Tgs.Promotion.Clients.PromotionContentInfo
		{
			Content = "Test Promotion Content",
			SubSubject = "Test Promotion Subject",
			Convention = "Test Promotion Terms",
			Localization = JsonDocument.Parse("{}"),
			PersonalComputerBanner = "http://www.google.com/test.png",
			MobileBanner = null
		};

		var actual = await sut.GetPromotionContentAsync(promotionId);

		Assert.IsNotNull(actual);

		expected
			.ToExpectedObject(ctx =>
				{
					ctx.PushStrategy<JsonDocumentComparisionStrategy>();
					_ = ctx.IgnoreEqualsOverride();
				})
			.ShouldMatch(actual);
	}

	[TestMethod]
	public async Task Get_Promotion_MainInfo()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now,
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}"
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.GetPromotionMainInfoAsync(promotionId);

		Assert.IsNotNull(actual);
		Assert.AreEqual(promotionId, actual.Id);
	}

	[TestMethod]
	public async Task Get_Promotion_Rule()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();

		var vipLevels = new[] { 1, 2 };

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = promotionId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now,
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{\"test\": 111}",
			BonusAmountLimit = 10M,
			GameType = (short)GameType.Lottery,
			Percentage = 20M,
			TurnoverMultiple = 30M
		});

		await dbContext.PromotionVipLevels.AddRangeAsync(
		vipLevels.Select(lvl => new PromotionVipLevel
		{
			Level = lvl,
			PromotionId = promotionId,
			VipLevelId = Guid.NewGuid()
		}).ToArray());

		_ = await dbContext.SaveChangesAsync();

		var expected = new StubPromotionRule
		{
			CompaignDetail = new StubPromotionRuleDetail
			{
				Test = 111
			},
			BonusAmountLimit = 10M,
			GameType = GameType.Lottery,
			Percentage = 20M,
			TurnoverMultiple = 30M,
			VipLevels = vipLevels
		};

		var actual = await sut.GetPromotionRuleAsync<StubPromotionRule>(promotionId);

		Assert.IsNotNull(actual);

		expected
			.ToExpectedObject(ctx =>
				ctx.IgnoreEqualsOverride())
			.ShouldMatch(actual);
	}

	[TestMethod]
	public async Task List_Promotions_By_Brand_Id()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var brandId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = Guid.NewGuid(),
			BrandId = brandId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now.AddDays(1D),
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}"
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.ListPromotionsAsync(brandId).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[TestMethod]
	public async Task List_Promotions_By_PlayerCode()
	{
		var fakePlayerService = Substitute.For<IPlayerServiceClient>();
		var fakeBrandService = Substitute.For<IBrandServiceClient>();

		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services => services
				.AddSingleton(fakePlayerService)
				.AddSingleton(fakeBrandService)));

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var brandId = Guid.NewGuid();
		var playerCode = "test";
		var playerVipLevelId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionRows.AddAsync(new PromotionRow
		{
			Id = Guid.NewGuid(),
			BrandId = brandId,
			Name = "Test Promotion",
			From = DateTime.Now.AddDays(-10D),
			To = DateTime.Now.AddDays(1D),
			PromotionType = "Roulette",
			AutoSentPlayerBonus = true,
			CampaignDetail = "{}"
		});

		_ = await dbContext.SaveChangesAsync();

		_ = fakePlayerService.GetPlayerInfoAsync(Arg.Is(playerCode), Arg.Any<CancellationToken>())
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				VipLevelId = playerVipLevelId,
				BrandId = brandId
			});

		_ = fakeBrandService.GetVipLevelInfosAsync(Arg.Is(brandId), Arg.Any<CancellationToken>())
			.Returns(new[] {
				new VipLevelInfo(playerVipLevelId, 1)
			}.ToAsyncEnumerable());

		var actual = await sut.ListPromotionsAsync(brandId, playerCode).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[TestMethod]
	public async Task List_Promotions_Should_Only_Return_Enabled_Data()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var brandId = Guid.NewGuid();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		await dbContext.PromotionRows.AddRangeAsync(
			new PromotionRow
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Name = "Test Promotion",
				From = DateTime.Now.AddDays(-10D),
				To = DateTime.Now.AddDays(1D),
				PromotionType = "TestType",
				AutoSentPlayerBonus = true,
				CampaignDetail = "{}",
				Status = (short)PromotionStatus.Enabled
			},
			new PromotionRow
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Name = "Test Promotion2",
				From = DateTime.Now.AddDays(-10D),
				To = DateTime.Now.AddDays(1D),
				PromotionType = "TestType",
				AutoSentPlayerBonus = true,
				CampaignDetail = "{}",
				Status = (short)PromotionStatus.Disabled
			},
			new PromotionRow
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Name = "Test Promotion3",
				From = DateTime.Now.AddDays(-10D),
				To = DateTime.Now.AddDays(1D),
				PromotionType = "TestType",
				AutoSentPlayerBonus = true,
				CampaignDetail = "{}",
				Status = (short)PromotionStatus.Archived
			});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.ListPromotionsAsync(brandId).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[TestMethod]
	public async Task Set_Old_Promotion_Rule()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		var gameProviders = new[]
		{
			new GameProvider
			{
				Id = Guid.NewGuid(),
				Code = "GP1",
				Name = "GP1",
				CreatedByUsername = "ModifyUser",
				UpdatedByUsername = "ModifyUser"
			},
			new GameProvider
			{
				Id = Guid.NewGuid(),
				Code = "GP2",
				Name = "GP2",
				CreatedByUsername = "ModifyUser",
				UpdatedByUsername = "ModifyUser"
			}
		};

		await dbContext.GameProviders.AddRangeAsync(gameProviders);

		_ = await dbContext.SaveChangesAsync();

		var rule = new
		{
			GameType = (short)GameType.Lottery,
			BonusAmountLimit = 10M,
			Percentage = 20M,
			TurnoverMultiple = 30M,
			Test = 111,
			GameProviderIds = gameProviders.Select(x => x.Id).ToArray()
		};

		await sut.SetPromotionRuleAsync(promotionId, rule, "ModifyUser");

		var actualGpIds = await dbContext.PromotionGameProviders
			.Select(row => row.GameProviderId)
			.ToArrayAsync()
			.ConfigureAwait(false);

		Assert.AreEqual(rule.BonusAmountLimit, promotionRow.BonusAmountLimit);
		Assert.AreEqual(rule.Percentage, promotionRow.Percentage);
		Assert.AreEqual(rule.TurnoverMultiple, promotionRow.TurnoverMultiple);
		CollectionAssert.AreEquivalent(rule.GameProviderIds, actualGpIds);
	}

	[TestMethod]
	public async Task Set_Promotion_Content()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		_ = await dbContext.SaveChangesAsync();

		var promotionContent = new PromotionContent
		{
			SubSubject = "Test SubSubject",
			PersonalComputerBanner = "https://localhost/banner1.png",
			MobileBanner = "https://localhost/banner2.png",
			Content = "Test Content",
			Convention = "Test Convention",
			Localization = JsonDocument.Parse("{}")
		};

		await sut.SetPromotionContentAsync(promotionId, promotionContent, "ModifyUser");

		Assert.AreEqual(promotionContent.SubSubject, promotionRow.SubSubject);
		Assert.AreEqual(promotionContent.PersonalComputerBanner, promotionRow.DesktopBanner);
		Assert.AreEqual(promotionContent.MobileBanner, promotionRow.MobileBanner);
		Assert.AreEqual(promotionContent.Content, promotionRow.Content);
		Assert.AreEqual(promotionContent.Convention, promotionRow.Terms);
	}

	[TestMethod]
	public async Task Set_Promotion_Rule()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();
		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services => services
				.AddSingleton(fakeMessageSender)));

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		_ = await dbContext.SaveChangesAsync();

		var rule = new
		{
			Test = 111
		};

		var expected = JsonSerializer.Serialize(rule, new JsonSerializerOptions { WriteIndented = false });

		_ = fakeMessageSender
			.RequestAsync(
				Arg.Is("tgs.promotion.testtype.setting"),
				Arg.Any<ReadOnlyMemory<byte>>(),
				Arg.Any<IEnumerable<MessageHeaderValue>>(),
				Arg.Any<CancellationToken>())
			.Returns(new CampaignDetailResponse
			{
				CampaignDetail = expected
			}.ToByteArray());

		await sut.SetPromotionRuleAsync(promotionId, rule, "ModifyUser");

		Assert.AreEqual(expected, promotionRow.CampaignDetail);
	}

	[TestMethod]
	public async Task Set_Promotion_Status()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		_ = await dbContext.SaveChangesAsync();

		await sut.SetPromotionStatusAsync(promotionId, Clients.PromotionStatus.Enabled, "ModifyUser");

		Assert.AreEqual(0, promotionRow.Status);
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
			},
			new VipLevel
			{
				Id = Guid.NewGuid(),
				BrandId = brandId,
				Level = 3,
				CreatedByUsername = "ModifyUser",
				UpdatedByUsername = "ModifyUser"
			}
		);

		_ = await dbContext.PromotionVipLevels.AddAsync(
			new PromotionVipLevel
			{
				PromotionId = promotionId,
				Level = 3,
				VipLevelId = Guid.NewGuid()
			}).ConfigureAwait(false);

		_ = await dbContext.SaveChangesAsync();

		await sut.SetPromotionVipLevelsAsync(promotionId, new[] { 1, 2 }, "ModifyUser");

		var vipRecords = dbContext.PromotionVipLevels.ToArray();

		Assert.AreEqual(2, vipRecords.Length);
	}

	[TestMethod]
	public async Task Tag_Player_Deposit()
	{
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();

		await using var web = new TestPromotionApplication(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton(fakePromotionCheckerFactory)));

		var httpClient = web.CreateClient();

		var sut = new PromotionClient(httpClient);

		var playerCode = "player1";
		var promotionId = Guid.NewGuid();

		var fakeFirstDepositChecker = Substitute.For<IPromotionChecker>();
		var fakeAccountFirstDepositChecker = Substitute.For<IPromotionChecker>();
		var fakeDailyFirstDepositChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeFirstDepositChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeAccountFirstDepositChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeDailyFirstDepositChecker);

		await sut.TagPlayerDepositAsync(playerCode, promotionId);

		_ = fakeFirstDepositChecker.Received(1)
			.TagPromotionTypeAsync(Arg.Is(playerCode), Arg.Is(promotionId));
		_ = fakeAccountFirstDepositChecker.Received(1)
			.TagPromotionTypeAsync(Arg.Is(playerCode), Arg.Is(promotionId));
		_ = fakeDailyFirstDepositChecker.Received(1)
			.TagPromotionTypeAsync(Arg.Is(playerCode), Arg.Is(promotionId));
	}

	[TestMethod]
	public async Task Untag_Player_Deposit()
	{
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();

		await using var web = new TestPromotionApplication(hostBuilder => hostBuilder
			.ConfigureServices(services => services
				.AddSingleton(fakePromotionCheckerFactory)));

		var httpClient = web.CreateClient();

		var sut = new PromotionClient(httpClient);

		var playerCode = "player1";
		var promotionId = Guid.NewGuid();

		var fakeFirstDepositChecker = Substitute.For<IPromotionChecker>();
		var fakeAccountFirstDepositChecker = Substitute.For<IPromotionChecker>();
		var fakeDailyFirstDepositChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeFirstDepositChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeAccountFirstDepositChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeDailyFirstDepositChecker);

		await sut.UntagPlayerDepositAsync(playerCode, promotionId);

		_ = fakeFirstDepositChecker.Received(1)
			.RollbackTagAsync(Arg.Is(playerCode), Arg.Is(promotionId));
		_ = fakeAccountFirstDepositChecker.Received(1)
			.RollbackTagAsync(Arg.Is(playerCode), Arg.Is(promotionId));
		_ = fakeDailyFirstDepositChecker.Received(1)
			.RollbackTagAsync(Arg.Is(playerCode), Arg.Is(promotionId));
	}

	[TestMethod]
	public async Task Update_Promotion_MainInfo()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		var createInfo = new Clients.CreatePromotionMainInfo
		{
			Name = "Test Promotion",
			EventDuring = new During(DateTime.Now.AddDays(-10), DateTime.Now),
			PromotionType = "TestType",
			IsAutoSent = true
		};

		var promotionTypes = new PromotionTypeGroup[]
		{
			new PromotionTypeGroup
			{
				Name = "Turnover",
				Types = new PromotionType[]
				{
					new PromotionType
					{
						Name = "NewType",
						SettlementIntervalType = "Daily"
					}
				}
			}
		};

		_ = await dbContext.Settings.AddAsync(new Setting
		{
			Key = "PromotionTypes",
			Value = JsonSerializer.Serialize(promotionTypes),
			CreatedByUsername = "test",
			UpdatedByUsername = "test"
		});

		_ = await dbContext.SaveChangesAsync();

		var newInfo = new Clients.CreatePromotionMainInfo
		{
			Name = "New Name",
			EventDuring = (DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)),
			PromotionType = "NewType",
			IsAutoSent = false,
			PromotionLimitAmount = 100M
		};

		await sut.UpdatePromotionMainInfoAsync(promotionId, newInfo, "ModifyUser");

		Assert.AreEqual(newInfo.Name, promotionRow.Name);
		Assert.AreEqual(newInfo.PromotionType, promotionRow.PromotionType);
		Assert.AreEqual(newInfo.IsAutoSent, promotionRow.AutoSentPlayerBonus);
		Assert.AreEqual(newInfo.PromotionLimitAmount, promotionRow.TotalAmountLimit);
		Assert.AreEqual(0, promotionRow.Unlimited);
		Assert.AreEqual(newInfo.EventDuring.From, promotionRow.From);
		Assert.AreEqual(newInfo.EventDuring.To, promotionRow.To);
	}

	[TestMethod]
	public async Task Update_Promotion_Used_Amount()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
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
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		_ = await dbContext.SaveChangesAsync();

		var snapshotId = Guid.NewGuid();

		await sut.UpdatePromotionUsedAmountAsync(promotionId, 100M, snapshotId);

		Assert.AreEqual(100M, promotionRow.AmountUsed);
		Assert.AreEqual(snapshotId, promotionRow.AmountUsedSnapshotId);
	}

	[TestMethod]
	public async Task Update_Promotion_Used_Amount_But_SnapshotId_Less_Then_Origin_Will_Not_Update()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionServiceClient(httpClient);

		var serialNumberGenerator = web.Services.GetRequiredService<ISerialNumberGenerator>();

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var firstSnapshotId = await serialNumberGenerator.GenerateAsync();
		var secondSnapshotId = await serialNumberGenerator.GenerateAsync();

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var promotionRow = new PromotionRow
		{
			Id = promotionId,
			BrandId = brandId,
			From = DateTime.Now.AddDays(-10),
			To = DateTime.Now,
			Name = "Test Promotion",
			PromotionType = "TestType",
			AutoSentPlayerBonus = true,
			Status = 1,
			CampaignDetail = "{}",
			AmountUsed = 999M,
			AmountUsedSnapshotId = secondSnapshotId
		};

		_ = await dbContext.PromotionRows.AddAsync(promotionRow);

		_ = await dbContext.SaveChangesAsync();

		await sut.UpdatePromotionUsedAmountAsync(promotionId, 1000M, firstSnapshotId);

		Assert.AreEqual(999M, promotionRow.AmountUsed);
	}
}
