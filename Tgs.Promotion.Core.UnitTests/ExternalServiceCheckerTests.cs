using System.Text.Json;
using Google.Protobuf;
using NSubstitute;
using Tgs.Promotion.Checkers;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class ExternalServiceCheckerTests
{
	[TestMethod]
	public async Task ExternalServiceChecker_Commit玩家參加活動()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new ExternalServiceChecker("Test", fakeMessageSender);

		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		await sut.CommitTagAsync(playerCode, promotionId);

		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is(string.Format(PromotionCheckConsts.CommitSubjectTemplate, sut.PromotionType.ToLowerInvariant())),
				Arg.Is<ReadOnlyMemory<byte>>(data => CheckOtherActionData(data, promotionId, playerCode)));
	}

	[TestMethod]
	public async Task ExternalServiceChecker_Rollback玩家參加活動()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new ExternalServiceChecker("Test", fakeMessageSender);

		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		await sut.RollbackTagAsync(playerCode, promotionId);

		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is(string.Format(PromotionCheckConsts.RollbackSubjectTemplate, sut.PromotionType.ToLowerInvariant())),
				Arg.Is<ReadOnlyMemory<byte>>(data => CheckOtherActionData(data, promotionId, playerCode)));
	}

	[TestMethod]
	public async Task ExternalServiceChecker_標記玩家參加活動()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new ExternalServiceChecker("Test", fakeMessageSender);

		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		await sut.TagPromotionTypeAsync(playerCode, promotionId);

		_ = fakeMessageSender.Received(1)
			.SendAsync(
				Arg.Is(string.Format(PromotionCheckConsts.TagSubjectTemplate, sut.PromotionType.ToLowerInvariant())),
				Arg.Is<ReadOnlyMemory<byte>>(data => CheckOtherActionData(data, promotionId, playerCode)));
	}

	[TestMethod]
	public async Task ExternalServiceChecker_檢查是否可使用活動()
	{
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new ExternalServiceChecker("Test", fakeMessageSender);

		var playerCode = "test1";
		var promotion = new PromotionMainInfo(
			Guid.NewGuid(),
			Guid.NewGuid(),
			"AAA",
			(DateTime.Now, DateTime.Now),
			"Test",
			null,
			false);
		var extend = new
		{
			P1 = 1,
			P2 = 2
		};

		var response = new CheckPromotionResponse
		{
			Result = true
		};

		_ = fakeMessageSender.RequestAsync(
			Arg.Is(string.Format(PromotionCheckConsts.CheckSubjectTemplate, sut.PromotionType.ToLowerInvariant())),
			Arg.Is<ReadOnlyMemory<byte>>(data => CheckCheckData(data, promotion, playerCode)))
			.Returns(response.ToByteArray());

		var actual = await sut.CheckAsync(
			playerCode,
			promotion,
			JsonSerializer.SerializeToDocument(extend, PromotionUtility.SerializerOptions));

		Assert.IsTrue(actual);
	}

	private static bool CheckCheckData(ReadOnlyMemory<byte> data, PromotionMainInfo info, string playerCode)
	{
		var request = CheckPromotionRequest.Parser.ParseFrom(data.Span);
		var extend = JsonDocument.Parse(request.Extend);

		return request.PlayerCode == playerCode
			&& request.PromotionId.ToGuid() == info.Id
			&& extend.RootElement.GetProperty("p1").GetInt32() == 1
			&& extend.RootElement.GetProperty("p2").GetInt32() == 2;
	}

	private static bool CheckOtherActionData(ReadOnlyMemory<byte> data, Guid? promotionId, string playerCode)
	{
		var request = CheckPromotionRequest.Parser.ParseFrom(data.Span);

		return request.PlayerCode == playerCode
			&& (Guid?)request.PromotionId == promotionId;
	}
}
