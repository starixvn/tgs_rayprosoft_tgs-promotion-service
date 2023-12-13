using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Promotion.Domain.Entities;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Service.IntegrationTests;

[TestClass]
public class PromotionParticipantTests
{
	[TestMethod]
	public async Task Can_Create_Promotion_Participant()
	{
		await using var web = new TestPromotionApplication(builder => builder
			.ConfigureServices(services =>
			{
				var fakePromotionRepository = Substitute.For<IPromotionRepository>();
				var fakePromotionTypeRepository = Substitute.For<IPromotionTypeRepository>();
				var fakePromotionJoinLockRepository = Substitute.For<IPromotionJoinLockRepository>();
				var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();
				var fakeMessageSender = Substitute.For<IMessageSender>();

				var promotion = new PromotionRow
				{
					Status = 0,
					From = DateTime.Now,
					To = DateTime.Now.AddDays(1),
					PromotionType = "test"
				};
				_ = fakePromotionRepository
					.GetPromotionByIdAsync(Arg.Any<Guid>())
					.Returns(promotion);

				_ = fakePromotionTypeRepository
					.IsDepositTypeAsync(Arg.Any<string>())
					.Returns(false);

				_ = fakePromotionJoinLockRepository
					.SetWhenNotExistsAsync(Arg.Any<string>(), Arg.Any<Guid>())
					.Returns(true);

				_ = fakePromotionParticipantRepository
					.ExistsAsync(Arg.Any<Expression<Func<PromotionParticipant, bool>>>())
					.Returns(false);

				_ = fakeMessageSender
					.PublishAsync(Arg.Any<string>(), Arg.Any<ReadOnlyMemory<byte>>())
					.Returns(ValueTask.CompletedTask);

				_ = services.AddSingleton(fakePromotionRepository);
				_ = services.AddSingleton(fakePromotionTypeRepository);
				_ = services.AddSingleton(fakePromotionJoinLockRepository);
				_ = services.AddSingleton(fakePromotionParticipantRepository);
				_ = services.AddSingleton(fakeMessageSender);
			}));

		var httpClient = web.CreateClient();

		var sut = new PromotionClient(httpClient);

		var promotionId = Guid.NewGuid();
		var playerCode = "playerCode";

		var actual =
			await sut.JoinAsync(new(promotionId, playerCode));

		Assert.IsNotNull(actual);
		Assert.IsTrue(actual.Valid);
	}
}
