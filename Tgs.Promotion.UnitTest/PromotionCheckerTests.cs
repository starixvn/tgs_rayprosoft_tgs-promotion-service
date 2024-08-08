using System;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Tgs.PlayerService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Checkers;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionCheckerTests
{
	[TestCase(100, true)]
	[TestCase(99, false)]
	public async Task Deposit_Reward_Checker_Return_True_When_Player_Deposit_Amount_More_Then_Setting(decimal amount, bool response)
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionSerializer = Substitute.For<PromotionSerializer>();

		var sut = new DepositRewardChecker(
			"Register",
			fakePromotionSerializer,
			fakePromotionRepository);

		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var playerCode = "test1";

		var promotion = new PromotionMainInfo(
			promotionId,
			brandId,
			"Test Promotion",
			(DateTime.Parse("2022/06/01 17:00:00+00"), DateTime.Parse("2022/06/05 17:00:00+00")),
			"Register",
			null,
			false);

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(Arg.Is(promotionId))
			.Returns(new PromotionSnapshot(
				promotionId,
				"test",
				brandId,
				DateTime.Now,
				DateTime.Now,
				PromotionStatus.Enabled,
				"aaa",
				null,
				false,
				0M,
				1,
				null,
				0M,
				0M,
				0M,
				JsonDocument.Parse(@"{""depositMinimumLimit"":100,""percentage"":10,""bonusAmount"":100,""turnoverMultiple"":10}"),
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				DateTime.Now,
				null,
				DateTime.Now,
				0M,
				null,
				0,
				null,
				null));

		var actual = await sut.CheckAsync(
			playerCode,
			promotion,
			JsonDocument.Parse($$"""{"depositAmount": {{amount}} }"""));

		ClassicAssert.IsTrue(actual == response);
	}

	[Test]
	public async Task FirstDeposit_Player_FirstDepositOn_Is_Null_Should_Return_True()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker("FirstDeposit", fakePlayerService, fakePlayerDataRepository);

		_ = fakePlayerService
			.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
			.Returns(new Player
			{
				FirstDepositOn = null
			});

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task FirstDeposit_Should_Check_Well()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		_ = fakePlayerService
			.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
			.Returns(new Player
			{
				FirstDepositOn = DateTime.UtcNow
			});

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task FirstPromotion_Should_Check_Well()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		_ = fakePlayerService
			.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
			.Returns(new Player
			{
				FirstDepositOn = DateTime.UtcNow,
				FirstPromotionOn = DateTime.UtcNow
			});

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Player_DailyFirstDeposit_Checker()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailyFirstDepositChecker("DailyFirstDeposit", fakePlayerDataService);

		_ = fakePlayerDataService
			.CheckPlayerIsDailyFirstDepositAsync(Arg.Any<string>())
			.Returns(true);

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Player_DailyFirstDeposit_With_PromotionId_Be_Tag()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailyFirstDepositChecker("DailyFirstDeposit", fakePlayerDataService);

		await sut.TagPromotionTypeAsync("ThisIsPlayerCode", Guid.NewGuid());

		_ = fakePlayerDataService.Received(1)
			.SetPlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_DailyFirstDeposit_With_PromotionId_Rollback_Tag()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailyFirstDepositChecker("DailyFirstDeposit", fakePlayerDataService);

		await sut.RollbackTagAsync("ThisIsPlayerCode", Guid.NewGuid());

		_ = fakePlayerDataService.Received(1)
			.RemovePlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_DailyFirstDeposit_Without_PromotionId_Not_Rollback_Tag()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailyFirstDepositChecker("DailyFirstDeposit", fakePlayerDataService);

		await sut.RollbackTagAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataService.Received(0)
			.RemovePlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_DailyFirstDeposit_Without_PromotionId_Not_Tag()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailyFirstDepositChecker("DailyFirstDeposit", fakePlayerDataService);

		await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataService.Received(0)
			.SetPlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_DailySecondDeposit_Checker()
	{
		var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

		var sut = new DailySecondDepositChecker("DailySecondDeposit", fakePlayerDataService);

		_ = fakePlayerDataService
			.CheckPlayerIsDailyFirstDepositAsync(Arg.Any<string>())
			.Returns(false);

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Player_FirstDeposit_Always_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataRepository.Received(1)
			.SetPlayerFirstDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_FirstDeposit_Checker()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		_ = fakePlayerService
			.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
			.Returns(new Player { FirstDepositOn = null });

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Player_FirstDeposit_Commit_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		var playerCode = "ThisIsPlayerCode";

		await sut.CommitTagAsync(playerCode, null);

		_ = fakePlayerDataRepository.Received(1)
			.RemovePlayerFirstDepositFlagAsync(Arg.Is(playerCode));
	}

	[Test]
	public async Task Player_FirstDeposit_Rollback_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstDepositChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.RollbackTagAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataRepository.Received(1)
			.RemovePlayerFirstDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_FirstJoinPromotion_With_PromotionId_Be_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.TagPromotionTypeAsync("ThisIsPlayerCode", Guid.NewGuid());

		_ = fakePlayerDataRepository.Received(1)
			.SetPlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_FirstJoinPromotion_Without_PromotionId_Be_Not_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataRepository.Received(0)
			.SetPlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_FirstPromotion_Checker()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		_ = fakePlayerService
			.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
			.Returns(new Player { FirstPromotionOn = null });

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Now, DateTime.Now.AddDays(1)),
			"FirstDeposit",
			null,
			false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Player_FirstPromotion_With_PromotionId_Commit_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		var playerCode = "ThisIsPlayerCode";

		await sut.CommitTagAsync(playerCode, Guid.NewGuid());

		_ = fakePlayerDataRepository.Received(1)
			.RemovePlayerFirstJoinPromotionAsync(Arg.Is(playerCode));
		_ = fakePlayerService.Received(1)
			.SetFirstPromotionTimeAsync(Arg.Is(playerCode), Arg.Any<DateTimeOffset>());
	}

	[Test]
	public async Task Player_FirstPromotion_With_PromotionId_Rollback_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.RollbackTagAsync("ThisIsPlayerCode", Guid.NewGuid());

		_ = fakePlayerDataRepository.Received(1)
			.RemovePlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Player_FirstPromotion_Without_PromotionId_Not_Commit_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		var playerCode = "ThisIsPlayerCode";

		await sut.CommitTagAsync(playerCode, null);

		_ = fakePlayerDataRepository.Received(0)
			.RemovePlayerFirstJoinPromotionAsync(Arg.Is(playerCode));
		_ = fakePlayerService.Received(0)
			.SetFirstPromotionTimeAsync(Arg.Is(playerCode), Arg.Any<DateTimeOffset>());
	}

	[Test]
	public async Task Player_FirstPromotion_Without_PromotionId_Not_Rollback_Tag()
	{
		var fakePlayerService = Substitute.For<IPlayerRepository>();
		var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

		var sut = new FirstJoinPromotionChecker(
			"FirstDeposit",
			fakePlayerService,
			fakePlayerDataRepository);

		await sut.RollbackTagAsync("ThisIsPlayerCode", null);

		_ = fakePlayerDataRepository.Received(0)
			.RemovePlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
	}

	[Test]
	public async Task Register_Checker_Return_False_When_Joined_The_Promotion()
	{
		var fakePlayerService = Substitute.For<IPlayerServiceClient>();
		var fakePlayerDataRepository = Substitute.For<IPromotionParticipantRepository>();

		var sut = new RegisterChecker(
			"Register",
			fakePlayerService,
			fakePlayerDataRepository);

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Parse("2022/06/01 17:00:00+00"), DateTime.Parse("2022/06/05 17:00:00+00")),
			"Register",
			null,
			false);

		_ = fakePlayerService.GetPlayerInfoAsync(Arg.Any<string>())
			.Returns(new PlayerInfo
			{
				RegisteredOn = DateTime.Parse("2022/06/03 16:01:00")
			});

		_ = fakePlayerDataRepository
			.ExistsAsync(Arg.Any<Expression<Func<PromotionParticipant, bool>>>())
			.Returns(true);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Register_Checker_Return_False_When_Player_RegisterOn_Is_Not_During_The_Promotion()
	{
		var fakePlayerService = Substitute.For<IPlayerServiceClient>();
		var fakePlayerDataRepository = Substitute.For<IPromotionParticipantRepository>();

		var sut = new RegisterChecker("Register", fakePlayerService, fakePlayerDataRepository);

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Parse("2022/06/01 17:00:00+00"), DateTime.Parse("2022/06/05 17:00:00+00")),
			"Register",
			null,
			false);

		_ = fakePlayerService
			.GetPlayerInfoAsync(Arg.Any<string>())
			.Returns(new PlayerInfo
			{
				RegisteredOn = DateTime.Parse("2022/06/01 16:01:00")
			});

		_ = fakePlayerDataRepository
			.ExistsAsync(Arg.Any<Expression<Func<PromotionParticipant, bool>>>())
			.Returns(false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsFalse(actual);
	}

	[Test]
	public async Task Register_Checker_Return_True_When_Player_RegisterOn_During_The_Promotion()
	{
		var fakePlayerService = Substitute.For<IPlayerServiceClient>();
		var fakePlayerDataRepository = Substitute.For<IPromotionParticipantRepository>();

		var sut = new RegisterChecker(
			"Register",
			fakePlayerService,
			fakePlayerDataRepository);

		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"Test Promotion",
			(DateTime.Parse("2022/06/01 17:00:00+00"), DateTime.Parse("2022/06/05 17:00:00+00")),
			"Register",
			null,
			false);

		_ = fakePlayerService.GetPlayerInfoAsync(Arg.Any<string>())
			.Returns(new PlayerInfo
			{
				RegisteredOn = DateTime.Parse("2022/06/01 17:01:00+00")
			});

		_ = fakePlayerDataRepository
			.ExistsAsync(Arg.Any<Expression<Func<PromotionParticipant, bool>>>())
			.Returns(false);

		var actual = await sut.CheckAsync("ThisIsPlayerCode", promotion, null);

		ClassicAssert.IsTrue(actual);
	}

	[Test]
	public async Task Tag_Player_Deposit()
	{
		var fakePromotionChecker = Substitute.For<IPromotionChecker>();
		var fakeCheckFactory = Substitute.For<IPromotionCheckerFactory>();

		_ = fakeCheckFactory.GetPromotionChecker(Arg.Any<string>())
			.Returns(fakePromotionChecker);

		var sut = new TagPlayerDepositCommandService(fakeCheckFactory);

		var command = new TagPlayerDepositCommand("ThisIsPlayerCode", null);

		await sut.ExecuteAsync(command);

		_ = fakePromotionChecker.Received(4)
			.TagPromotionTypeAsync(Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[Test]
	public async Task Untag_Player_Deposit()
	{
		var fakePromotionChecker = Substitute.For<IPromotionChecker>();
		var fakeCheckFactory = Substitute.For<IPromotionCheckerFactory>();

		_ = fakeCheckFactory.GetPromotionChecker(Arg.Any<string>())
			.Returns(fakePromotionChecker);

		var sut = new CancelTagPlayerDepositCommandService(fakeCheckFactory);

		var command = new CancelTagPlayerDepositCommand("player1", null);

		await sut.ExecuteAsync(command);

		_ = fakePromotionChecker.Received(3)
			.RollbackTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
	}
}
