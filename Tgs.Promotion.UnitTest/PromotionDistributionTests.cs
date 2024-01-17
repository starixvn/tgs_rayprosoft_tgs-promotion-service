using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Domain.Entities;
using Tgs.SerialNumberGenerator;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionDistributionTests
{
	[Test]
	public async Task Can_Send_Player_Bucket_By_Manual()
	{
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionDistributionRepository = Substitute.For<IPromotionDistributionRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo { PlayerCode = playerCode, BrandId = Guid.NewGuid() });

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode))
			.Returns(new Dictionary<string, string> { ["DefaultWalletId"] = Guid.NewGuid().ToString() }
				.ToImmutableDictionary());

		var promotionDistributeId = Guid.NewGuid();
		var planId = Guid.NewGuid();

		var promotionDistributes = new PromotionDistribute[]
			{
				new()
				{
					Id = promotionDistributeId,
					PromotionId = promotionId,
					PlanId = planId,
					PlayerCode = playerCode,
					BrandId = Guid.NewGuid(),
					BonusAmount = 100M,
					SentOn = null,
					Bucket = 100M
				}
			}.AsQueryable()
			.BuildMock();

		_ = fakePromotionDistributionRepository.GetPromotionDistributionQueries().Returns(promotionDistributes);

		_ = fakePromotionRepository.FindPromotionDistributePlanDetailAsync(Arg.Any<Guid>())
			.Returns(new PromotionDistributePlanDetailInfo(planId, """
			                                                       {
			                                                         "id": "018c5d64-4f60-0000-0002-07e753f7573f",
			                                                         "to": "2023-12-23T16:00:00Z",
			                                                         "from": "2023-12-17T16:00:00Z",
			                                                         "name": "testvi-每日救援金",
			                                                         "number": 1,
			                                                         "brandId": "4e86719f-243f-46f6-b010-5c758b7285af",
			                                                         "createdOn": "2023-12-12T09:38:24.224606Z",
			                                                         "updatedOn": "2023-12-12T09:38:24.254715Z",
			                                                         "vipLevels": [
			                                                           1
			                                                         ],
			                                                         "percentage": 1,
			                                                         "subSubject": "test",
			                                                         "localization": "{}",
			                                                         "promotionType": "ReliefMoneyDaily",
			                                                         "campaignDetail": "{\"percentage\": 1, \"bonusAmountLimit\": 1, \"turnoverMultiple\": 1}",
			                                                         "gameProviderIds": [],
			                                                         "bonusAmountLimit": 1,
			                                                         "totalAmountLimit": 1000,
			                                                         "turnoverMultiple": 1,
			                                                         "createdByUsername": "admin.enabled",
			                                                         "updatedByUsername": "admin.enabled",
			                                                         "autoSentPlayerBonus": true
			                                                       }
			                                                       """));

		var sut = new DistributePromotionBonusManuallyCommandService(fakePromotionRepository,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator,
			fakePromotionDistributionRepository,
			NullLogger<DistributePromotionBonusManuallyCommandService>.Instance);

		var command = new DistributePromotionBonusManuallyCommand(promotionDistributeId,
			playerCode,
			null);

		await sut.ExecuteAsync(command);

		_ = fakePlayerBucketForTypesClient.ReceivedWithAnyArgs(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<BucketType>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.ReceivedWithAnyArgs().UpdatePromotionDistributeAsync(default, default, default);

		_ = fakeCashServiceClient.ReceivedWithAnyArgs(1)
			.PromotionAsync(default, 0, null, null, null);
	}

	[Test]
	public async Task Can_Send_Promotion_Distribution_By_Manual()
	{
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakePromotionDistributionRepository = Substitute.For<IPromotionDistributionRepository>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo { PlayerCode = playerCode, BrandId = Guid.NewGuid() });

		_ = fakePlayerServiceClient.GetPlayerPropertiesAsync(Arg.Is(playerCode))
			.Returns(new Dictionary<string, string> { ["DefaultWalletId"] = Guid.NewGuid().ToString() }
				.ToImmutableDictionary());

		var promotionDistributeId = Guid.NewGuid();
		var planId = Guid.NewGuid();

		var promotionDistributes = new PromotionDistribute[]
			{
				new()
				{
					Id = promotionDistributeId,
					PromotionId = promotionId,
					PlanId = planId,
					PlayerCode = playerCode,
					BrandId = Guid.NewGuid(),
					BonusAmount = 100M,
					SentOn = null,
					Bucket = 100M
				}
			}.AsQueryable()
			.BuildMock();

		_ = fakePromotionDistributionRepository.GetPromotionDistributionQueries().Returns(promotionDistributes);

		_ = fakePromotionRepository.FindPromotionDistributePlanDetailAsync(Arg.Any<Guid>())
			.Returns(new PromotionDistributePlanDetailInfo(planId, null));

		var sut = new DistributePromotionBonusManuallyCommandService(fakePromotionRepository,
			fakeCashServiceClient,
			fakePlayerServiceClient,
			fakePlayerBucketForTypesClient,
			fakeSerialNumberGenerator,
			fakePromotionDistributionRepository,
			NullLogger<DistributePromotionBonusManuallyCommandService>.Instance);

		var command = new DistributePromotionBonusManuallyCommand(promotionDistributeId,
			playerCode,
			null);

		await sut.ExecuteAsync(command);

		_ = fakePlayerBucketForTypesClient.ReceivedWithAnyArgs(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<BucketType>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.ReceivedWithAnyArgs().UpdatePromotionDistributeAsync(default, default, default);

		_ = fakeCashServiceClient.ReceivedWithAnyArgs(1)
			.PromotionAsync(default, 0, null, null, null);
	}
}
