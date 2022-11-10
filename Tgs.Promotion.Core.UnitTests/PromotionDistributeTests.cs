using System.Collections.Immutable;
using NSubstitute;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Domain.Entities;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class PromotionDistributeTests
{
	[TestMethod]
	public async Task Distribute_Bonus_By_Plan_And_Player()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 "manager");

		var walletId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(command.PlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = command.PlayerCode,
				BrandId = Guid.NewGuid()
			});

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(command.PlayerCode))
			.Returns(new Dictionary<string, string>
			{
				["DefaultWalletId"] = walletId.ToString()
			}.ToImmutableDictionary());

		_ = fakePromotionRepository.FindPromotionDistributePlanAsync(Arg.Is(command.PlanId))
			.Returns(new PromotionDistributePlanInfo(
				Guid.NewGuid(),
				Guid.NewGuid(),
				Guid.NewGuid(),
				0));

		_ = fakeServiceCoordinator.GetAsync<GetPromotionMainInfoQuery, PromotionMainInfo?>(Arg.Any<GetPromotionMainInfoQuery>())
			.Returns(new PromotionMainInfo(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"Ttt",
				(From: DateTime.Now.AddDays(-1D), To: DateTime.Now.AddDays(1D)),
				"Test",
				null,
				true));

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionDistributeAsync(
				Arg.Any<PromotionDistributeInfo>(),
				Arg.Is(command.Distributor));

		_ = fakePlayerBucketClient.Received(1)
			.CreateBucketAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.NeedTurnover),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>());

		_ = fakeCashServiceClient.Received(1)
			.PromotionAsync(
				Arg.Is(walletId),
				Arg.Is(command.Amount),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Is(default(Guid?)));
	}

	[TestMethod]
	public async Task Distribute_Bonus_Without_Find_Player_Will_Throw_Exception()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketClient = Substitute.For<IPlayerBucketClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 "manager");

		_ = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
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

		_ = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => await sut.GetAsync(command));
	}
}
