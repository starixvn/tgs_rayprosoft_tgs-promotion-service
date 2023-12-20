using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.CashService.Client;
using Tgs.PlayerBucket.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Domain.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionDispatchTests
{
	[Test]
	public async Task Can_Send_Promotion_Dispatch_By_Manual()
	{
		var fakePlayerBucketForTypesClient = Substitute.For<IPlayerBucketForTypesClient>();
		var fakeCashServiceClient = Substitute.For<ICashServiceClient>();
		var fakeTransactionRepository = Substitute.For<ITransactionRepository>();
		var fakePromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();

		var playerCode = "playerCode";

		var promotionBonusId = Guid.NewGuid();

		var bonus = new PromotionPlayer[]
			{
				new()
				{
					PromotionBonusId = promotionBonusId,
					PlayerCode = playerCode,
					BucketAmountByManual = 100M,
					BonusAmount = 100M,
					SentOn = null,
					PlayerCodeNavigation = new Player(),
					PromotionBonus = new PromotionBonus
					{
						Detail = """
			                                                       {
			                                                         "id": "018c80c0-2f26-0000-0002-07e7597d893c",
			                                                         "to": "2023-12-30T16:00:00Z",
			                                                         "from": "2023-12-18T16:00:00Z",
			                                                         "name": "jeffersontest",
			                                                         "status": 0,
			                                                         "brandId": "4e86719f-243f-46f6-b010-5c758b7285af",
			                                                         "unlimited": true,
			                                                         "vipLevels": [
			                                                           5,
			                                                           1,
			                                                           4,
			                                                           2,
			                                                           3
			                                                         ],
			                                                         "amountUsed": 300,
			                                                         "percentage": 100,
			                                                         "promotionType": "DepositReward",
			                                                         "campaignDetail": "{\"percentage\": 100, \"bucketTypes\": [\"\"], \"bonusAmountLimit\": 100, \"turnoverMultiple\": 10, \"depositMinimumLimit\": 1}",
			                                                         "bonusAmountLimit": 100,
			                                                         "totalAmountLimit": 0,
			                                                         "turnoverMultiple": 10,
			                                                         "autoSentPlayerBonus": false
			                                                       }
			                                                       """
					}
				}
			}.AsQueryable()
			.BuildMock();

		_ = fakePromotionBonusRepository.GetPromotionBonusPlayersQueries().Returns(bonus);

		_ = fakeCashServiceClient.PromotionAsync(Arg.Any<Guid>(),
				Arg.Any<decimal>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<Guid?>(),
				Arg.Any<CancellationToken>())
			.Returns(new TransactionResponse { TransactionId = Guid.NewGuid() });

		var sut = new DispatchPromotionBonusQueryService(fakePromotionBonusRepository,
			fakeCashServiceClient,
			fakeTransactionRepository,
			fakePlayerBucketForTypesClient);

		var command = new DispatchPromotionBonusQuery
		{
			OperatorUserId = Guid.NewGuid(),
			Players = [
				new() { PlayerCode = playerCode, PromotionBonusId = promotionBonusId }
			]
		};

		_ = await sut.GetAsync(command);

		_ = fakePromotionBonusRepository.ReceivedWithAnyArgs(1)
			.UpdatePromotionBonusPlayersAsync(null!);

		_ = fakePlayerBucketForTypesClient.ReceivedWithAnyArgs(1)
			.CreateBucketForBucketTypesAsync(
				Arg.Any<string>(),
				Arg.Any<decimal>(),
				Arg.Any<BucketType>(),
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<IEnumerable<string>?>(),
				Arg.Any<CancellationToken>());

		_ = fakeCashServiceClient.ReceivedWithAnyArgs(1)
			.PromotionAsync(default, 0, null, null, null);
	}
}