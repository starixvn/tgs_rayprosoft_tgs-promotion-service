using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Tgs.MessageQueue;
using Tgs.Payment.Clients;
using Tgs.Payment.Protos;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Core.MessageQueue
{
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
            if (promotionCheckerFactory is null)
            {
                throw new ArgumentNullException(nameof(promotionCheckerFactory));
            }

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

            var formEvent = FormEvent.Parser.ParseFrom(data.Span);

            if (formEvent == null)
                return;

            _logger.LogInformation($"formEvent: ({JsonFormatter.Default.Format(formEvent)})");

            var depositForm = await _paymentServiceClient
                .GetDepositFormAsync(Guid.Parse(formEvent.BrandId), Guid.Parse(formEvent.FormId))
                .ConfigureAwait(false);

            if (depositForm == null)
                return;

            var currentDate = DateTimeOffset.Now.Date;

            switch (depositForm.Status)
            {
                case PaymentFlowState.Approved:
                    await _playerService
                        .SetFirstDepositTimeAsync(depositForm.PlayerCode, DateTimeOffset.Now)
                        .ConfigureAwait(false);

                    await _firstDepositChecker
                        .CommitTagAsync(depositForm.PlayerCode, depositForm.PromotionId)
                        .ConfigureAwait(false);

                    if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
                        await _dailyFirstDepositChecker
                            .CommitTagAsync(depositForm.PlayerCode, depositForm.PromotionId)
                            .ConfigureAwait(false);

                    break;

                case PaymentFlowState.Reject:
                    await _firstDepositChecker
                        .RollbackTagAsync(depositForm.PlayerCode, depositForm.PromotionId)
                        .ConfigureAwait(false);

                    if (depositForm.CreatedOn.ToLocalTime().Date == currentDate)
                        await _dailyFirstDepositChecker
                            .RollbackTagAsync(depositForm.PlayerCode, depositForm.PromotionId)
                            .ConfigureAwait(false);
                    break;
            }
        }
    }
}