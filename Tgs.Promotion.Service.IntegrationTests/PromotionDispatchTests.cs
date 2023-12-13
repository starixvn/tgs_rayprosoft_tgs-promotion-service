using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Clients.Models.PromotionDispatch;
using Tgs.Promotion.Domain.Entities;

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
				var fakPromotionDistributionRepository = Substitute.For<IPromotionDistributionRepository>();
				var fakeSettingsRepository = Substitute.For<ISettingsRepository>();
				var fakePlayerRepository = Substitute.For<IPlayerRepository>();
				var fakePromotionRepository = Substitute.For<IPromotionRepository>();

				var promotionPlayers = Array.Empty<PromotionPlayer>();

				var promotionDistributes = Array.Empty<PromotionDistribute>();

				_ = fakPromotionBonusRepository.GetPromotionBonusPlayersQueries(Arg.Any<Func<IQueryable<PromotionPlayer>, IQueryable<PromotionPlayer>>?>(), Arg.Any<bool>())
					.Returns(promotionPlayers.AsQueryable().BuildMock());

				_ = fakPromotionDistributionRepository.GetPromotionDistributionQueries(Arg.Any<Func<IQueryable<PromotionDistribute>, IQueryable<PromotionDistribute>>?>(), Arg.Any<bool>())
					.Returns(promotionDistributes.AsQueryable().BuildMock());

				_ = fakeSettingsRepository
					.GetSettingAsync(Arg.Any<Expression<Func<Setting, bool>>>(),
						Arg.Any<Func<IQueryable<Setting>, IQueryable<Setting>>?>())
					.Returns(new Setting { Key = "Key", Value = @"[{""Name"": ""Turnover""}]" });

				var players = new Player[] { new() { PlayerCode = "playercode" } };

				_ = fakePlayerRepository
					.GetPlayerQueries(Arg.Any<Func<IQueryable<Player>, IQueryable<Player>>?>())
					.Returns(players.AsQueryable());

				_ = services.AddSingleton(fakeSettingsRepository);
				_ = services.AddSingleton(fakPromotionBonusRepository);
				_ = services.AddSingleton(fakPromotionDistributionRepository);
				_ = services.AddSingleton(fakePlayerRepository);
				_ = services.AddSingleton(fakePromotionRepository);
			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionDispatchClient(httpClient);

		var brandId = Guid.NewGuid();

		var actual = await sut.GetPromotionDispatchesAsync(new GetPromotionDispatchesRequest
		{
			BrandId = brandId,
			Page = 1,
			Rows = 20
		});

		Assert.IsNotNull(actual);
	}
}
