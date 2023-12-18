using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Checkers;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class RegisterCheckerTests
{
	[TestMethod]
	public async Task RegisterChecker_如果玩家資料沒有RegisteredOn_檢查會回傳False()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();

		var sut = new RegisterChecker(
			"test",
			fakePlayerServiceClient,
			fakePromotionParticipantRepository);

		var playerCode = "test1";
		var promotionMainInfo = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"aaa",
			(DateTime.Now.AddDays(-1D), DateTime.Now.AddDays(1D)),
			"test",
			null,
			false);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = promotionMainInfo.BrandId,
			});
		_ = fakePlayerServiceClient.GetPlayerStatusAsync(Arg.Is(playerCode))
			.Returns(PlayerService.Client.PlayerStatus.Enabled);

		var actual = await sut.CheckAsync(playerCode, promotionMainInfo, null);

		Assert.IsFalse(actual);
	}

	[TestMethod]
	public async Task RegisterChecker_檢查玩家䚾冊日期是否在活動期間()
	{
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();
		var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();

		var sut = new RegisterChecker(
			"test",
			fakePlayerServiceClient,
			fakePromotionParticipantRepository);

		var playerCode = "test1";
		var promotionMainInfo = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"aaa",
			(DateTime.Now.AddDays(-1D), DateTime.Now.AddDays(1D)),
			"test",
			null,
			false);

		_ = fakePlayerServiceClient.GetPlayerInfoAsync(Arg.Is(playerCode))
			.Returns(new PlayerInfo
			{
				PlayerCode = playerCode,
				BrandId = promotionMainInfo.BrandId,
				RegisteredOn = DateTime.UtcNow,
			});
		_ = fakePlayerServiceClient.GetPlayerStatusAsync(Arg.Is(playerCode))
			.Returns(PlayerService.Client.PlayerStatus.Enabled);

		var actual = await sut.CheckAsync(playerCode, promotionMainInfo, null);

		Assert.IsTrue(actual);
	}
}
