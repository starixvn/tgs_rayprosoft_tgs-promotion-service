using System.Collections.Immutable;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.PlayerService.Client;
using Tgs.Promotion.MessageQueue;

namespace Tgs.Promotion.Core.UnitTests;

[TestClass]
public class DepositStatusChangedMessageHandlerTests
{
	[TestMethod]
	public async Task DepositStatusChanedMessageHandler_如果找不到存款單_不會處理()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakePlayerServiceClient.Received(0)
			.SetPlayerPropertiesAsync(
				Arg.Any<string>(),
				Arg.Any<IImmutableDictionary<PlayerPropertyName, string?>>());
		_ = fakeChecker.Received(0)
			.CommitTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
		_ = fakeChecker.Received(0)
			.RollbackTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[TestMethod]
	public async Task DepositStatusChangedMessageHandler_如果存款單之前的狀態不是WaitApprove_不會處理()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.Approved.ToString()
		};
		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formEvent.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = formEvent.FormId.ToGuid(),
				BrandId = formEvent.BrandId.ToGuid(),
				Status = PaymentFlowState.Approved,
				PlayerCode = playerCode,
				CreatedOn = DateTime.UtcNow,
				PromotionId = promotionId
			});

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakePlayerServiceClient.Received(0)
			.SetPlayerPropertiesAsync(
				Arg.Any<string>(),
				Arg.Any<IImmutableDictionary<PlayerPropertyName, string?>>());
		_ = fakeChecker.Received(0)
			.CommitTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
		_ = fakeChecker.Received(0)
			.RollbackTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
	}

	[TestMethod]
	public async Task DepositStatusChangedMessageHandler_當存款拒絕事件發生時_表單建立時間已經超過當日_不會Rollback每日的Flag()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formEvent.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = formEvent.FormId.ToGuid(),
				BrandId = formEvent.BrandId.ToGuid(),
				Status = PaymentFlowState.Reject,
				PlayerCode = playerCode,
				CreatedOn = DateTime.UtcNow.AddDays(-1D),
				PromotionId = promotionId
			});

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakeChecker.Received(1)
			.RollbackTagAsync(Arg.Is(playerCode), Arg.Is((Guid?)null));
		_ = fakeChecker.Received(1)
			.RollbackTagAsync(
				Arg.Is(playerCode),
				Arg.Is(promotionId));
	}

	[TestMethod]
	public async Task DepositStatusChangedMessageHandler_當存款拒絕事件發生時_會Rollback_Flag()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formEvent.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = formEvent.FormId.ToGuid(),
				BrandId = formEvent.BrandId.ToGuid(),
				Status = PaymentFlowState.Reject,
				PlayerCode = playerCode,
				CreatedOn = DateTime.UtcNow,
				PromotionId = promotionId
			});

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakeChecker.Received(1)
			.RollbackTagAsync(Arg.Is(playerCode), Arg.Is((Guid?)null));
		_ = fakeChecker.Received(2)
			.RollbackTagAsync(
				Arg.Is(playerCode),
				Arg.Is(promotionId));
	}

	[TestMethod]
	public async Task DepositStatusChangedMessageHandler_當存款通過事件發生時_如果表單建立時間已經超過當日_不會Commit每日的Flag()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formEvent.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = formEvent.FormId.ToGuid(),
				BrandId = formEvent.BrandId.ToGuid(),
				Status = PaymentFlowState.Approved,
				PlayerCode = playerCode,
				CreatedOn = DateTime.UtcNow.AddDays(-1D),
				PromotionId = promotionId
			});

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					dict => dict.ContainsKey(PlayerPropertyName.FirstDepositOn)));
		_ = fakeChecker.Received(1)
			.CommitTagAsync(Arg.Is(playerCode), Arg.Is((Guid?)null));
		_ = fakeChecker.Received(1)
			.CommitTagAsync(
				Arg.Is(playerCode),
				Arg.Is(promotionId));
	}

	[TestMethod]
	public async Task DepositStatusChangedMessageHandler_當存款通過事件發生時_會Commit首存Flag()
	{
		var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();
		var fakePromotionCheckerFactory = Substitute.For<IPromotionCheckerFactory>();
		var fakePlayerServiceClient = Substitute.For<IPlayerServiceClient>();

		var fakeChecker = Substitute.For<IPromotionChecker>();

		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("AccountFirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("FirstDeposit"))
			.Returns(fakeChecker);
		_ = fakePromotionCheckerFactory.GetPromotionChecker(Arg.Is("DailyFirstDeposit"))
			.Returns(fakeChecker);

		var sut = new DepositStatusChangedMessageHandler(
			fakePaymentServiceClient,
			fakePromotionCheckerFactory,
			fakePlayerServiceClient,
			NullLogger<DepositStatusChangedMessageHandler>.Instance);

		var formEvent = new FormEventEx
		{
			BrandId = Guid.NewGuid(),
			FormId = Guid.NewGuid(),
			Status = PaymentFlowState.WaitApprove.ToString()
		};
		var playerCode = "test1";
		var promotionId = Guid.NewGuid();

		_ = fakePaymentServiceClient.GetDepositFormAsync(Arg.Is(formEvent.FormId.ToGuid()))
			.Returns(new DepositFormInfo
			{
				Id = formEvent.FormId.ToGuid(),
				BrandId = formEvent.BrandId.ToGuid(),
				Status = PaymentFlowState.Approved,
				PlayerCode = playerCode,
				CreatedOn = DateTime.UtcNow,
				PromotionId = promotionId
			});

		await sut.HandleAsync(formEvent.ToByteArray());

		_ = fakePlayerServiceClient.Received(1)
			.SetPlayerPropertiesAsync(
				Arg.Is(playerCode),
				Arg.Is<IImmutableDictionary<PlayerPropertyName, string?>>(
					dict => dict.ContainsKey(PlayerPropertyName.FirstDepositOn)));
		_ = fakeChecker.Received(1)
			.CommitTagAsync(Arg.Is(playerCode), Arg.Is((Guid?)null));
		_ = fakeChecker.Received(2)
			.CommitTagAsync(
				Arg.Is(playerCode),
				Arg.Is(promotionId));
	}
}
