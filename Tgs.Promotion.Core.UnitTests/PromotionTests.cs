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
		var sut = new CreatePromotionMainInfoCommandService(fakePromotionRepository);

		var command = new CreatePromotionMainInfoCommand(
			Guid.NewGuid(),
			"TestPromotion",
			(DateTime.Now.AddDays(-10), DateTime.Now),
			"TestType",
			null,
			true,
			"ModifyUser");

		_ = await sut.GetAsync(command);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionAsync(Arg.Any<CreatePromotionMainInfo>(), Arg.Is(command.Modifier));
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

	[TestMethod]
	public async Task Update_Promotion_Content()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionContentCommandService(fakePromotionRepository);

		var command = new UpdatePromotionContentCommand(
			Guid.NewGuid(),
			null,
			null,
			null,
			null,
			null,
			JsonDocument.Parse("{}"),
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionContentAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<PromotionContentDetail>(),
				Arg.Is(command.Modifier));
	}

	[TestMethod]
	public async Task Get_Promotion_MainInfo()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetPromotionMainInfoQueryService(fakePromotionRepository);

		var query = new GetPromotionMainInfoQuery(Guid.NewGuid());

		await sut.GetAsync(query);

		_ = fakePromotionRepository.Received(1)
			.GetPromotionMainInfoAsync(Arg.Is(query.PromotionId));
	}

	[TestMethod]
	public async Task Set_Promotion_Rule()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionRuleCommandService(fakePromotionRepository);

		var command = new UpdatePromotionRuleCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{}"),
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionRuleAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<PromotionRule>(),
				Arg.Is(command.Modifier));
	}
}
