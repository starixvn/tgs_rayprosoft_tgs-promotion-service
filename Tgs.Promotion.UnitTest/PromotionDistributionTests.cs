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

		_ = fakePromotionRepository.ReceivedWithAnyArgs().UpdatePromotionDistributeAsync(default, default);

		_ = fakeCashServiceClient.ReceivedWithAnyArgs(1)
			.PromotionAsync(default, 0, null, null, null);
	}
}