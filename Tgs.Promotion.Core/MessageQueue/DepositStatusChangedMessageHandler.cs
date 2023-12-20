using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.MessageQueue;

public class DepositStatusChangedMessageHandler : IMessageHandler
{
	private readonly IPromotionChecker _dailyFirstDepositChecker;
	private readonly IPromotionChecker _firstDepositChecker;
	private readonly ILogger<DepositStatusChangedMessageHandler> _logger;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IPlayerRepository _playerService;

	public DepositStatusChangedMessageHandler(
		IPaymentServiceClient paymentServiceClient,
		IPromotionCheckerFactory promotionCheckerFactory,
		IPlayerRepository playerService,
		ILogger<DepositStatusChangedMessageHandler> logger)
	{
		ArgumentNullException.ThrowIfNull(promotionCheckerFactory);

		_paymentServiceClient =
			paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_firstDepositChecker = promotionCheckerFactory.GetPromotionChecker("FirstDeposit");
		_dailyFirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("DailyFirstDeposit");
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Deposit status change event.");

		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var previousStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		_logger.LogInformation("formEvent: ({formEvent})", JsonFormatter.Default.Format(formEvent));

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm == null)
			return;

		_logger.LogInformation("Current Status: {status}", depositForm.Status);

		if (previousStatus == PaymentFlowState.WaitApprove)
			return;

		var currentDate = DateTimeOffset.Now.Date;

		switch (depositForm.Status)
		{
			case PaymentFlowState.Approved:
				await _playerService
					.SetFirstDepositTimeAsync(depositForm.PlayerCode, DateTimeOffset.Now)
					.ConfigureAwait(false);

				await _firstDepositChecker
					.CommitTagAsync(depositForm.PlayerCode, depositForm.PromotionId, cancellationToken)
					.ConfigureAwait(false);

				if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
					await _dailyFirstDepositChecker
						.CommitTagAsync(depositForm.PlayerCode, depositForm.PromotionId, cancellationToken)
						.ConfigureAwait(false);

				break;

			case PaymentFlowState.Reject:
				await _firstDepositChecker
					.RollbackTagAsync(depositForm.PlayerCode, depositForm.PromotionId, cancellationToken)
					.ConfigureAwait(false);

				if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
					await _dailyFirstDepositChecker
						.RollbackTagAsync(depositForm.PlayerCode, depositForm.PromotionId, cancellationToken)
						.ConfigureAwait(false);
				break;
		}
	}
}
