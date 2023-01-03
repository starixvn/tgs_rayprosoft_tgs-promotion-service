using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.Promotion.ApplicationServices;
using Tgs.Promotion.Models;
using Tgs.SerialNumberGenerator;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class PromotionTests
{
	[TestMethod]
	public async Task Create_Promotion()
	{
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeQueryPromotionTypeGroupService = Substitute.For<IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup>>();

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var brandId = Guid.NewGuid();
		var command = new CreatePromotionMainInfoCommand(
			brandId,
			"TestPromotion",
			(utcDateTime.AddDays(-10), utcDateTime),
			"TestType",
			null,
			true,
			"ModifyUser");

		_ = fakeQueryPromotionTypeGroupService
			.QueryAsync(Arg.Any<PromotionTypeGroupsQuery>())
			.Returns(new[]
			{
				new PromotionTypeGroup
				{
					Name = "TestType",
					Types = Array.Empty<PromotionType>(),
					Trans = new Translation()
				}
			}.ToAsyncEnumerable());

		var createPromotionId = Guid.NewGuid();
		var eventId = Guid.NewGuid();

		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId, createPromotionId);

		var expectedSnapshot = new PromotionSnapshot(
			Id: createPromotionId,
			Name: "TestPromotion",
			BrandId: brandId,
			From: utcDateTime.AddDays(-10),
			To: utcDateTime,
			Status: PromotionStatus.Disabled,
			PromotionType: command.PromotionType,
			SettlementIntervalType: null,
			AutoSentPlayerBonus: true,
			BonusAmountLimit: 0M,
			Unlimited: 0,
			GameType: null,
			Percentage: 0M,
			TotalAmountLimit: 0M,
			TurnoverMultiple: 0M,
			CampaignDetail: JsonDocument.Parse("{}"),
			Content: null,
			Localization: null,
			DesktopBanner: null,
			MobileBanner: null,
			SubSubject: null,
			Terms: null,
			CreatedOn: utcDateTime,
			CreatedByUsername: "ModifyUser",
			UpdatedOn: utcDateTime,
			UpdatedByUsername: null,
			AmountUsed: 0M,
			AmountUsedSnapshotId: null,
			DisplayOrder: 0,
			VipLevelId: null,
			SnapshotEventId: eventId);

		var sut = new CreatePromotionMainInfoCommandService(
			fakeSerialNumberGenerator,
			fakeSystemClock,
			fakePromotionRepository,
			fakeQueryPromotionTypeGroupService);

		var actual = await sut.GetAsync(command);

		Assert.AreEqual(createPromotionId, actual);

		_ = fakePromotionRepository.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(actual),
				Arg.Is(PromotionEventAction.CreateMainInfo),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.UpdatePromotionSnapshotAsync(
			Arg.Is(expectedSnapshot),
			Arg.Is(eventId),
			Arg.Is(utcDateTime),
			Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Get_Promotion_Content()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetPromotionContentQueryService(fakePromotionRepository);

		var query = new GetPromotionContentQuery(Guid.NewGuid());

		_ = await sut.GetAsync(query);

		_ = fakePromotionRepository.Received(1)
			.GetPromotionContentAsync(Arg.Is(query.PromotionId));
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
					JsonDocument.Parse("{}"),
					0)
			}.ToAsyncEnumerable());

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Any<string>())
			.Returns(new AlwaysTrueChecker());

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(1, actual.Length);
	}

	[TestMethod]
	public async Task Query_PromotionIds_With_Name()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var brandId = Guid.NewGuid();

		var promotions = new[]
		{
			new PromotionMainInfo(
				Guid.NewGuid(),
				brandId,
				"test1",
				(DateTime.Parse("2022-01-01"), DateTime.Parse("2022-01-31")),
					"Roulette",
					null,
					false),
			new PromotionMainInfo(
				Guid.NewGuid(),
				brandId,
				"test2",
				(DateTime.Parse("2022-01-01"), DateTime.Parse("2022-01-31")),
				"Roulette",
				null,
				false),
			new PromotionMainInfo(
				Guid.NewGuid(),
				brandId,
				"test3",
				(DateTime.Parse("2022-01-01"), DateTime.Parse("2022-01-31")),
				"Roulette",
				null,
				false)
		};

		var query = new PromotionIdsQuery(
			brandId,
			"test",
			null);

		_ = fakePromotionRepository
			.QueryPromotionsAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>())
				.Returns(promotions.ToAsyncEnumerable());

		var sut = new PromotionIdsQueryService(fakePromotionRepository);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(3, actual.Length);

		_ = fakePromotionRepository
			.Received(1)
			.QueryPromotionsAsync(
				Arg.Is(brandId),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Query_PromotionIds_With_Name_And_PromotionType()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var brandId = Guid.NewGuid();

		var promotions = new[]
		{
			new PromotionMainInfo(
				Guid.NewGuid(),
				brandId,
				"test1",
				(DateTime.Parse("2022-01-01"), DateTime.Parse("2022-01-31")),
				"Roulette",
				null,
				false),
			new PromotionMainInfo(
				Guid.NewGuid(),
				brandId,
				"test2",
				(DateTime.Parse("2022-01-01"), DateTime.Parse("2022-01-31")),
				"Roulette",
				null,
				false)
		};

		var expected = promotions
			.Select(p => p.Id)
			.ToArray();

		var query = new PromotionIdsQuery(
			brandId,
			"test",
			"Roulette");

		_ = fakePromotionRepository
			.QueryPromotionsByPromotionTypeAsync(
				Arg.Is(brandId),
				Arg.Is(query.PromotionType!),
				Arg.Any<CancellationToken>())
			.Returns(promotions.ToAsyncEnumerable());

		var sut = new PromotionIdsQueryService(fakePromotionRepository);

		var actual = await sut.QueryAsync(query).ToArrayAsync();

		Assert.AreEqual(2, actual.Length);
		CollectionAssert.AreEqual(expected, actual);

		_ = fakePromotionRepository
			.Received(1)
			.QueryPromotionsByPromotionTypeAsync(
				Arg.Is(brandId),
				Arg.Is(query.PromotionType!),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Set_Promotion_Rule()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionRuleCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository,
			new PromotionSerializer());

		var command = new UpdatePromotionRuleCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{\"playerAutoJoin\": true}"),
			0M,
			0M,
			2M,
			null,
			"ModifyUser");
		
		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-10),
			utcDateTime.AddDays(1),
			PromotionStatus.Disabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			promotion.Status,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			command.BonusAmountLimit,
			promotion.Unlimited,
			command.GameType,
			command.Percentage,
			promotion.TotalAmountLimit,
			command.TurnoverMultiple,
			command.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion.AmountUsed,
			promotion.AmountUsedSnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateRule),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Set_Promotion_Rule_Has_GameType()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionRuleCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository,
			new PromotionSerializer());

		var command = new UpdatePromotionRuleCommand(
			Guid.NewGuid(),
			JsonDocument.Parse("{\"playerAutoJoin\": true}"),
			0M,
			0M,
			2M,
			GameType.TableGame,
			"ModifyUser");

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-10),
			utcDateTime.AddDays(1),
			PromotionStatus.Disabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);
		
		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			promotion.Status,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			command.BonusAmountLimit,
			promotion.Unlimited,
			command.GameType,
			command.Percentage,
			promotion.TotalAmountLimit,
			command.TurnoverMultiple,
			command.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion.AmountUsed,
			promotion.AmountUsedSnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository.Received(1)
			.SetPromotionGameProvidersAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<IEnumerable<Guid>>());

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateRule),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Set_Promotion_Status()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new SetPromotionStatusCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository);

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var command = new SetPromotionStatusCommand(
			Guid.NewGuid(),
			PromotionStatus.Enabled,
			"ModifyUser");

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-10),
			utcDateTime.AddDays(1),
			PromotionStatus.Disabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
			Arg.Any<CancellationToken>())
			.Returns(promotion);

		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			PromotionStatus.Enabled,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			promotion.BonusAmountLimit,
			promotion.Unlimited,
			promotion.GameType,
			promotion.Percentage,
			promotion.TotalAmountLimit,
			promotion.TurnoverMultiple,
			promotion.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion.AmountUsed,
			promotion.AmountUsedSnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.ChangeStatus),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Update_Promotion_Content()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionContentCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository);

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var command = new UpdatePromotionContentCommand(
			Guid.NewGuid(),
			null,
			null,
			null,
			null,
			null,
			JsonDocument.Parse("{}"),
			"ModifyUser");

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-10),
			utcDateTime.AddDays(1),
			PromotionStatus.Enabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var serializerOptions = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false,
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
			ReferenceHandler = ReferenceHandler.IgnoreCycles
		};

		var expectedSnapshot = new PromotionSnapshot(
				command.PromotionId,
				promotion.Name,
				promotion.BrandId,
				promotion.From,
				promotion.To,
				promotion.Status,
				promotion.PromotionType,
				promotion.SettlementIntervalType,
				promotion.AutoSentPlayerBonus,
				promotion.BonusAmountLimit,
				promotion.Unlimited,
				promotion.GameType,
				promotion.Percentage,
				promotion.TotalAmountLimit,
				promotion.TurnoverMultiple,
				promotion.CampaignDetail,
				command.Content,
				JsonSerializer.Serialize(command.Localization, serializerOptions),
				command.PersonalComputerBanner,
				command.MobileBanner,
				command.SubSubject,
				command.Convention,
				promotion.CreatedByUsername,
				promotion.CreatedOn,
				command.Modifier,
				utcDateTime,
				promotion.AmountUsed,
				promotion.AmountUsedSnapshotId,
				promotion.DisplayOrder,
				promotion.VipLevelId,
				eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateContent),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task Update_Promotion_MainInfo()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();
		var fakeQueryPromotionTypeGroupService = Substitute.For<IQueryService<PromotionTypeGroupsQuery, PromotionTypeGroup>>();

		var sut = new UpdatePromotionMainInfoCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository, 
			fakeQueryPromotionTypeGroupService);

		var command = new UpdatePromotionMainInfoCommand(
			Guid.NewGuid(),
			"Test_Promotion",
			(DateTime.Now.AddDays(-10), DateTime.Now),
			"Test_Type",
			null,
			true,
			"ModifyUser");

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;
		
		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-15),
			utcDateTime.AddDays(-10),
			PromotionStatus.Enabled,
			"TestType",
			null,
			false,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		_ = fakeQueryPromotionTypeGroupService
			.QueryAsync(Arg.Any<PromotionTypeGroupsQuery>())
			.Returns(new[]
			{
				new PromotionTypeGroup
				{
					Name = "Test_Type",
					Types = Array.Empty<PromotionType>(),
					Trans = new Translation()
				}
			}.ToAsyncEnumerable());

		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			command.Name,
			promotion.BrandId,
			command.EventDuring.From,
			command.EventDuring.To,
			promotion.Status,
			command.PromotionType,
			null,
			command.IsAutoSent,
			promotion.BonusAmountLimit,
			1,
			promotion.GameType,
			promotion.Percentage,
			0M,
			promotion.TurnoverMultiple,
			promotion.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion.AmountUsed,
			promotion.AmountUsedSnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateMainInfo),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
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
	public async Task 不更新PromotionUsedAmount當要更新的AmountUsedSnapshotId較Snapshot的AmountUsedSnapshotId為舊()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionUsedAmountCommandService(fakeSystemClock, fakeSerialNumberGenerator, fakePromotionRepository);

		var command = new UpdatePromotionUsedAmountCommand(
			Guid.NewGuid(),
			10000M,
			Guid.Parse("01854c81-5489-0000-0001-07e620dd98d5")
			);

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-15),
			utcDateTime.AddDays(-10),
			PromotionStatus.Enabled,
			"TestType",
			null,
			false,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			2000M,
			Guid.Parse("01854deb-6f98-0000-0001-07e620dd98d5"),
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.DidNotReceive()
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateTotalUsedAmount),
				Arg.Any<JsonDocument>(),
				Arg.Is((string?)null),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.DidNotReceive()
			.UpdatePromotionSnapshotAsync(
				Arg.Any<PromotionSnapshot>(),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 以PromotionId查詢Events()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetEventsQueryService(fakePromotionRepository);

		var query = new GetEventsQuery(
			Guid.NewGuid());

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakePromotionRepository
			.Received(1)
			.QueryPromotionEventsAsync(
				Arg.Is(query.PromotionId),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 更新PromotionUsedAmount當Snapshot的EventId為null()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionUsedAmountCommandService(fakeSystemClock, fakeSerialNumberGenerator, fakePromotionRepository);

		var command = new UpdatePromotionUsedAmountCommand(
			Guid.NewGuid(),
			10000M,
			Guid.NewGuid());

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-15),
			utcDateTime.AddDays(-10),
			PromotionStatus.Enabled,
			"TestType",
			null,
			false,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			promotion.Status,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			promotion.BonusAmountLimit,
			promotion.Unlimited,
			promotion.GameType,
			promotion.Percentage,
			promotion.TotalAmountLimit,
			promotion.TurnoverMultiple,
			promotion.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			promotion.UpdatedByUsername,
			promotion.UpdatedOn,
			command.TotalAmount,
			command.SnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateTotalUsedAmount),
				Arg.Any<JsonDocument>(),
				Arg.Is((string?)null),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 更新PromotionUsedAmount當要更新的AmountUsedSnapshotId較Snapshot的AmountUsedSnapshotId為新()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new UpdatePromotionUsedAmountCommandService(fakeSystemClock, fakeSerialNumberGenerator, fakePromotionRepository);

		var command = new UpdatePromotionUsedAmountCommand(
			Guid.NewGuid(),
			10000M,
			Guid.Parse("01854deb-6f98-0000-0001-07e620dd98d5"));

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			command.PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-15),
			utcDateTime.AddDays(-10),
			PromotionStatus.Enabled,
			"TestType",
			null,
			false,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			Guid.Parse("01854c81-5489-0000-0001-07e620dd98d5"),
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(command.PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);
		
		var expectedSnapshot = new PromotionSnapshot(
			command.PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			promotion.Status,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			promotion.BonusAmountLimit,
			promotion.Unlimited,
			promotion.GameType,
			promotion.Percentage,
			promotion.TotalAmountLimit,
			promotion.TurnoverMultiple,
			promotion.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			promotion.UpdatedByUsername,
			promotion.UpdatedOn,
			command.TotalAmount,
			command.SnapshotId,
			promotion.DisplayOrder,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(command.PromotionId),
				Arg.Is(PromotionEventAction.UpdateTotalUsedAmount),
				Arg.Any<JsonDocument>(),
				Arg.Is((string?)null),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 查詢Event的Detail()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetEventDetailQueryService(fakePromotionRepository);

		var query = new GetEventDetailQuery(
			Guid.NewGuid(),
			Guid.NewGuid());

		_ = await sut.GetAsync(query).ConfigureAwait(false);

		_ = fakePromotionRepository
			.Received(1)
			.GetEventDetailAsync(
				Arg.Is(query.PromotionId),
				Arg.Is(query.EventId),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 根據PromotionType取得PromotionMainInfo()
	{
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new GetPromotionMainInfosByPromotionTypeQueryService(fakePromotionRepository);

		var query = new GetPromotionMainInfosByPromotionTypeQuery("TestType");

		_ = await sut.QueryAsync(query).ToArrayAsync();

		_ = fakePromotionRepository.Received(1)
			.QueryPromotionsByPromotionTypeAsync(Arg.Is(query.PromotionType));
	}

	[TestMethod]
	public async Task 變更兩個Promotion的DisplayOrder()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new ChangePromotionOrdersCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository);

		var orders = new PromotionOrder[]
		{
			new (Guid.NewGuid(), 10),
			new (Guid.NewGuid(), 3)
		};

		var command = new ChangePromotionOrdersCommand(
			orders,
			"ModifyUser");

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;
		var brandId = Guid.NewGuid();

		var promotion1 = new PromotionSnapshot(
		orders[0].PromotionId,
		"TestPromotion1",
		brandId,
		utcDateTime.AddDays(-10),
		utcDateTime.AddDays(1),
		PromotionStatus.Enabled,
		"TestType",
		null,
		true,
		0,
		1,
		null,
		0M,
		0M,
		0M,
		JsonDocument.Parse("{}"),
		null,
		null,
		null,
		null,
		null,
		null,
		"Creator",
		utcDateTime.AddDays(-15),
		null,
		utcDateTime.AddDays(-15),
		0M,
		null,
		0,
		null,
		Guid.NewGuid());

		var promotion2 = new PromotionSnapshot(
			orders[1].PromotionId,
			"TestPromotion2",
			brandId,
			utcDateTime.AddDays(-5),
			utcDateTime.AddDays(2),
			PromotionStatus.Enabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-5),
			null,
			utcDateTime.AddDays(-5),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(orders[0].PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion1);

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(orders[1].PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion2);

		var eventId1 = Guid.NewGuid();
		var eventId2 = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId1, eventId2);

		var expectedSnapshot1 = new PromotionSnapshot(
			orders[0].PromotionId,
			promotion1.Name,
			promotion1.BrandId,
			promotion1.From,
			promotion1.To,
			promotion1.Status,
			promotion1.PromotionType,
			promotion1.SettlementIntervalType,
			promotion1.AutoSentPlayerBonus,
			promotion1.BonusAmountLimit,
			promotion1.Unlimited,
			promotion1.GameType,
			promotion1.Percentage,
			promotion1.TotalAmountLimit,
			promotion1.TurnoverMultiple,
			promotion1.CampaignDetail,
			promotion1.Content,
			promotion1.Localization,
			promotion1.DesktopBanner,
			promotion1.MobileBanner,
			promotion1.SubSubject,
			promotion1.Terms,
			promotion1.CreatedByUsername,
			promotion1.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion1.AmountUsed,
			promotion1.AmountUsedSnapshotId,
			orders[0].Order,
			promotion1.VipLevelId,
			eventId1);

		var expectedSnapshot2 = new PromotionSnapshot(
			orders[1].PromotionId,
			promotion2.Name,
			promotion2.BrandId,
			promotion2.From,
			promotion2.To,
			promotion2.Status,
			promotion2.PromotionType,
			promotion2.SettlementIntervalType,
			promotion2.AutoSentPlayerBonus,
			promotion2.BonusAmountLimit,
			promotion2.Unlimited,
			promotion2.GameType,
			promotion2.Percentage,
			promotion2.TotalAmountLimit,
			promotion2.TurnoverMultiple,
			promotion2.CampaignDetail,
			promotion2.Content,
			promotion2.Localization,
			promotion2.DesktopBanner,
			promotion2.MobileBanner,
			promotion2.SubSubject,
			promotion2.Terms,
			promotion2.CreatedByUsername,
			promotion2.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion2.AmountUsed,
			promotion2.AmountUsedSnapshotId,
			orders[1].Order,
			promotion2.VipLevelId,
			eventId2);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId1),
				Arg.Is(orders[0].PromotionId),
				Arg.Is(PromotionEventAction.ChangeDisplayOrder),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot1),
				Arg.Is(eventId1),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId2),
				Arg.Is(orders[1].PromotionId),
				Arg.Is(PromotionEventAction.ChangeDisplayOrder),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository
			.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot2),
				Arg.Is(eventId2),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}

	[TestMethod]
	public async Task 變更單一Promotion的DisplayOrder()
	{
		var fakeSystemClock = Substitute.For<ISystemClock>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakePromotionRepository = Substitute.For<IPromotionRepository>();

		var sut = new ChangePromotionOrdersCommandService(
			fakeSystemClock,
			fakeSerialNumberGenerator,
			fakePromotionRepository);

		var orders = new PromotionOrder[]
		{
			new (Guid.NewGuid(), 10)
		};

		var command = new ChangePromotionOrdersCommand(
			orders,
			"ModifyUser");

		var currentTime = DateTimeOffset.UtcNow;
		_ = fakeSystemClock.UtcNow.Returns(currentTime);

		var utcDateTime = currentTime.UtcDateTime;

		var promotion = new PromotionSnapshot(
			orders[0].PromotionId,
			"TestPromotion",
			Guid.NewGuid(),
			utcDateTime.AddDays(-10),
			utcDateTime.AddDays(1),
			PromotionStatus.Disabled,
			"TestType",
			null,
			true,
			0,
			1,
			null,
			0M,
			0M,
			0M,
			JsonDocument.Parse("{}"),
			null,
			null,
			null,
			null,
			null,
			null,
			"Creator",
			utcDateTime.AddDays(-15),
			null,
			utcDateTime.AddDays(-15),
			0M,
			null,
			0,
			null,
			Guid.NewGuid());

		_ = fakePromotionRepository.GetCurrentPromotionSnapshotAsync(
				Arg.Is(orders[0].PromotionId),
				Arg.Any<CancellationToken>())
			.Returns(promotion);

		var eventId = Guid.NewGuid();
		_ = fakeSerialNumberGenerator.GenerateAsync(Arg.Any<CancellationToken>())
			.Returns(eventId);

		var expectedSnapshot = new PromotionSnapshot(
			orders[0].PromotionId,
			promotion.Name,
			promotion.BrandId,
			promotion.From,
			promotion.To,
			promotion.Status,
			promotion.PromotionType,
			promotion.SettlementIntervalType,
			promotion.AutoSentPlayerBonus,
			promotion.BonusAmountLimit,
			promotion.Unlimited,
			promotion.GameType,
			promotion.Percentage,
			promotion.TotalAmountLimit,
			promotion.TurnoverMultiple,
			promotion.CampaignDetail,
			promotion.Content,
			promotion.Localization,
			promotion.DesktopBanner,
			promotion.MobileBanner,
			promotion.SubSubject,
			promotion.Terms,
			promotion.CreatedByUsername,
			promotion.CreatedOn,
			command.Modifier,
			utcDateTime,
			promotion.AmountUsed,
			promotion.AmountUsedSnapshotId,
			orders[0].Order,
			promotion.VipLevelId,
			eventId);

		await sut.ExecuteAsync(command);

		_ = fakePromotionRepository
			.Received(1)
			.CreatePromotionEventAsync(
				Arg.Is(eventId),
				Arg.Is(orders[0].PromotionId),
				Arg.Is(PromotionEventAction.ChangeDisplayOrder),
				Arg.Any<JsonDocument>(),
				Arg.Is(command.Modifier),
				Arg.Any<CancellationToken>());

		_ = fakePromotionRepository.Received(1)
			.UpdatePromotionSnapshotAsync(
				Arg.Is(expectedSnapshot),
				Arg.Is(eventId),
				Arg.Is(utcDateTime),
				Arg.Any<CancellationToken>());
	}
}
