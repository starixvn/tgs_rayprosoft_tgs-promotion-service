using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionHistoryTests
{
	[TestMethod]
	public async Task 以PromotionId查詢Events()
	{
		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionHistoryClient(httpClient);

		var promotionId = Guid.Parse("01855bd3-60b5-0000-0001-07e620dd98d5");

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		var currentTime = DateTime.UtcNow;

		await dbContext.PromotionEventSources
			.AddRangeAsync(new[]
			{
				new PromotionEventSource
				{
					Id = Guid.Parse("01855bd3-60b3-0000-0001-07e620dd98d5"),
					PromotionId = promotionId,
					Action = PromotionEventAction.CreateMainInfo.ToString(),
					ChangeSets = $$"""
	 [
	   {
	     "op": "add",
	     "path": "",
	     "value": {
	       "id": "01855bd3-60b5-0000-0001-07e620dd98d5",
	       "to": "{{currentTime.AddDays(2)}}",
	       "from": "{{currentTime.AddDays(1)}}",
	       "name": "TestPromotion",
	       "status": "Disabled",
	       "brandId": "4e86719f-243f-46f6-b010-5c758b7285af",
	       "createdOn": "",
	       "unlimited": 0,
	       "updatedOn": "{{currentTime}}",
	       "amountUsed": 0,
	       "percentage": 0,
	       "displayOrder": 1,
	       "promotionType": "DepositReward",
	       "campaignDetail": {},
	       "snapshotEventId": "01855bd3-60b3-0000-0001-07e620dd98d5",
	       "bonusAmountLimit": 0,
	       "totalAmountLimit": 1,
	       "turnoverMultiple": 0,
	       "createdByUsername": "Modifier1",
	       "autoSentPlayerBonus": true
	     }
	   }
	 ]
	 """,
					ModifyUser = "Modifier1",
					CreatedOn = currentTime
				},
				new PromotionEventSource
				{
					Id = Guid.Parse("01855c4a-b9e6-0000-0001-07e620dd98d5"),
					PromotionId = promotionId,
					Action = PromotionEventAction.ChangeStatus.ToString(),
					ChangeSets = $$"""
	 [
	   {
	     "op": "replace",
	     "path": "/updatedByUsername",
	     "value": "Modifier1"
	   },
	   {
	     "op": "replace",
	     "path": "/updatedOn",
	     "value": "{{currentTime.AddMinutes(1)}}"
	   },
	   {
	     "op": "replace",
	     "path": "/snapshotEventId",
	     "value": "01855c4a-b9e6-0000-0001-07e620dd98d5"
	   }
	 ]
	 """,
					ModifyUser = "Modifier1",
					CreatedOn = currentTime.AddMinutes(1)
				}
			});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.QueryPromotionEventsAsync(promotionId).ToArrayAsync();

		Assert.IsNotNull(actual);
		Assert.AreEqual(2, actual.Length);
	}

	[TestMethod]
	public async Task 查詢Event的Detail()
	{
		var jsonSerializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		await using var web = new TestPromotionApplication();

		var httpClient = web.CreateClient();

		var sut = new PromotionHistoryClient(httpClient);

		var promotionId = Guid.NewGuid();
		var eventId = Guid.NewGuid();

		var currentTime = DateTime.UtcNow;
		var changeSets = """[{"op": "replace", "path": "/status", "value": "Disabled"}]""";

		var dbContext = web.Services.GetRequiredService<TgsContext>();
		
		_ = await dbContext.PromotionEventSources.AddAsync(new PromotionEventSource
		{
			Id = eventId,
			PromotionId = promotionId,
			Action = PromotionEventAction.ChangeStatus.ToString(),
			ChangeSets = JsonSerializer.Serialize(changeSets, jsonSerializerOptions),
			CreatedOn = currentTime,
			ModifyUser = "Modifier1"
		});
		
		_ = await dbContext.SaveChangesAsync();

		var expected = new PromotionEventDetailInfo
		{
			EventId = eventId,
			PromotionId = promotionId,
			Action = Clients.PromotionEventAction.ChangeStatus,
			ChangeSets = JsonDocument.Parse(changeSets),
			CreatedOn = currentTime,
			Modifier = "Modifier1"
		};

		var actual = await sut.GetPromotionEventDetailAsync(
			promotionId, eventId);

		Assert.IsNotNull(actual);
		Assert.AreEqual(expected.EventId, actual.EventId);
		Assert.AreEqual(expected.Action, actual.Action);
	}
}
