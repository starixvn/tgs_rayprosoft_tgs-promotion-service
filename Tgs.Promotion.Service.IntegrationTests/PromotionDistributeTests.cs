using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MockQueryable.Moq;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionDistributeTests
{
	[TestMethod]
	public async Task Can_Get_Promotion_Dispatch_List()
	{
		var distributeId = Guid.NewGuid();
		var planId = Guid.NewGuid();

		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services =>
			{

				var fakPromotionDistributeRepository = Substitute.For<IPromotionDistributeRepository>();
				var fakePromotionDistributePlanRepository = Substitute.For<IPromotionDistributePlanRepository>();
				var fakePlayerRepository = Substitute.For<IPlayerRepository>();
				var fakePromotionRepository = Substitute.For<IPromotionRepository>();

				_ = fakPromotionDistributeRepository.CreatePromotionDistributeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>())
					.Returns(distributeId);

				_ = fakePromotionDistributePlanRepository
					.CreatePromotionDistributePlanAsync(Arg.Any<Guid>(),
						Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<string>())
					.Returns(planId);

				_ = fakePlayerRepository
					.GetPlayerAsync(Arg.Any<Expression<Func<Player, bool>>>())
					.Returns(new Player { PlayerCode = "playercode" });

				var promotion = new PromotionRow { };
				_ = fakePromotionRepository
					.GetPromotionByIdAsync(Arg.Any<Guid>())
					.Returns(promotion);

				_ = services.AddSingleton(fakPromotionDistributeRepository);
				_ = services.AddSingleton(fakePromotionDistributePlanRepository);
				_ = services.AddSingleton(fakePlayerRepository);
				_ = services.AddSingleton(fakePromotionRepository);

			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionDistributeClient(httpClient);

		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var conditionHash = 0;
		var playerCode = "playerCode";

		var actual = await sut.DistributePromotionAsync(brandId, promotionId, conditionHash, playerCode, 10, null);

		Assert.IsNotNull(actual);
		Assert.IsTrue(actual == distributeId);

	}
}
