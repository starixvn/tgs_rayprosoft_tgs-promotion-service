using System;
using System.Linq;
using System.Threading.Tasks;
using MockQueryable.Moq;
using NSubstitute;
using NUnit.Framework;
using Tgs.Promotion.ApplicationServices.Promotion;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models.PromotionBonus;

namespace Tgs.Promotion.UnitTest;

[TestFixture]
public class PromotionReliefMoneyWeeklyDistributionTests
{
	[Test]
	public async Task Can_Distribute_Weekly_Promotions()
	{
		var promotionRepositoryMock = Substitute.For<IPromotionRepository>();
		var funcDistributionMock = Substitute.For<Func<string, IDistribution>>();

		var promotionsQueryHandlerMock =
			new CreatePromotionBonusWeeklyHandler(
				promotionRepositoryMock,
				funcDistributionMock);

		var brandId = Guid.NewGuid();
		var playerCode = "player1";
		var promotionId = Guid.NewGuid();
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		var promotions = new PromotionRow[]
			{
				new PromotionRow
				{
					Id = promotionId,
					BrandId = brandId,
					From = previousMonday.DateTime,
					To = previousSunday.DateTime,
					SettlementIntervalType = "Weekly",
					PromotionType = "ReliefMoneyWeekly",
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
							CreatedOn = previousMonday.DateTime
						}
					}
				}
			}
			.AsQueryable()
			.BuildMock();

		_ = promotionRepositoryMock.GetPromotionQueries().Returns(promotions);

		var request = new CreatePromotionBonusWeeklyRequest();

		_ = await promotionsQueryHandlerMock.GetAsync(request, default);

		_ = funcDistributionMock.Received(1)
			.Invoke(Arg.Is("ReliefMoneyWeekly"));
	}
}
