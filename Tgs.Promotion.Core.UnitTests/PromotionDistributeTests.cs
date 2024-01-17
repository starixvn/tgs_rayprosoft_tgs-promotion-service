using System.Collections.Immutable;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
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
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator
			);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 null,
			 "manager",
			 Array.Empty<string>());

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

		_ = fakePlayerBucketForTypesClient.Received(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.NeedTurnover),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());

		_ = fakeCashServiceClient.Received(1)
			.PromotionAsync(
				Arg.Is(walletId),
				Arg.Is(command.Amount),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Is(default(Guid?)));
	}

	[TestMethod]
	public async Task Distribute_Bonus_By_Plan_And_Player_With_ReferenceId()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 Guid.NewGuid(),
			 "manager",
			 Array.Empty<string>());

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
				Arg.Is<PromotionDistributeInfo>(
					x =>
						x.PlanId == command.PlanId
						&& x.PlayerCode == command.PlayerCode
						&& x.ReferenceId == command.ReferenceId),
				Arg.Is(command.Distributor));

		_ = fakePlayerBucketForTypesClient.Received(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Is(command.PlayerCode),
				Arg.Is(command.NeedTurnover),
				Arg.Is(BucketType.Promotion),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());

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
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 null,
			 "manager",
			 Array.Empty<string>());

		_ = await Assert.ThrowsExceptionAsync<InvalidOperationException>(
			async () => await sut.GetAsync(command));
	}

	[TestMethod]
	public async Task Distribute_Log_Will_Add_SentOn_After_Distributed()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 null,
			 "manager",
			 Array.Empty<string>());

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

		_ = fakePromotionRepository
			.Received(1)
			.InsertPromotionDistributeLogAsync(
				Arg.Is<PromotionDistributeLog>(x =>
					x.Action == DistributeAction.Distribute &&
					x.ChangeSets.RootElement.EnumerateArray().Single().GetProperty("op").GetString() == "add" &&
					x.ChangeSets.RootElement.EnumerateArray().Single().GetProperty("path").GetString() == "/sentOn"));
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

	[TestMethod]
	public async Task 發放活動恵時如果遇到流水建立的短暫性失敗依然可以發放成功()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 null,
			 "manager",
			 Array.Empty<string>());

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

		_ = fakeServiceCoordinator.GetAsync<GetPromotionMainInfoQuery, PromotionMainInfo?>(
				Arg.Any<GetPromotionMainInfoQuery>())
			.Returns(new PromotionMainInfo(
				Guid.NewGuid(),
				Guid.NewGuid(),
				"Ttt",
				(From: DateTime.Now.AddDays(-1D), To: DateTime.Now.AddDays(1D)),
				"Test",
				null,
				true));

		var distributeId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync()
			.Returns(distributeId, Guid.NewGuid(), Guid.NewGuid());

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionDistributeAsync(
				Arg.Is(distributeId),
				Arg.Any<DateTime>(),
				Arg.Any<string>());
	}

	[TestMethod]
	public async Task 發放活動優惠如果是自動發放會執行後面發放的程序()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeServiceCoordinator = Substitute.For<IServiceCoordinator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var sut = new DistributePromotionBonusCommandService(
			fakePromotionRepository,
			fakeServiceCoordinator,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator);

		var command = new DistributePromotionBonusCommand(
			 Guid.NewGuid(),
			 "player1",
			 100M,
			 1000M,
			 null,
			 "manager",
			 Array.Empty<string>());

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

		var distributeId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync()
			.Returns(distributeId, Guid.NewGuid(), Guid.NewGuid());

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionDistributeAsync(
				Arg.Is(distributeId),
				Arg.Any<DateTime>(),
				Arg.Any<string>());
	}
}
