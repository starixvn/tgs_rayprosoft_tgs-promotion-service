using System.Collections.Immutable;
using System.Text.Json;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class PromotionTests
{
	[TestMethod]
	public async Task Create_Promotion()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeQeryPromotionTypeGroupService = Substitute.For<IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup>>();

		var sut = new CreatePromotionMainInfoCommandService(
			 fakePromotionRepository,
			 fakeQeryPromotionTypeGroupService);

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
	public async Task Get_Promotion_MainInfo()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetPromotionMainInfoQueryService(fakePromotionRepository);

		var query = new GetPromotionMainInfoQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakePromotionRepository.Received(1)
			.GetPromotionMainInfoAsync(Arg.Is(query.PromotionId));
	}

	[TestMethod]
	public async Task Get_Promotion_Rule()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetPromotionRuleQueryService(fakePromotionRepository);

		var query = new GetPromotionRuleQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakePromotionRepository.Received(1)
			.GetPromotionRuleAsync(Arg.Is(query.PromotionId));
	}

	[TestMethod]
	public async Task Get_PromotionTypeGroups()
	{
		var fakeSettingRepository = Substitute.For<ISettingsRepository>();

		var sut = new PromotionTypeGroupsQueryService(fakeSettingRepository);

		var query = new PromotionTypeGroupsQuery();

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakeSettingRepository.Received(1)
			.GetPromotionTypeGroupsAsync();
	}

	[TestMethod]
	public async Task List_Promotions_By_Brand()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new ListPromotionsByBrandQueryService(fakePromotionRepository);

		var query = new ListPromotionsByBrandQuery(Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakePromotionRepository.Received(1)
			.ListPromotionsAsync(Arg.Is(query.BrandId));
	}

	[TestMethod]
	public async Task List_Promotions_By_Player()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakeVipLevelMapService = Substitute.For<IGetService<GetBrandVipLevelMapQuery, IImmutableDictionary<Guid, VipLevelInfo>>>();
		var fakePromotionQueryService = Substitute.For<IQueryService<ListPromotionsByBrandQuery, PromotionView>>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();

		var sut = new ListPromotionsByPlayerQueryService(
			fakePlayerServiceClient,
			fakeVipLevelMapService,
			fakePromotionQueryService,
			fakePromotionCheckerFactory);

		var query = new ListPromotionsByPlayerQuery(Guid.NewGuid(), "test");

		var playerVipLevelId = Guid.NewGuid();

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(query.PlayerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = query.PlayerCode,
				VipLevelId = playerVipLevelId
			});

		_ = fakeVipLevelMapService.GetAsync(Arg.Any<GetBrandVipLevelMapQuery>())
			.Returns(new Dictionary<Guid, VipLevelInfo>
			{
				[playerVipLevelId] = new VipLevelInfo(playerVipLevelId, 1)
			}.ToImmutableDictionary());

		_ = fakePromotionQueryService.QueryAsync(Arg.Any<ListPromotionsByBrandQuery>())
			.Returns(new[]
			{
				new PromotionView(
					Guid.NewGuid(),
					"Test1",
					(DateTime.Now.AddDays(-10), DateTime.Now.AddDays(10)),
					PromotionStatus.Enabled,
					"AAA",
					null,
					0M,
					new[]{1 },
					null,
					null,
					null,
					null,
					null,
					JsonDocument.Parse("{}"),
					JsonDocument.Parse("{}"))
			}.ToAsyncEnumerable());

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Any<string>())
			.Returns(new AlwaysTrueChecker());

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[TestMethod]
	public async Task Set_Promotion_Rule()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionRuleCommandService(
			fakePromotionRepository,
			new PromotionSerializer());

		var command = new UpdatePromotionRuleCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{}"),
			0M,
			0M,
			2M,
			null,
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionRuleAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<PromotionRule>(),
				Arg.Is(command.Modifier));
	}

	[TestMethod]
	public async Task Set_Promotion_Rule_Has_GameTypeAsync()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionRuleCommandService(
			fakePromotionRepository,
			new PromotionSerializer());

		var command = new UpdatePromotionRuleCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{}"),
			0M,
			0M,
			2M,
			GameType.TableGame,
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionRuleAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<PromotionRule>(),
				Arg.Is(command.Modifier));

		_ = fakePromotionRepository.Received(1)
			.SetPromotionGameProvidersAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<IEnumerable<Guid>>());
	}

	[TestMethod]
	public async Task Set_Promotion_Status()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new SetPromotionStatusCommandService(fakePromotionRepository);

		var command = new SetPromotionStatusCommand(
			Guid.NewGuid(),
			PromotionStatus.Enabled,
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionStatusAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.Status),
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
	public async Task Update_Promotion_MainInfo()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionMainInfoCommandService(fakePromotionRepository);

		var command = new UpdatePromotionMainInfoCommand(
			Guid.NewGuid(),
			"TestPromotion",
			(DateTime.Now.AddDays(-10), DateTime.Now),
			"TestType",
			null,
			true,
			"ModifyUser");

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionMainInfoAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<UpdatePromotionMainInfo>(),
				Arg.Is(command.Modifier));
	}

	[TestMethod]
	public async Task Update_Promotion_Used_AmountAsync()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionUsedAmountCommandService(fakePromotionRepository);

		var command = new UpdatePromotionUsedAmountCommand(
			Guid.NewGuid(),
			10000M,
			Guid.NewGuid());

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionUsedAmountAsync(
				Arg.Is(command.PromotionId),
				Arg.Is(command.TotalAmount),
				Arg.Is(command.SnapshotId));
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
