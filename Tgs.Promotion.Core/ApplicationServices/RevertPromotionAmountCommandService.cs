using Microsoft.Extensions.Logging;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.ApplicationServices
{
	internal class RevertPromotionAmountCommandService : ICommandService<RevertPromotionAmountCommand>
	{
		private readonly IPromotionRepository _promotionRepository;
		private readonly ILogger<RevertPromotionAmountCommandService> _logger;

		public RevertPromotionAmountCommandService(
			IPromotionRepository promotionRepository,
			ILogger<RevertPromotionAmountCommandService> logger)
		{
			_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async ValueTask ExecuteAsync(
			RevertPromotionAmountCommand command,
			CancellationToken cancellationToken = default)
		{
			var promotion = await _promotionRepository.GetPromotionByIdAsync(command.PromotionId);

			var amount = promotion!.Percentage * command.Amount / 100M;
			amount = amount > promotion.BonusAmountLimit ? promotion.BonusAmountLimit : amount;

			_logger.LogInformation($"Revert {amount}");

			var result = await _promotionRepository
				.RevertPromotionAmountAsync(command.PromotionId, amount)
				.ConfigureAwait(false);

			_logger.LogInformation($"Revert result: {result}");
		}
	}
}
