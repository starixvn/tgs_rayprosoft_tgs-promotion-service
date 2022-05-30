using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class PromotionDistributeTests
{
	[TestMethod]
	public async Task Distribute_Bonus_By_Plan_And_Player()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketRepository = Substitute.For<IPlayerBucketRepository>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeCashServiceClient,
			fakePlayerRepository,
			fakePlayerBucketRepository);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 "manager");

		var walletId = Guid.NewGuid();
		fakePlayerRepository.GetPlayerByPlayerCodeAsync(Arg.Is(command.PlayerCode))
			.Returns(new Player
			{
				PlayerCode = command.PlayerCode,
				DefaultWalletId = walletId
			});

		var newDistributeId = Guid.NewGuid();
		fakePromotionRepository.CreatePromotionDistributeAsync(
			Arg.Is(command.PlanId),
			Arg.Is(command.PlayerCode),
			Arg.Is(command.Amount),
			Arg.Is(command.NeedTurnover),
			Arg.Is(command.Distributor))
			.Returns(newDistributeId);

		var actual = await sut.GetAsync(command);

		Assert.AreNotEqual(Guid.Empty, actual);

		_ = fakePlayerBucketRepository.Received(1)
			.CreatePlayerBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(BucketType.Promotion),
				  Arg.Is(command.NeedTurnover));
		_ = fakeCashServiceClient.Received(1)
			.PromotionAsync(
				Arg.Is(walletId),
				Arg.Is(command.Amount),
				Arg.Any<string>(),
				Arg.Is(newDistributeId.ToString()),
				Arg.Is(default(Guid?)));
	}

	[TestMethod]
	public async Task Distribute_Bonus_Without_Find_Player_Will_Throw_Exception()
	{
		var fakePlayerRepository = Substitute.For<IPlayerRepository>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketRepostory = Substitute.For<IPlayerBucketRepository>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeCashServiceClient,
			fakePlayerRepository,
			fakePlayerBucketRepostory);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 "manager");

		await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			async () => await sut.GetAsync(command));
	}

	[TestMethod]
	public async Task Get_Promotion_Distribute_Plan()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetOrCreatePromotionDistributePlanCommandService(fakePromotionRepository);

		var command = new GetOrCreatePromotionDistributePlanCommand(
			 Guid.NewGuid(),
			 0L);

		_ = fakePromotionRepository.GetPromotionByIdAsync(Arg.Is(command.PromotionId))
			.Returns(new PromotionRow
			{
				Id = command.PromotionId
			});

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionDistributePlanAsync(
			Arg.Is(command.PromotionId),
			Arg.Is(command.ConditionHash));
	}

	[TestMethod]
	public async Task Get_Promotion_Distribute_Plan_Without_Find_Player_Will_Throw_Exception()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetOrCreatePromotionDistributePlanCommandService(fakePromotionRepository);

		var command = new GetOrCreatePromotionDistributePlanCommand(
			 Guid.NewGuid(),
			 0L);

		await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await sut.GetAsync(command));
	}
}
