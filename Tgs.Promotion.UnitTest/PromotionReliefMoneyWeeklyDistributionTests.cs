using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionReliefMoneyWeeklyDistributionTests
{
	[Test]
	public async Task Can_Distribute_Weekly_Promotions()
	{
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var funcDistributionMock = Substitute.For<Func<string, IDistribution>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusWeeklyHandler(
				promotionRepositoryMock,
				funcDistributionMock);

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

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousMonday.DateTime,
					To = previousSunday.DateTime,
					SettlementIntervalType = "Weekly",
					PromotionType = "ReliefMoneyWeekly",
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
							CreatedOn = previousMonday.DateTime
						}
					}
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var request = new CreatePromotionBonusWeeklyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = funcDistributionMock.Received(1)
			.Invoke(Arg.Is("ReliefMoneyWeekly"));
	}

	[Test]
	public async Task Check_Player_Auto_Join_With_True()
	{
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var playerBucketClientMock = Substitute.For<IPlayerBucketClient>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var loggerMock = Substitute.For<ILogger<CreatePromotionBonusWeeklyHandler>>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyWeeklyDistributionMock =
			new ReliefMoneyWeeklyDistribution(
				playerBucketClientMock,
				cashServiceClientMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				transactionRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock,
				loggerMock);

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

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousMonday.DateTime,
					To = previousSunday.DateTime,
					SettlementIntervalType = "Weekly",
					PromotionType = "ReliefMoneyWeekly",
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
							CreatedOn = previousMonday.DateTime
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

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		_ = tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		_ = tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		_ = tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable();
		var mockBrandGameSet = GetDbSet(brandGames);
		_ = tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = Array.Empty<PromotionParticipant>().AsQueryable();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		_ = tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		_ = reliefMoneyWeeklyDistributionMock.SetPromotion(promotions.First(), previousMonday, previousSunday);
		_ = await reliefMoneyWeeklyDistributionMock.SetConditionDataAsync(default);

		_ = tgsContextMock.Received(0).PromotionParticipants;
	}

	[Test]
	public async Task Check_Player_Auto_Join_With_None()
	{
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var playerBucketClientMock = Substitute.For<IPlayerBucketClient>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var loggerMock = Substitute.For<ILogger<CreatePromotionBonusWeeklyHandler>>();
		var tgsContextMock = Substitute.For<TgsContext>();

		var reliefMoneyWeeklyDistributionMock =
			new ReliefMoneyWeeklyDistribution(
				playerBucketClientMock,
				cashServiceClientMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				transactionRepositoryMock,
				serialNumberGeneratorMock,
				tgsContextMock,
				loggerMock);

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

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
				new()
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousMonday.DateTime,
					To = previousSunday.DateTime,
					SettlementIntervalType = "Weekly",
					PromotionType = "ReliefMoneyWeekly",
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
							CreatedOn = previousMonday.DateTime
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

		var promotionVipLevels = Array.Empty<PromotionVipLevel>().AsQueryable();
		var mockPromotionVipLevelSet = GetDbSet(promotionVipLevels);
		_ = tgsContextMock.PromotionVipLevels.Returns(mockPromotionVipLevelSet.Object);

		var promotionGameProviders = Array.Empty<PromotionGameProvider>().AsQueryable();
		var mockPromotionGameProviderSet = GetDbSet(promotionGameProviders);
		_ = tgsContextMock.PromotionGameProviders.Returns(mockPromotionGameProviderSet.Object);

		var playerGameAggregates = gameAggregates.AsQueryable();
		var mockPlayerGameAggregateSet = GetDbSet(playerGameAggregates);
		_ = tgsContextMock.PlayerGameRebateAggregates.Returns(mockPlayerGameAggregateSet.Object);

		var brandGames = Array.Empty<BrandGame>().AsQueryable();
		var mockBrandGameSet = GetDbSet(brandGames);
		_ = tgsContextMock.BrandGames.Returns(mockBrandGameSet.Object);

		var promotionParticipants = Array.Empty<PromotionParticipant>().AsQueryable();
		var mockPromotionParticipantSet = GetDbSet(promotionParticipants);
		_ = tgsContextMock.PromotionParticipants.Returns(mockPromotionParticipantSet.Object);

		_ = reliefMoneyWeeklyDistributionMock.SetPromotion(promotions.First(), previousMonday, previousSunday);
		_ = await reliefMoneyWeeklyDistributionMock.SetConditionDataAsync(default);

		_ = tgsContextMock.Received(1).PromotionParticipants;
	}

	private static Mock<DbSet<T>> GetDbSet<T>(IQueryable<T> data) where T : class
	{
		var mockSet = new Mock<DbSet<T>>();
		_ = mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(data.Provider);
		_ = mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
		_ = mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
		_ = mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());
		return mockSet;
	}
}