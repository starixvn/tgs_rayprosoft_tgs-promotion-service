using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Repository;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionDispatchTests
{
	[TestMethod]
	public async Task Can_Get_Promotion_Dispatch_List()
	{
		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services =>
			{
				var fakPromotionBonusRepository = Substitute.For<IPromotionBonusRepository>();
				var fakeSettingsRepository = Substitute.For<ISettingsRepository>();
				var fakePlayerRepository = Substitute.For<IPlayerRepository>();
				var fakePromotionRepository = Substitute.For<IPromotionRepository>();

				var promotionPlayers = Array.Empty<PromotionPlayer>();
				_ = fakPromotionBonusRepository.GetPromotionBonusPlayersQueries(Arg.Any<Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>?>(), Arg.Any<bool>())
					.Returns(promotionPlayers.AsQueryable().BuildMock());

				_ = fakeSettingsRepository
					.GetSettingAsync(Arg.Any<Expression<Func<Setting, bool>>>(),
						Arg.Any<Func<IQueryable<Setting>, IQueryable<Setting>>?>())
					.Returns(new Setting { Key = "Key", Value = @"[{""Name"": ""Turnover""}]" });

				var players = new Player[] { new Player { PlayerCode = "playercode" } };

				_ = fakePlayerRepository
					.GetPlayerQueries(Arg.Any<Func<IQueryable<Player>, IQueryable<Player>>?>())
					.Returns(players.AsQueryable());

				_ = services.AddSingleton(fakeSettingsRepository);
				_ = services.AddSingleton(fakPromotionBonusRepository);
				_ = services.AddSingleton(fakePlayerRepository);
				_ = services.AddSingleton(fakePromotionRepository);
			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionDispatchClient(httpClient);

		var brandId = Guid.NewGuid();

		var actual = await sut.GetPromotionDispatchesAsync(new Clients.Models.PromotionDispatch.GetPromotionDispatchesRequest
		{
			BrandId = brandId
		});

		Assert.IsNotNull(actual);
	}
}
