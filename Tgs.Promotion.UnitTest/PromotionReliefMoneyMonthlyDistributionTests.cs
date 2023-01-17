using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.BrandCertificate.Clients;
using Tgs.CashService.Client;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.ApplicationServices.Promotion.Workers;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionReliefMoneyMonthlyDistributionTests
{
	[Test]
	public async Task Can_Distribute_Monthly_Promotions()
	{
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var funcDistributionMock = Substitute.For<Func<string, IDistribution>>();
		var brandServiceClientMock = Substitute.For<IBrandServiceClient>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionRepositoryMock,
				brandServiceClientMock,
				funcDistributionMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(7, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.DateTime,
					To = previousLastDayOfMonth.DateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.DateTime
						}
					}
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		_ = brandServiceClientMock.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new Guid[]
		{
			brandId
		}.ToAsyncEnumerable());

		var request = new CreatePromotionBonusMonthlyRequest { TimeSpanMinutes = 480 };

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = funcDistributionMock.Received(1)
			.Invoke(Arg.Is("ReliefMoneyMonthly"));
	}

	[Test]
	public async Task Check_Player_Auto_Join_With_True()
	{
		var distributionTaskQueueMock = Substitute.For<IDistributionTaskQueue>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyMonthlyDistributionMock =
			new ReliefMoneyMonthlyDistribution(
				distributionTaskQueueMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.DateTime,
					To = previousLastDayOfMonth.DateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.DateTime
						}
					},
					CampaignDetail = "{\"playerAutoJoin\": true}"
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = reportDate.ToUniversalTime().DateTime,
					WinLoss = 100,
					GameType = (short)GameType.Slot
				}
			}.AsQueryable()
			.BuildMock();

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable().BuildMock();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable().BuildMock();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable().BuildMock();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable().BuildMock();
		var mockBrandGameSet = GetDbSet(brandGames);
		tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = Array.Empty<PromotionParticipant>().AsQueryable().BuildMock();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		_ = reliefMoneyMonthlyDistributionMock.SetPromotion(promotions.First(), previousFirstDayOfMonth,
			previousLastDayOfMonth);
		_ = await reliefMoneyMonthlyDistributionMock.SetConditionDataAsync(default);

		_ = tgsContextMock.Received(0).PromotionParticipants;
	}

	[Test]
	public async Task Check_Player_Auto_Join_With_None()
	{
		var distributionTaskQueueMock = Substitute.For<IDistributionTaskQueue>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyMonthlyDistributionMock =
			new ReliefMoneyMonthlyDistribution(
				distributionTaskQueueMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.DateTime,
					To = previousLastDayOfMonth.DateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.DateTime
						}
					},
					CampaignDetail = "{}"
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = reportDate.ToUniversalTime().DateTime,
					WinLoss = 100,
					GameType = (short)GameType.Slot
				}
			}.AsQueryable()
			.BuildMock();

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable().BuildMock();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable().BuildMock();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable().BuildMock();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable().BuildMock();
		var mockBrandGameSet = GetDbSet(brandGames);
		tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = Array.Empty<PromotionParticipant>().AsQueryable().BuildMock();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		_ = reliefMoneyMonthlyDistributionMock.SetPromotion(promotions.First(), previousFirstDayOfMonth,
			previousLastDayOfMonth);
		_ = await reliefMoneyMonthlyDistributionMock.SetConditionDataAsync(default);

		_ = tgsContextMock.Received(1).PromotionParticipants;
	}

	[Test]
	public async Task Check_Bonus_Amount()
	{
		var distributionTaskQueueMock = Substitute.For<IDistributionTaskQueue>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyMonthlyDistributionMock =
			new ReliefMoneyMonthlyDistribution(
				distributionTaskQueueMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.UtcDateTime,
					To = previousLastDayOfMonth.UtcDateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = false,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PromotionId = promotionId,
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.UtcDateTime
						}
					},
					CampaignDetail = "{}"
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousFirstDayOfMonth.UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				},
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousFirstDayOfMonth.AddDays(-1).UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				},
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousFirstDayOfMonth.UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				}
			}.AsQueryable()
			.BuildMock();

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable().BuildMock();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable().BuildMock();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable().BuildMock();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable().BuildMock();
		var mockBrandGameSet = GetDbSet(brandGames);
		tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = promotions.SelectMany(x => x.PromotionParticipants).AsQueryable().BuildMock();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		var promotionBonuses = Array.Empty<PromotionBonus>().AsQueryable().BuildMock();
		var mockPromotionBonus = GetDbSet(promotionBonuses);
		tgsContextMock.PromotionBonuses.Returns(mockPromotionBonus.Object);

		_ = reliefMoneyMonthlyDistributionMock.SetPromotion(promotions.First(), previousFirstDayOfMonth,
			previousLastDayOfMonth);
		_ = await reliefMoneyMonthlyDistributionMock.SetConditionDataAsync(default);
		_ = await reliefMoneyMonthlyDistributionMock.CalculateBonusAmountAsync(default);
		_ = await reliefMoneyMonthlyDistributionMock.DistributeBonusAsync(default);

		_ = tgsContextMock.Received(1).PromotionParticipants;
		_ = promotionBonusRepositoryMock.Received(1).CreatePromotionBonusAsync(
			Arg.Is<PromotionBonusModel>(x => x.Players.Any(p => p.BonusAmount == 20)));
	}

	[Test]
	public async Task Check_Bonus_Amount_With_Plus7_Time_Zone()
	{
		var distributionTaskQueueMock = Substitute.For<IDistributionTaskQueue>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyMonthlyDistributionMock =
			new ReliefMoneyMonthlyDistribution(
				distributionTaskQueueMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(7, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.UtcDateTime,
					To = previousLastDayOfMonth.UtcDateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = false,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PromotionId = promotionId,
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.UtcDateTime
						}
					},
					CampaignDetail = "{}"
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousFirstDayOfMonth.UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				},
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousFirstDayOfMonth.AddDays(-1).UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				},
				new()
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDateTime = previousLastDayOfMonth.AddHours(3).UtcDateTime,
					WinLoss = 100,
					GameProviderId = Guid.NewGuid(),
					VipLevel = 1,
					GameType = (short)GameType.Slot
				}
			}.AsQueryable()
			.BuildMock();

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable().BuildMock();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable().BuildMock();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable().BuildMock();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable().BuildMock();
		var mockBrandGameSet = GetDbSet(brandGames);
		tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = promotions.SelectMany(x => x.PromotionParticipants).AsQueryable().BuildMock();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		var promotionBonuses = Array.Empty<PromotionBonus>().AsQueryable().BuildMock();
		var mockPromotionBonus = GetDbSet(promotionBonuses);
		tgsContextMock.PromotionBonuses.Returns(mockPromotionBonus.Object);

		_ = reliefMoneyMonthlyDistributionMock.SetPromotion(promotions.First(), previousFirstDayOfMonth,
			previousLastDayOfMonth);
		_ = await reliefMoneyMonthlyDistributionMock.SetConditionDataAsync(default);
		_ = await reliefMoneyMonthlyDistributionMock.CalculateBonusAmountAsync(default);
		_ = await reliefMoneyMonthlyDistributionMock.DistributeBonusAsync(default);

		_ = tgsContextMock.Received(1).PromotionParticipants;
		_ = promotionBonusRepositoryMock.Received(1).CreatePromotionBonusAsync(
			Arg.Is<PromotionBonusModel>(x => x.Players.Any(p => p.BonusAmount == 20)));
	}

	[Test]
	public async Task Can_Distribute_Monthly_Promotions_With_Plus7_Time_Zone()
	{
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var funcDistributionMock = Substitute.For<Func<string, IDistribution>>();
		var brandServiceClientMock = Substitute.For<IBrandServiceClient>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionRepositoryMock,
				brandServiceClientMock,
				funcDistributionMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(7, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousFirstDayOfMonth.DateTime,
					To = previousLastDayOfMonth.DateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new[]
					{
						new PromotionParticipant
						{
							PlayerCode = playerCode,
							CreatedOn = previousFirstDayOfMonth.DateTime
						}
					}
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		_ = brandServiceClientMock.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new Guid[]
		{
			brandId
		}.ToAsyncEnumerable());

		var request = new CreatePromotionBonusMonthlyRequest { TimeSpanMinutes = 420 };

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = funcDistributionMock.Received(1)
			.Invoke(Arg.Is("ReliefMoneyMonthly"));
	}

	private static Mock<DbSet<T>> GetDbSet<T>(IQueryable<T> data) where T : class
	{
		var mockSet = new Mock<DbSet<T>>();
		mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
		mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
		mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
		mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
		return mockSet;
	}
}