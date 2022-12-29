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

		await dbContext.PromotionEventSources
			.AddRangeAsync(new[]
			{
				new PromotionEventSource
				{
					Id = Guid.Parse("01855bd3-60b3-0000-0001-07e620dd98d5"),
					PromotionId = promotionId,
					Action = PromotionEventAction.CreateMainInfo.ToString(),
					ChangeSets = "[{\"op\": \"add\", \"path\": \"\", \"value\": {\"id\": \"01855bd3-60b5-0000-0001-07e620dd98d5\", \"to\": \"2022-12-31T00:00:00Z\", \"from\": \"2022-12-27T00:00:00Z\", \"name\": \"TestPromotion\", \"status\": \"Disabled\", \"brandId\": \"4e86719f-243f-46f6-b010-5c758b7285af\", \"createdOn\": \"2022-12-29T03:00:57.645456Z\", \"unlimited\": 0, \"updatedOn\": \"2022-12-29T03:00:57.645456Z\", \"amountUsed\": 0, \"percentage\": 0, \"displayOrder\": 1, \"promotionType\": \"DepositReward\", \"campaignDetail\": {}, \"snapshotEventId\": \"01855bd3-60b3-0000-0001-07e620dd98d5\", \"bonusAmountLimit\": 0, \"totalAmountLimit\": 1, \"turnoverMultiple\": 0, \"createdByUsername\": \"Modifier1\", \"autoSentPlayerBonus\": true}}]",
					ModifyUser = "Modifier1",
					CreatedOn = DateTime.UtcNow
				},
				new PromotionEventSource
				{
					Id = Guid.Parse("01855c4a-b9e6-0000-0001-07e620dd98d5"),
					PromotionId = promotionId,
					Action = PromotionEventAction.ChangeStatus.ToString(),
					ChangeSets = "[{\"op\": \"replace\", \"path\": \"/updatedByUsername\", \"value\": \"Modifier1\"}, {\"op\": \"replace\", \"path\": \"/updatedOn\", \"value\": \"2022-12-29T05:11:19.2708927Z\"}, {\"op\": \"replace\", \"path\": \"/snapshotEventId\", \"value\": \"01855c4a-b9e6-0000-0001-07e620dd98d5\"}]",
					ModifyUser = "Modifier1",
					CreatedOn = DateTime.UtcNow
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
		
		var expectedChangeSets = new StubChangeSet[] { new("replace", "/status", "Disabled") };

		var dbContext = web.Services.GetRequiredService<TgsContext>();

		_ = await dbContext.PromotionEventSources.AddAsync(new PromotionEventSource
		{
			Id = eventId,
			PromotionId = promotionId,
			Action = PromotionEventAction.ChangeStatus.ToString(),
			ChangeSets = JsonSerializer.Serialize(expectedChangeSets, jsonSerializerOptions),
			CreatedOn = DateTime.UtcNow
		});

		_ = await dbContext.SaveChangesAsync();

		var actual = await sut.GetPromotionEventDetailAsync(
			promotionId, eventId);

		Assert.IsNotNull(actual);
		CollectionAssert.AreEquivalent(
			expectedChangeSets, 
			actual.ChangeSets.Deserialize <StubChangeSet[]>(jsonSerializerOptions));
	}
}
