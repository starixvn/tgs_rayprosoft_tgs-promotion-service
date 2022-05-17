using System.Text.Json;
using NSubstitute;
using Tgs.Promotion.ApplicationServices;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class PromotionTests
{
	[TestMethod]
	public async Task Create_Promotion()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var sut = new CreatePromotionCommandService(fakePromotionRepository);

		var command = new CreatePromotionCommand(
			Guid.NewGuid(),
			"TestPromotion",
			(DateTime.Now.AddDays(-10), DateTime.Now),
			"TestType",
			null,
			true,
			JsonDocument.Parse("{}"),
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionAsync(Arg.Any<CreatePromotionInfo>());
	}
}
