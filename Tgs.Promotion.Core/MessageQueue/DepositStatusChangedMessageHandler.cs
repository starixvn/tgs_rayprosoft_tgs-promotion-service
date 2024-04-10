using System.Collections.Immutable;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.PlayerService.Client;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.MessageQueue;

public class DepositStatusChangedMessageHandler : IMessageHandler<ReadOnlyMemory<byte>>
{
	private readonly IPromotionChecker _accountFirstDepositChecker;
	private readonly IPromotionChecker _dailyFirstDepositChecker;
	private readonly IPromotionChecker _firstDepositChecker;
	private readonly ILogger<DepositStatusChangedMessageHandler> _logger;
	private readonly IPaymentServiceClient _paymentServiceClient;
	private readonly IPlayerServiceClient _playerServiceClient;

	public DepositStatusChangedMessageHandler(
		IPaymentServiceClient paymentServiceClient,
		IPromotionCheckerFactory promotionCheckerFactory,
		IPlayerServiceClient playerServiceClient,
		ILogger<DepositStatusChangedMessageHandler> logger)
	{
		ArgumentNullException.ThrowIfNull(promotionCheckerFactory);

		_paymentServiceClient =
			paymentServiceClient ?? throw new ArgumentNullException(nameof(paymentServiceClient));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_firstDepositChecker = promotionCheckerFactory.GetPromotionChecker("FirstDeposit");
		_dailyFirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("DailyFirstDeposit");
		_accountFirstDepositChecker = promotionCheckerFactory.GetPromotionChecker("AccountFirstDeposit");
	}

	public async ValueTask HandleAsync(
		string subject,
		ReadOnlyMemory<byte> data,
		IEnumerable<MessageHeaderValue>? headerValues,
		CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("Deposit status change event.");

		var formEvent = FormEventEx.Parser.ParseFrom(data.Span);
		var previousStatus = Enum.Parse<PaymentFlowState>(formEvent.Status);

		_logger.LogInformation("formEvent: ({formEvent})", JsonFormatter.Default.Format(formEvent));

		if (previousStatus is PaymentFlowState.Reject)
			return;

		var depositForm = await _paymentServiceClient.GetDepositFormAsync(
			formEvent.FormId,
			cancellationToken).ConfigureAwait(false);

		if (depositForm == null)
			return;

		_logger.LogInformation("Current Status: {status}", depositForm.Status);

		var currentDate = DateTimeOffset.Now.Date;

		switch (depositForm.Status)
		{
			case PaymentFlowState.Approved:
				await _playerServiceClient.SetPlayerPropertiesAsync(
					depositForm.PlayerCode,
					new Dictionary<PlayerPropertyName, string?>
					{
						[PlayerPropertyName.FirstDepositOn] = DateTime.UtcNow.ToString("o")
					}.ToImmutableDictionary(),
					cancellationToken: cancellationToken).ConfigureAwait(false);

				await _accountFirstDepositChecker.CommitTagAsync(
					depositForm.PlayerCode,
					null,
					cancellationToken).ConfigureAwait(false);
				await _firstDepositChecker.CommitTagAsync(
					depositForm.PlayerCode,
					depositForm.PromotionId,
					cancellationToken).ConfigureAwait(false);

				if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
					await _dailyFirstDepositChecker.CommitTagAsync(
						depositForm.PlayerCode,
						depositForm.PromotionId,
						cancellationToken).ConfigureAwait(false);

				break;

			case PaymentFlowState.Reject:
				_logger.LogInformation("Deposit roll back depositFormId: {id}, promotionId:{promotionId}",
					depositForm.Id,
					depositForm.PromotionId);

				await _accountFirstDepositChecker.RollbackTagAsync(
					depositForm.PlayerCode,
					null,
					cancellationToken).ConfigureAwait(false);
				await _firstDepositChecker.RollbackTagAsync(
					depositForm.PlayerCode,
					depositForm.PromotionId,
					cancellationToken).ConfigureAwait(false);

				if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
					await _dailyFirstDepositChecker.RollbackTagAsync(
						depositForm.PlayerCode,
						depositForm.PromotionId,
						cancellationToken).ConfigureAwait(false);
				break;
		}
	}
}
