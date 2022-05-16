using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using Tgs.Promotion.Core;
using Tgs.Promotion.Core.Services;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.UnitTest
{
	[TestFixture]
	public class PromotionCheckerTests
	{
		[Test]
		public async Task FirstDeposit_Player_FirstDepositOn_Is_Null_Should_Return_True()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

			fakePlayerService
				.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
				.Returns(new Player
				{
					FirstDepositOn = null
				});

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task FirstDeposit_Should_Check_Well()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

			fakePlayerService
				.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
				.Returns(new Player
				{
					FirstDepositOn = DateTime.UtcNow
				});

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsFalse(actual);
		}

		[Test]
		public async Task FirstPromotion_Should_Check_Well()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			fakePlayerService
				.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
				.Returns(new Player
				{
					FirstDepositOn = DateTime.UtcNow,
					FirstPromotionOn = DateTime.UtcNow
				});

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsFalse(actual);
		}

		[Test]
		public async Task Player_DailyFirstDeposit_Checker()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailyFirstDepositChecker(fakePlayerDataService);

			fakePlayerDataService
				.CheckPlayerIsDailyFirstDepositAsync(Arg.Any<string>())
				.Returns(true);

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task Player_DailyFirstDeposit_Without_PromotionId_Not_Rollback_Tag()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailyFirstDepositChecker(fakePlayerDataService);

			await sut.RollbackTagAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataService.Received(0)
				.RemovePlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_DailyFirstDeposit_With_PromotionId_Rollback_Tag()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailyFirstDepositChecker(fakePlayerDataService);

			await sut.RollbackTagAsync("ThisIsPlayerCode", Guid.NewGuid());

			_ = fakePlayerDataService.Received(1)
				.RemovePlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_DailyFirstDeposit_With_PromotionId_Be_Tag()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailyFirstDepositChecker(fakePlayerDataService);

			await sut.TagPromotionTypeAsync("ThisIsPlayerCode", Guid.NewGuid());

			_ = fakePlayerDataService.Received(1)
				.SetPlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_DailyFirstDeposit_Without_PromotionId_Not_Tag()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailyFirstDepositChecker(fakePlayerDataService);

			await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataService.Received(0)
				.SetPlayerDailyDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_DailySecondDeposit_Checker()
		{
			var fakePlayerDataService = Substitute.For<IPlayerDataRepository>();

			var sut = new DailySecondDepositChecker(fakePlayerDataService);

			fakePlayerDataService
				.CheckPlayerIsDailyFirstDepositAsync(Arg.Any<string>())
				.Returns(false);

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task Player_FirstDeposit_Always_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataRepository.Received(1)
				.SetPlayerFirstDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_FirstDeposit_Checker()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

			fakePlayerService
				.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
				.Returns(new Player { FirstDepositOn = null });

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task Player_FirstDeposit_Commit_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

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

			var sut = new FirstDepositChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.RollbackTagAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataRepository.Received(1)
				.RemovePlayerFirstDepositFlagAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_FirstJoinPromotion_With_PromotionId_Be_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.TagPromotionTypeAsync("ThisIsPlayerCode", Guid.NewGuid());

			_ = fakePlayerDataRepository.Received(1)
				.SetPlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_FirstJoinPromotion_Without_PromotionId_Be_Not_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.TagPromotionTypeAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataRepository.Received(0)
				.SetPlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_FirstPromotion_Checker()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			fakePlayerService
				.GetPlayerByPlayerCodeAsync(Arg.Any<string>())
				.Returns(new Player { FirstPromotionOn = null });

			var actual = await sut.CheckAsync("ThisIsPlayerCode", null);

			Assert.IsTrue(actual);
		}

		[Test]
		public async Task Player_FirstPromotion_With_PromotionId_Commit_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

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

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.RollbackTagAsync("ThisIsPlayerCode", Guid.NewGuid());

			_ = fakePlayerDataRepository.Received(1)
				.RemovePlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Player_FirstPromotion_Without_PromotionId_Not_Commit_Tag()
		{
			var fakePlayerService = Substitute.For<IPlayerRepository>();
			var fakePlayerDataRepository = Substitute.For<IPlayerDataRepository>();

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

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

			var sut = new FirstJoinPromotionChecker(fakePlayerService, fakePlayerDataRepository);

			await sut.RollbackTagAsync("ThisIsPlayerCode", null);

			_ = fakePlayerDataRepository.Received(0)
				.RemovePlayerFirstJoinPromotionAsync(Arg.Is("ThisIsPlayerCode"));
		}

		[Test]
		public async Task Tag_Player_Deposit()
		{
			var fakeServiceProvider = Substitute.For<IServiceProvider>();
			var fakePromotionChecker = Substitute.For<IPromotionChecker>();

			fakeServiceProvider.GetService(Arg.Any<Type>())
				.Returns(fakePromotionChecker);

			var fakeCheckFactory = new PromotionCheckerFactory(
				new Dictionary<string, Type>
				{
					["FirstDeposit"] = typeof(object),
					["AccountFirstDeposit"] = typeof(object),
					["DailyFirstDeposit"] = typeof(object),
					["DailySecondDeposit"] = typeof(object),
					["TurnoverRebate"] = typeof(object),
					["ReliefMoneyDaily"] = typeof(object),
					["ReliefMoneyWeekly"] = typeof(object),
					["ReliefMoneyMonthly"] = typeof(object),
				},
				fakeServiceProvider);

			var sut = new TagPlayerDepositCommandService(fakeCheckFactory);

			var command = new TagPlayerDepositCommand("ThisIsPlayerCode", null);

			await sut.ExecuteAsync(command);

			_ = fakePromotionChecker.Received(3)
				.TagPromotionTypeAsync(Arg.Any<string>(), Arg.Any<Guid?>());
		}
	}
}
