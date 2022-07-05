using System;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionReliefMoneyWeeklyDistributionTests
{
	[Test]
	public async Task Can_Distribute_Weekly_Promotions()
	{
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var playerBucketRepositoryMock = Substitute.For<IPlayerBucketRepository>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusWeeklyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusWeeklyHandler(
				promotionBonusServiceMock,
				playerBucketRepositoryMock,
				cashServiceClientMock,
				playerGameRebateAggregateServiceMock,
				playerServiceMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				transactionRepositoryMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				brandGameServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
				new PromotionRow
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

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new PlayerGameRebateAggregate
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = previousMonday.DateTime,
					WinLoss = 100,
					GameType = (short)GameType.Slot,

				}
			}.AsQueryable()
			.BuildMock();

		var brandGames = new[]
			{
				new BrandGame
				{
					GameId = 123,

				}
			}
			.AsQueryable()
			.BuildMock();

		var players = new Player[]
		{
			new Player
			{
				BrandId = brandId,
				PlayerCode = playerCode,
				DefaultWalletId= walletId,
			}
		}.AsQueryable()
			.BuildMock();

		var promotionBonuses = new PromotionBonus[]
		{
			new PromotionBonus
			{
				Id = promotionBonusId,
				BrandId = brandId,
				PromotionPlayers = new PromotionPlayer[]
				{
					new PromotionPlayer
					{
						Id=1,
						PlayerCode= playerCode,
						BonusAmount = 10,
						
					}
				}
			}
		};

		promotionRepositoryMock.GetPromotionQueries().Returns(promotions);
		playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		playerServiceMock.GetQueries().Returns(players);
		brandGameServiceMock.GetQueries().Returns(brandGames);
		promotionBonusServiceMock.GetQueries().Returns(new PromotionBonus[]{}.AsQueryable().BuildMock());
		promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		transactionServiceMock.GetQueries().Returns(new Transaction[]
		{
		}.AsQueryable().BuildMock());

		cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse{TransactionId=Guid.NewGuid()});

		var request = new CreatePromotionBonusWeeklyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		await cashServiceClientMock.Received(1)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Distribute_Zero_Weekly_Promotions_When_No_Promotions()
	{
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var playerBucketRepositoryMock = Substitute.For<IPlayerBucketRepository>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusWeeklyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusWeeklyHandler(
				promotionBonusServiceMock,
				playerBucketRepositoryMock,
				cashServiceClientMock,
				playerGameRebateAggregateServiceMock,
				playerServiceMock,
				promotionRepositoryMock,
				promotionBonusRepositoryMock,
				transactionRepositoryMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				brandGameServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
			}
			.AsQueryable()
			.BuildMock();

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new PlayerGameRebateAggregate
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = previousMonday.DateTime,
					WinLoss = 100,
					GameType = (short)GameType.Slot,

				}
			}.AsQueryable()
			.BuildMock();

		var brandGames = new[]
			{
				new BrandGame
				{
					GameId = 123,

				}
			}
			.AsQueryable()
			.BuildMock();

		var players = new Player[]
		{
			new Player
			{
				BrandId = brandId,
				PlayerCode = playerCode,
				DefaultWalletId= walletId,
			}
		}.AsQueryable()
			.BuildMock();

		var promotionBonuses = new PromotionBonus[]
		{
			new PromotionBonus
			{
				Id = promotionBonusId,
				BrandId = brandId,
				PromotionPlayers = new PromotionPlayer[]
				{
					new PromotionPlayer
					{
						Id=1,
						PlayerCode= playerCode,
						BonusAmount = 10,

					}
				}
			}
		};

		promotionRepositoryMock.GetPromotionQueries().Returns(promotions);
		playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		playerServiceMock.GetQueries().Returns(players);
		brandGameServiceMock.GetQueries().Returns(brandGames);
		promotionBonusServiceMock.GetQueries().Returns(new PromotionBonus[] { }.AsQueryable().BuildMock());
		promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		transactionServiceMock.GetQueries().Returns(new Transaction[]
		{
		}.AsQueryable().BuildMock());

		cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var request = new CreatePromotionBonusWeeklyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		await cashServiceClientMock.Received(0)
			.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

}