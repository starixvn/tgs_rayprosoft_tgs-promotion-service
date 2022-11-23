using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.Domain.Entities;
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
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusMonthlyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionBonusRepositoryMock,
				fakePlayerBucketClient,
				cashServiceClientMock,
				transactionRepositoryMock,
				promotionRepositoryMock,
				promotionRowServiceMock,
				promotionBonusServiceMock,
				playerGameRebateAggregateServiceMock,
				brandGameServiceMock,
				playerServiceMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);
		var previousMonth = now.AddMonths(-1);
		var firstDayOfMonth = new DateTimeOffset(previousMonth.Year, previousMonth.Month, 1,0,0,0, nowDateTime.Offset);
		var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = firstDayOfMonth.UtcDateTime,
					To = lastDayOfMonth.UtcDateTime,
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
							CreatedOn = firstDayOfMonth.UtcDateTime
						}
					},
					CampaignDetail = "{\"playerAutoJoin\": true}"
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
					ReportDate = firstDayOfMonth.UtcDateTime,
					WinLoss = 100,
					GameType = (short)GameType.Slot,
				}
			}
			.AsQueryable()
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

		_ = promotionRowServiceMock.GetQueries().Returns(promotions);
		_ = playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		_ = brandGameServiceMock.GetQueries().Returns(brandGames);
		_ = playerServiceMock.GetQueries().Returns(players);
		_ = promotionBonusServiceMock.GetQueries().Returns(Array.Empty<PromotionBonus>().AsQueryable().BuildMock());
		_ = promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		_ = transactionServiceMock.GetQueries().Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

		_ = cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var request = new CreatePromotionBonusMonthlyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(1)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Distribute_Zero_Monthly_Promotions_When_No_Promotions()
	{
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusMonthlyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionBonusRepositoryMock,
				fakePlayerBucketClient,
				cashServiceClientMock,
				transactionRepositoryMock,
				promotionRepositoryMock,
				promotionRowServiceMock,
				promotionBonusServiceMock,
				playerGameRebateAggregateServiceMock,
				brandGameServiceMock,
				playerServiceMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var promotionBonusId = Guid.NewGuid();
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));
		var previousMonth = now.AddMonths(-1);
		var firstDayOfMonth = new DateTime(previousMonth.Year, previousMonth.Month, 1);

		var promotions = Array.Empty<PromotionRow>()
			.AsQueryable()
			.BuildMock();

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new PlayerGameRebateAggregate
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = firstDayOfMonth,
					WinLoss = 100,
					GameType = (short)GameType.Slot,
				}
			}
			.AsQueryable()
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

		_ = promotionRowServiceMock.GetQueries().Returns(promotions);
		_ = playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		_ = brandGameServiceMock.GetQueries().Returns(brandGames);
		_ = playerServiceMock.GetQueries().Returns(players);
		_ = promotionBonusServiceMock.GetQueries().Returns(Array.Empty<PromotionBonus>().AsQueryable().BuildMock());
		_ = promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		_ = transactionServiceMock.GetQueries().Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

		_ = cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var request = new CreatePromotionBonusMonthlyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(0)
			.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Distribute_Promotions_With_Is_Auto_Join()
	{
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusMonthlyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionBonusRepositoryMock,
				fakePlayerBucketClient,
				cashServiceClientMock,
				transactionRepositoryMock,
				promotionRepositoryMock,
				promotionRowServiceMock,
				promotionBonusServiceMock,
				playerGameRebateAggregateServiceMock,
				brandGameServiceMock,
				playerServiceMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);
		var previousMonth = now.AddMonths(-1);
		var firstDayOfMonth = new DateTimeOffset(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0, nowDateTime.Offset);
		var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = firstDayOfMonth.UtcDateTime,
					To = lastDayOfMonth.UtcDateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new PromotionParticipant[]
					{
					},
					CampaignDetail = "{\"playerAutoJoin\": true}"
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
					ReportDate = firstDayOfMonth.UtcDateTime,
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

		_ = promotionRowServiceMock.GetQueries().Returns(promotions);
		_ = playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		_ = playerServiceMock.GetQueries().Returns(players);
		_ = brandGameServiceMock.GetQueries().Returns(brandGames);
		_ = promotionBonusServiceMock.GetQueries().Returns(Array.Empty<PromotionBonus>().AsQueryable().BuildMock());
		_ = promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		_ = transactionServiceMock.GetQueries().Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

		_ = cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var request = new CreatePromotionBonusMonthlyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(1)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Distribute_Promotions_With_Is_Not_Auto_Join()
	{
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusMonthlyHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusMonthlyHandler(
				promotionBonusRepositoryMock,
				fakePlayerBucketClient,
				cashServiceClientMock,
				transactionRepositoryMock,
				promotionRepositoryMock,
				promotionRowServiceMock,
				promotionBonusServiceMock,
				playerGameRebateAggregateServiceMock,
				brandGameServiceMock,
				playerServiceMock,
				serialNumberGeneratorMock,
				transactionServiceMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var nowDateTime = DateTimeOffset.UtcNow.ToOffset(new(8, 0, 0));
		var now = new DateTimeOffset(nowDateTime.Year,
			nowDateTime.Month,
			nowDateTime.Day,
			0,
			0,
			0,
			nowDateTime.Offset);
		var previousMonth = now.AddMonths(-1);
		var firstDayOfMonth = new DateTimeOffset(previousMonth.Year, previousMonth.Month, 1, 0, 0, 0, nowDateTime.Offset);
		var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = firstDayOfMonth.UtcDateTime,
					To = lastDayOfMonth.UtcDateTime,
					SettlementIntervalType = "Monthly",
					PromotionType = "ReliefMoneyMonthly",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new PromotionParticipant[]
					{
					},
					CampaignDetail = "{\"playerAutoJoin\": false}"
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
					ReportDate = firstDayOfMonth.UtcDateTime,
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

		_ = promotionRowServiceMock.GetQueries().Returns(promotions);
		_ = playerGameRebateAggregateServiceMock.GetQueries().Returns(gameAggregates);
		_ = playerServiceMock.GetQueries().Returns(players);
		_ = brandGameServiceMock.GetQueries().Returns(brandGames);
		_ = promotionBonusServiceMock.GetQueries().Returns(Array.Empty<PromotionBonus>().AsQueryable().BuildMock());
		_ = promotionBonusRepositoryMock.CreatePromotionBonusAsync(Arg.Any<PromotionBonusModel[]>())
			.Returns(promotionBonuses);
		_ = transactionServiceMock.GetQueries().Returns(Array.Empty<Transaction>().AsQueryable().BuildMock());

		_ = cashServiceClientMock.PromotionAsync(Arg.Any<Guid>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
			Arg.Any<Guid?>()).Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var request = new CreatePromotionBonusMonthlyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(0)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}
}
