using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionHistoryTests
{
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
