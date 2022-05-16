using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Promotion.Core;
using Tgs.Promotion.Core.MessageQueue;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.UnitTest
{
	[TestFixture]
	public class DepositStatusChangedMessageHandlerTests
	{
		[Test]
		public async Task When_Deposit_Form_Approved_Will_Commit_Tag()
		{
			var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

			var fakeServiceProvider = Substitute.For<IServiceProvider>();
			var fakePromotionChecker = Substitute.For<IPromotionChecker>();

			fakeServiceProvider.GetService(Arg.Any<Type>())
				.Returns(fakePromotionChecker);

			var fakeCheckFactory = new PromotionCheckerFactory(
				new Dictionary<string, Type>
				{
					["FirstDeposit"] = typeof(object),
					["DailyFirstDeposit"] = typeof(object)
				},
				fakeServiceProvider);

			var fakePlayerService = Substitute.For<IPlayerRepository>();

			var sut = new DepositStatusChangedMessageHandler(
				fakePaymentServiceClient,
				fakeCheckFactory,
				fakePlayerService,
				NullLogger<DepositStatusChangedMessageHandler>.Instance);

			var formEvent = new FormEvent
			{
				FormId = Guid.NewGuid().ToString(),
				BrandId = Guid.NewGuid().ToString()
			};

			fakePaymentServiceClient.GetDepositFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
				.Returns(new DepositFormInfo
				{
					Id = Guid.Parse(formEvent.FormId),
					BrandId = Guid.Parse(formEvent.BrandId),
					Status = PaymentFlowState.Approved,
					CreatedOn = DateTimeOffset.UtcNow,
					PlayerCode = "ThisIsPlayerCode"
				});

			await sut.HandleAsync(formEvent.ToByteArray());

			_ = fakePromotionChecker.Received(2)
				.CommitTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
			_ = fakePlayerService.Received(1)
				.SetFirstDepositTimeAsync(Arg.Is("ThisIsPlayerCode"), Arg.Any<DateTimeOffset>());
		}

		[Test]
		public async Task When_Deposit_Form_Rejected_Will_Rollback()
		{
			var fakePaymentServiceClient = Substitute.For<IPaymentServiceClient>();

			var fakeServiceProvider = Substitute.For<IServiceProvider>();
			var fakePromotionChecker = Substitute.For<IPromotionChecker>();
			var fakePromotionParticipantRepository = Substitute.For<IPromotionParticipantRepository>();

			fakeServiceProvider.GetService(Arg.Any<Type>())
				.Returns(fakePromotionChecker);

			var fakeCheckFactory = new PromotionCheckerFactory(
				new Dictionary<string, Type>
				{
					["FirstDeposit"] = typeof(object),
					["DailyFirstDeposit"] = typeof(object)
				},
				fakeServiceProvider);

			var fakePlayerService = Substitute.For<IPlayerRepository>();

			var sut = new DepositStatusChangedMessageHandler(
				fakePaymentServiceClient,
				fakeCheckFactory,
				fakePlayerService,
				NullLogger<DepositStatusChangedMessageHandler>.Instance);

			var formEvent = new FormEvent
			{
				FormId = Guid.NewGuid().ToString(),
				BrandId = Guid.NewGuid().ToString()
			};

			fakePaymentServiceClient.GetDepositFormAsync(Arg.Any<Guid>(), Arg.Any<Guid>())
				.Returns(new DepositFormInfo
				{
					Id = Guid.Parse(formEvent.FormId),
					BrandId = Guid.Parse(formEvent.BrandId),
					Status = PaymentFlowState.Reject,
					CreatedOn = DateTimeOffset.UtcNow
				});

			await sut.HandleAsync(formEvent.ToByteArray());

			_ = fakePromotionChecker.Received(2)
				.RollbackTagAsync(Arg.Any<string>(), Arg.Any<Guid?>());
		}
	}
}
