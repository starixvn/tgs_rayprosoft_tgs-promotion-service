using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.Promotion;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionJoinedTests
{
	[TestMethod]
	public async Task Check_Promotion_Joined()
	{
		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services =>
			{
				var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();

				_ = services.AddSingleton(fakePromotionParticipantRepository);
			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionClient(httpClient);

		var playerCode = "playerCode";

		var promotionId = Guid.NewGuid();

		var actual = await sut.CheckPromotionJoinedAsync(playerCode, promotionId);

		Assert.IsNotNull(actual);
	}

	[TestMethod]
	public async Task Get_Joined_Promotion()
	{
		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services =>
			{
				var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();

				var promotionParticipants = Array.Empty<PromotionParticipant>();
				_ = fakePromotionParticipantRepository.GetPromotionParticipantQueries(Arg.Any<Func<IQueryable<PromotionParticipant>, IQueryable<PromotionParticipant>>?>(), Arg.Any<bool>())
					.Returns(promotionParticipants.AsQueryable().BuildMock());

				_ = services.AddSingleton(fakePromotionParticipantRepository);
			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionClient(httpClient);

		var playerCode = "playerCode";

		var request = new GetJoinedPromotionsRequest
		{
			From = DateTimeOffset.Now.AddDays(-7D),
			To = DateTimeOffset.Now,
			Page = 1,
			Rows = 20,
			OrderBy = (short)SortOrder.Descending,
			PlayerCode = playerCode,
			ShowAll = false,
			SortBy = "CreatedOn",
		};

		var actual =
			await sut.GetJoinedPromotionsAsync(request);

		Assert.IsNotNull(actual);
		Assert.IsTrue(actual.Valid);
	}
}
