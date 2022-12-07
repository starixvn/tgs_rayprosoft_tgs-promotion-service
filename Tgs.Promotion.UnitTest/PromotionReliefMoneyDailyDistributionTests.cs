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
public class PromotionReliefMoneyDailyDistributionTests
{
	[Test]
	public async Task Can_Distribute_Daily_Promotions()
	{
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var fakePlayerBucketCleint = Substitute.For<IPlayerBucketClient>();
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
		var logMock = Substitute.For<ILogger<CreatePromotionBonusDailyQueryHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusDailyQueryHandler(
				promotionBonusRepositoryMock,
				fakePlayerBucketCleint,
				cashServiceClientMock,
				transactionRepositoryMock,
				promotionRepositoryMock,
				promotionRowServiceMock,
				promotionBonusServiceMock,
				playerGameRebateAggregateServiceMock,
				brandGameServiceMock,
				playerServiceMock,
				transactionServiceMock,
				serialNumberGeneratorMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = reportDate.ToUniversalTime().DateTime,
					To = reportDate.ToUniversalTime().DateTime,
					SettlementIntervalType = "Daily",
					PromotionType = "ReliefMoneyDaily",
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
							CreatedOn = reportDate.ToUniversalTime().DateTime
						}
					},
					CampaignDetail = "{}"
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
					ReportDate = reportDate.ToUniversalTime().DateTime,
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

		var request = new CreatePromotionBonusDailyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(1)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Distribute_Zero_Daily_Promotions_When_No_Promotions()
	{
		var promotionRowServiceMock = Substitute.For<ITgsService<PromotionRow>>();
		var brandGameServiceMock = Substitute.For<ITgsService<BrandGame>>();
		var cashServiceClientMock = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var promotionBonusRepositoryMock = Substitute.For<IPromotionBonusRepository>();
		var playerGameRebateAggregateServiceMock = Substitute.For<ITgsService<PlayerGameRebateAggregate>>();
		var playerServiceMock = Substitute.For<ITgsService<Player>>();
		var promotionBonusServiceMock = Substitute.For<ITgsService<PromotionBonus>>();
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var serialNumberGeneratorMock = Substitute.For<ISerialNumberGenerator>();
		var transactionRepositoryMock = Substitute.For<ITransactionRepository>();
		var transactionServiceMock = Substitute.For<ITgsService<Transaction>>();
		var logMock = Substitute.For<ILogger<CreatePromotionBonusDailyQueryHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusDailyQueryHandler(
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
				transactionServiceMock,
				serialNumberGeneratorMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var promotions = Array.Empty<PromotionRow>()
			.AsQueryable()
			.BuildMock();

		var gameAggregates = new PlayerGameRebateAggregate[]
			{
				new PlayerGameRebateAggregate
				{
					PlayerCode = playerCode,
					BrandId = brandId,
					ReportDate = reportDate.ToUniversalTime().DateTime,
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

		var request = new CreatePromotionBonusDailyRequest();

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
		var logMock = Substitute.For<ILogger<CreatePromotionBonusDailyQueryHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusDailyQueryHandler(
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
				transactionServiceMock,
				serialNumberGeneratorMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = reportDate.ToUniversalTime().DateTime,
					To = reportDate.ToUniversalTime().DateTime,
					SettlementIntervalType = "Daily",
					PromotionType = "ReliefMoneyDaily",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = Array.Empty<PromotionParticipant>(),
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
					ReportDate = reportDate.ToUniversalTime().DateTime,
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

		var request = new CreatePromotionBonusDailyRequest();

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
		var logMock = Substitute.For<ILogger<CreatePromotionBonusDailyQueryHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusDailyQueryHandler(
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
				transactionServiceMock,
				serialNumberGeneratorMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = reportDate.ToUniversalTime().DateTime,
					To = reportDate.ToUniversalTime().DateTime,
					SettlementIntervalType = "Daily",
					PromotionType = "ReliefMoneyDaily",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = new PromotionParticipant[]
					{
						new PromotionParticipant
						{
							PlayerCode = playerCode,
							CreatedOn = reportDate.ToUniversalTime().DateTime
						}
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
					ReportDate = reportDate.ToUniversalTime().DateTime,
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

		var request = new CreatePromotionBonusDailyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(1)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Can_Not_Distribute_Promotions_With_Is_Not_Auto_Join()
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
		var logMock = Substitute.For<ILogger<CreatePromotionBonusDailyQueryHandler>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusDailyQueryHandler(
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
				transactionServiceMock,
				serialNumberGeneratorMock,
				logMock);

		var brandId = Guid.NewGuid();
		var walletId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionBonusId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();

		var previousDay = DateTimeOffset.Now.ToOffset(new(8, 0, 0)).AddDays(-1);

		var reportDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = reportDate.ToUniversalTime().DateTime,
					To = reportDate.ToUniversalTime().DateTime,
					SettlementIntervalType = "Daily",
					PromotionType = "ReliefMoneyDaily",
					Percentage = 10,
					BonusAmountLimit = 100,
					Unlimited = 1,
					TotalAmountLimit = 1000,
					Status = (short)PromotionStatus.Enabled,
					GameType = (short?)GameType.Slot,
					AutoSentPlayerBonus = true,
					PromotionParticipants = Array.Empty<PromotionParticipant>(),
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
					ReportDate = reportDate.ToUniversalTime().DateTime,
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

		var request = new CreatePromotionBonusDailyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = await cashServiceClientMock.Received(0)
			.PromotionAsync(Arg.Is(walletId), Arg.Is<decimal>(10), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid?>());
	}
}
