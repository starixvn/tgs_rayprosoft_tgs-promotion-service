using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionAsync(Arg.Any<CreatePromotionInfo>());
	}

	[TestMethod]
	public async Task Update_Promotion_VipLevels()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionVipLevelsCommandService(fakePromotionRepository);

		var command = new UpdatePromotionVipLevelsCommand(
			Guid.NewGuid(),
			new[] { 1, 2 },
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionVipLevelsAsync(
				Arg.Is(command.PromotionId),
				Arg.Is<IEnumerable<int>>(levels => levels.Count() == 2 && levels.First() == 1 && levels.Last() == 2),
				Arg.Is(command.Modifier));
	}
}
