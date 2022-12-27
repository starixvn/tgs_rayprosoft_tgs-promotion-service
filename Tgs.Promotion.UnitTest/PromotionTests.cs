using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using ExpectedObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.MessageQueue;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionTests
{
	[TestCase(true)]
	[TestCase(false)]
	public async Task Can_Get_Promotions(bool showAll)
	{
		var promotionServiceMock = new Mock<IPromotionRepository>();

		var promotionsQueryHandlerMock =
			new GetPromotionsQueryService(promotionServiceMock.Object);

		var request = new GetPromotionsQuery
		{
			BrandId = Guid.NewGuid(),
			Page = 1,
			Rows = 10,
			SortBy = "updatedOn",
			OrderBy = SortOrder.Descending,
			ShowAll = showAll
		};

		var list = CreatePromotionFixture()
			.CreateMany<PromotionRow>(20)
			.AsQueryable()
			.BuildMock();

		foreach (var promotion in list)
		{
			promotion.BrandId = request.BrandId;
		}

		_ = promotionServiceMock.Setup(x =>
				x.GetPromotionQueries(
					It.IsAny<Func<IQueryable<PromotionRow>,
						IQueryable<PromotionRow>>>(),
					false))
			.Returns(list);

		var result = await promotionsQueryHandlerMock.GetAsync(request, new());

		promotionServiceMock.Verify(x => x.GetPromotionQueries(
			It.IsAny<Func<IQueryable<PromotionRow>,
				IQueryable<PromotionRow>>>(),
			false));

		Assert.AreEqual(result.Items.Length, showAll ? 20 : 10);
	}

	[Test]
	public async Task Can_Get_Promotions_Without_Archived()
	{
		var promotionServiceMock = new Mock<IPromotionRepository>();

		var promotionsQueryHandlerMock =
			new GetPromotionsQueryService(promotionServiceMock.Object);

		var request = new GetPromotionsQuery
		{
			BrandId = Guid.NewGuid(),
			Page = 1,
			Rows = 10,
			SortBy = "updatedOn",
			OrderBy = SortOrder.Descending,
			ShowAll = false
		};

		var list = CreatePromotionFixture()
			.CreateMany<PromotionRow>(10)
			.AsQueryable()
			.BuildMock();

		var index = 0;

		foreach (var promotion in list)
		{
			if (index == 0)
			{
				promotion.Status = (short)PromotionStatus.Archived;
			}

			promotion.BrandId = request.BrandId;

			index++;
		}

		_ = promotionServiceMock
			.Setup(x => x.GetPromotionQueries(
				It.IsAny<Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>>(),
				false))
			.Returns(list);

		var result = await promotionsQueryHandlerMock.GetAsync(request, new());

		promotionServiceMock.Verify(x => x.GetPromotionQueries(
			It.IsAny<Func<IQueryable<PromotionRow>,
				IQueryable<PromotionRow>>>(),
			false));

		Assert.AreEqual(result.Items.Length, 9);
	}

	[Test]
	public async Task Can_Revert_Promotion()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new RevertPromotionAmountCommandService(
			fakePromotionRepository,
			NullLogger<RevertPromotionAmountCommandService>.Instance);

		var command = new RevertPromotionAmountCommand(Guid.NewGuid(), 1000M);

		var percentage = 10M;
		var bonusAmountLimit = 500M;

		_ = fakePromotionRepository.GetPromotionByIdAsync(command.PromotionId).Returns(
			new PromotionRow
			{
				Percentage = percentage,
				BonusAmountLimit = bonusAmountLimit
			});

		var amount = command.Amount * percentage / 100M;
		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.RevertPromotionAmountAsync(Arg.Is(command.PromotionId), Arg.Is(amount));
	}

	[Test]
	public async Task Check_Player_Can_Join_Promotion_Should_Be_True()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var checkerFactory = new PromotionCheckerFactory(
			new Dictionary<string, Type>
			{
				["TestPromotionType"] = typeof(IPromotionChecker)
			},
			fakeServiceProvider);

		var sut = new CheckCanJoinPromotionQueryService(
			fakePromotionRepository,
			checkerFactory);

		var query = new CheckCanJoinPromotionQuery(
			Guid.NewGuid(),
			"ThisIsPlayerCode",
			Guid.NewGuid(),
			100);

		_ = fakePromotionRepository
			.GetPromotionByIdAsync(query.PromotionId)
			.Returns(new PromotionRow
			{
				BrandId = query.BrandId,
				PromotionType = "TestPromotionType",
				CampaignDetail = "{}"
			});

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakeChecker
			.CheckJoinAsync(Arg.Any<ConditionData>(), Arg.Any<PromotionMainInfo>(), Arg.Any<PromotionRule>())
			.Returns(true);

		_ = fakeServiceProvider
			.GetService(Arg.Any<Type>())
			.Returns(fakeChecker);

		_ = fakePromotionRepository
			.UpdatePromotionAmountAsync(query.PromotionId, Arg.Any<decimal>())
			.Returns(true);

		var actual = await sut.GetAsync(query);

		Assert.IsTrue(actual);
	}

	[Test]
	public async Task Get_Promotions_By_Player_Should_Return_Match_Promotions()
	{
		var fakeValidPromotionQueryService =
			Substitute.For<IQueryService<GetValidPromotionsQuery, PromotionModel>>();
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePromotionChecker = Substitute.For<IPromotionChecker>();

		var sut = new GetValidPromotionsByPlayerQueryService(
			fakeValidPromotionQueryService,
			fakePlayerService,
			fakePromotionCheckerFactory);

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Any<string>())
			.Returns(fakePromotionChecker);

		_ = fakePromotionChecker.CheckAsync(Arg.Any<string>(), Arg.Any<PromotionMainInfo>())
			.Returns(true);

		var testData = new[]
		{
			new PromotionModel
			{
				Id = Guid.NewGuid(),
				VipLevels = Array.Empty<int>()
			},
			new PromotionModel
			{
				Id = Guid.NewGuid(),
				VipLevels = Array.Empty<int>()
			},
			new PromotionModel
			{
				Id = Guid.NewGuid(),
				VipLevels = Array.Empty<int>()
			}
		};

		var expected = testData.ToExpectedObject();

		var query = new GetValidPromotionsByPlayerQuery(Guid.NewGuid(), "ThisIsPlayerCode");

		_ = fakePlayerService
			.GetPlayerAsync(
				Arg.Any<Expression<Func<Player, bool>>>(),
				Arg.Any<Func<IQueryable<Player>, IQueryable<Player>>>())
			.Returns(new Player
			{
				PlayerCode = query.PlayerCode,
				Vip = new VipLevel
				{
					Level = 1
				}
			});

		_ = fakeValidPromotionQueryService.QueryAsync(Arg.Any<GetValidPromotionsQuery>())
			.Returns(testData.ToAsyncEnumerable());

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		expected.ShouldMatch(actual);
	}

	[Test]
	public void Get_Promotions_Without_Player_Should_Return_All_Match_Promotions()
	{
		var fakePromotionRepository = new Mock<IPromotionRepository>();

		_ = fakePromotionRepository
			.Setup(x => x.GetPromotionQueries(It.IsAny<Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>?>(), default))
			.Returns(Array.Empty<PromotionRow>().AsQueryable().BuildMock());

		var sut = new GetValidPromotionsQueryService(fakePromotionRepository.Object);

		var query = new GetValidPromotionsQuery(Guid.NewGuid());

		_ = sut.QueryAsync(query);

		fakePromotionRepository.Verify(x => x.GetPromotionQueries(It.IsAny<Func<
				IQueryable<PromotionRow>,
				IQueryable<PromotionRow>>>(),
			It.IsAny<bool>()), Times.Once());
	}

	[Test]
	public void Can_Create_Deposit_Participant()
	{
		var fakePromotionRepository = new Mock<IPromotionRepository>();
		var fakePromotionParticipantRepository = new Mock<IPromotionParticipantRepository>();

		_ = fakePromotionRepository
			.Setup(x => x.GetPromotionByIdAsync(It.IsAny<Guid>()))
			.ReturnsAsync(new PromotionRow());

		var sut = new CreateDepositPromotionParticipantQueryService(fakePromotionRepository.Object, fakePromotionParticipantRepository.Object, NullLogger<CreateDepositPromotionParticipantQueryService>.Instance);

		var query = new CreateDepositPromotionParticipantQuery(Guid.NewGuid(),"playerCode");

		_ = sut.GetAsync(query, default);

		fakePromotionParticipantRepository.Verify(x => x.CreatePromotionParticipantAsync(It.IsAny<Guid>(),
			It.IsAny<string>()), Times.Once());
	}

	[Test]
	public void Can_Create_Participant()
	{
		var fakePromotionRepository = new Mock<IPromotionRepository>();
		var fakePromotionParticipantRepository = new Mock<IPromotionParticipantRepository>();
		var fakePromotionTypeRepository = new Mock<IPromotionTypeRepository>();
		var fakePromotionJoinLockRepository = new Mock<IPromotionJoinLockRepository>();
		var fakeMessageSender = new Mock<IMessageSender>();

		_ = fakePromotionRepository
			.Setup(x => x.GetPromotionByIdAsync(It.IsAny<Guid>()))
			.ReturnsAsync(new PromotionRow
			{
				Status = (short)PromotionStatus.Enabled,
				From = DateTime.Now.AddDays(-1),
				To= DateTime.Now.AddDays(1),
				PromotionType = "test"
			});

		_ = fakePromotionTypeRepository
			.Setup(x => x.IsDepositTypeAsync(It.IsAny<string>()))
			.ReturnsAsync(false);

		_ = fakePromotionJoinLockRepository
			.Setup(x => x.SetWhenNotExistsAsync(It.IsAny<string>(), It.IsAny<Guid>()))
			.ReturnsAsync(true);

		var sut = new CreatePromotionParticipantQueryService(
			fakePromotionRepository.Object, 
			fakePromotionParticipantRepository.Object,
			fakePromotionTypeRepository.Object,
			fakePromotionJoinLockRepository.Object,
			fakeMessageSender.Object,
			NullLogger<CreatePromotionParticipantQueryService>.Instance);

		var query = new CreatePromotionParticipantQuery(Guid.NewGuid(), "playerCode");

		_ = sut.GetAsync(query, default);

		fakePromotionParticipantRepository.Verify(x => x.CreatePromotionParticipantAsync(It.IsAny<Guid>(),
			It.IsAny<string>()), Times.Once());
	}

	[Test]
	public void Can_Dispatch_Promotions()
	{
		var fakePromotionBonusRepository = new Mock<IPromotionBonusRepository>();
		var fakeCashServiceClient = new Mock<ICashServiceClient>();
		var fakeTransactionRepository = new Mock<ITransactionRepository>();
		var fakePlayerBucketClient = new Mock<IPlayerBucketClient>();

		var list = CreatePromotionPlayerFixture()
			.CreateMany<PromotionPlayer>(20)
			.AsQueryable()
			.BuildMock();

		_ = fakePromotionBonusRepository
			.Setup(x => x.GetPromotionBonusPlayersQueries(It.IsAny<Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>?>(), It.IsAny<bool>()))
			.Returns(list);

		_ = fakeCashServiceClient
			.Setup(x => x.PromotionAsync(
				It.IsAny<Guid>(), 
				It.IsAny<decimal>(), 
				It.IsAny<string?>(), 
				It.IsAny<string?>(),
				It.IsAny<Guid?>(),
				default))
			.ReturnsAsync(new TransactionResponse{TransactionId= Guid.NewGuid()});

		var sut = new DispatchPromotionBonusQueryService(
			fakePromotionBonusRepository.Object,
			fakeCashServiceClient.Object,
			fakeTransactionRepository.Object,
			fakePlayerBucketClient.Object);

		var query = new DispatchPromotionBonusQuery
		{
			Players = list.Select(x => new DispatchPlayer
			{
				PlayerCode= x.PlayerCode,
				PromotionBonusId= x.PromotionBonusId,
			}).ToArray()
		};

		_ = sut.GetAsync(query, default);

		fakePlayerBucketClient.Verify(x => x.CreateBucketAsync(
			It.IsAny<string>(),
			It.IsAny<decimal>(), 
			It.IsAny<BucketType>(), 
			It.IsAny<string?>(),default), Times.Exactly(20));

		fakePromotionBonusRepository.Verify(x => x.UpdatePromotionBonusPlayersAsync(
			It.IsAny<PromotionPlayer[]>()), Times.Once());

		fakeTransactionRepository.Verify(x => x.CreateTransactionPromotionsAsync(
			It.IsAny<TransactionPromotion[]>()), Times.Once());
	}

	[Test]
	public async Task Can_Get_Joined_Promotions()
	{
		var fakePromotionParticipantRepository = new Mock<IPromotionParticipantRepository>();

		var list = CreatePromotionParticipantFixture()
			.CreateMany<PromotionParticipant>(20)
			.AsQueryable()
			.BuildMock();

		_ = fakePromotionParticipantRepository
			.Setup(x => x.GetPromotionParticipantQueries(It.IsAny<Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>?>(), It.IsAny<bool>()))
			.Returns(list);

		var sut = new GetJoinedPromotionsQueryService(
			fakePromotionParticipantRepository.Object);

		var query = new GetJoinedPromotionsQuery
		{
			PlayerCode = list.First().PlayerCode!
		};

		var response = await sut.GetAsync(query, default);

		Assert.IsTrue(response.Items.Length > 0);
	}

	[Test]
	public async Task Can_Get_Distribution_List()
	{
		var fakePromotionBonusRepository = new Mock<IPromotionBonusRepository>();
		var fakePromotionDistributionRepository = new Mock<IPromotionDistributionRepository>();
		var fakeSettingsRepository = new Mock<ISettingsRepository>();
		var fakePlayerRepository = new Mock<IPlayerRepository>();

		var brandId = Guid.NewGuid();

		var promotionDistributes = CreatePromotionDistributionFixture()
			.CreateMany<PromotionDistribute>(20)
			.AsQueryable()
			.BuildMock();

		var promotionPlayers = CreatePromotionPlayerFixture()
			.CreateMany<PromotionPlayer>(20)
			.AsQueryable()
			.BuildMock();

		_ = fakeSettingsRepository
			.Setup(x => x.GetSettingAsync(It.IsAny<Expression<Func<Setting, bool>>>(),It.IsAny<Func<IQueryable<Setting>, IQueryable<Setting>>?>()))
			.ReturnsAsync(new Setting{Value= "[{\"Name\":\"test\",\"Types\":[{\"Name\":\"tt\",\"SettlementIntervalType\":\"Weekly\",\"Trans\": {\"En\": \"test\",\"Zh\":\"測試\"}}]}]" });

		_ = fakePromotionDistributionRepository
			.Setup(x => x.GetPromotionDistributionQueries(It.IsAny<Func<IQueryable<PromotionDistribute>, IQueryable<PromotionDistribute>>?>(), default))
			.Returns(promotionDistributes);

		_ = fakePromotionBonusRepository
			.Setup(x => x.GetPromotionBonusPlayersQueries(It.IsAny<Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>?>(), default))
			.Returns(promotionPlayers);

		var sut = new GetPromotionDispatchQueryService(
			fakePromotionBonusRepository.Object,
			fakePromotionDistributionRepository.Object,
			fakePlayerRepository.Object,
			fakeSettingsRepository.Object);

		var query = new GetPromotionDispatchesQuery
		{
			BrandId = brandId
		};

		var response = await sut.GetAsync(query, default);

		Assert.IsTrue(response.Valid);
	}


	private static Fixture CreatePromotionFixture()
	{
		var fixture = new Fixture();

		fixture.Customize<PromotionRow>(u => u
			.With(s => s.PromotionGameProviders,
				Array.Empty<PromotionGameProvider>())
			.With(s => s.PromotionVipLevels,
				Array.Empty<PromotionVipLevel>())
			.With(s => s.PromotionParticipants,
				Array.Empty<PromotionParticipant>())
			.With(s => s.Localization, "{}")
			.With(s => s.Status, (short)PromotionStatus.Enabled)
			.With(s => s.PromotionBonus, Array.Empty<PromotionBonus>())
			.With(s => s.TransactionPromotions, Array.Empty<TransactionPromotion>())
			.With(s => s.VipLevel, new VipLevel { }));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		return fixture;
	}

	private static Fixture CreatePromotionPlayerFixture()
	{
		var fixture = new Fixture();

		fixture.Customize<PromotionPlayer>(u => u
			.With(s => s.PlayerCodeNavigation, CreatePlayerFixture().Create<Player>())
			.With(s => s.PromotionBonus, new PromotionBonus())
			.With(s => s.SentOn, (DateTime?)null));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		return fixture;
	}

	private static Fixture CreatePromotionParticipantFixture()
	{
		var fixture = new Fixture();

		fixture.Customize<PromotionParticipant>(u => u
			.With(s => s.PlayerCodeNavigation, CreatePlayerFixture().Create<Player>())
			.With(s => s.Promotion, new PromotionRow{ PromotionType = "test"}));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		return fixture;
	}

	private static Fixture CreatePlayerFixture()
	{
		var fixture = new Fixture();

		fixture.Customize<Player>(u => u
			.With(s => s.Birthdate, new DateOnly()));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		return fixture;
	}

	private static Fixture CreatePromotionDistributionFixture()
	{
		var fixture = new Fixture();

		fixture.Customize<PromotionDistribute>(u => u
			.With(s => s.Promotion, CreatePromotionFixture().Create<PromotionRow>()));
		fixture.Behaviors.Add(new OmitOnRecursionBehavior());

		return fixture;
	}
}
