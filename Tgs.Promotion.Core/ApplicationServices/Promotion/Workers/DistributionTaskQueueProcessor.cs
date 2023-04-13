using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Tgs.CashService.Client;
using Tgs.PlayerService.Client;
using Tgs.Promotion.Domain.Entities;
using Tgs.Service.PlayerBuckets;

namespace Tgs.Promotion.ApplicationServices.Promotion.Workers;

public interface IDistributionTaskQueueProcessor
{
	Task ProcessQueueAsync(CancellationToken cancellationToken);
}

public class DistributionTaskQueueProcessor : IDistributionTaskQueueProcessor
{
	private readonly ICashServiceClient _cashServiceClient;
	private readonly ILogger<DistributionTaskQueueProcessor> _logger;
	private readonly IPlayerBucketClient _playerBucketClient;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IDistributionTaskQueue _taskQueue;
	private readonly TgsContext _tgsContext;
	private readonly ITransactionRepository _transactionRepository;
	private readonly IPromotionPlayerRepository _promotionPlayerRepository;

	public DistributionTaskQueueProcessor(
		TgsContext tgsContext,
		IDistributionTaskQueue taskQueue,
		ICashServiceClient cashServiceClient,
		IPlayerServiceClient playerServiceClient,
		IPlayerBucketClient playerBucketClient,
		ITransactionRepository transactionRepository,
		IPromotionPlayerRepository promotionPlayerRepository,
		ILogger<DistributionTaskQueueProcessor> logger)
	{
		_taskQueue = taskQueue;
		_cashServiceClient = cashServiceClient;
		_playerServiceClient = playerServiceClient;
		_playerBucketClient = playerBucketClient;
		_transactionRepository = transactionRepository;
		_promotionPlayerRepository = promotionPlayerRepository;
		_tgsContext = tgsContext;
		_logger = logger;
	}

	public async Task ProcessQueueAsync(CancellationToken cancellationToken)
	{
		var request = await _taskQueue.DequeueAsync(cancellationToken);

		_logger.LogInformation("player promotion distribution request: {request}", request);

		if (await _promotionPlayerRepository.GetPromotionPlayerQueries()
		    .AnyAsync(x => x.Id == request.PromotionPlayerId && x.SentOn == null, cancellationToken))
		{
			var parentPlayer = await _playerServiceClient
				.GetPlayerPropertiesAsync(request.PlayerCode, cancellationToken).ConfigureAwait(false);
			var defaultWalletId = Guid.Parse(parentPlayer["DefaultWalletId"]);

			await _promotionPlayerRepository.SetSentOnAsync(request.PromotionPlayerId);

			_ = await Policy.Handle<Exception>()
				.RetryAsync(
					3,
					(exception, retryCount) => _logger.LogError(
						"Promotion Relief Weekly PlayerCode:{pb.PlayerCode} BucketAmount:{pb.BucketAmount} retryCount:{retryCount} Exception:{exception}",
						request.PlayerCode,
						request.BucketAmount,
						retryCount,
						exception))
				.ExecuteAsync(
					async () =>
						await _playerBucketClient.CreateBucketAsync(
							request.PlayerCode,
							request.BucketAmount,
							BucketType.Promotion,
							request.PromotionPlayerId.ToString(),
							null,
							cancellationToken).ConfigureAwait(false));

			var transactionResponse =
				await Policy.Handle<HttpRequestException>()
					.WaitAndRetryAsync(
						3,
						retryAttempt => TimeSpan.FromSeconds(1),
						(exception, time, retryCount, context) => _logger.LogError(
							"promotion daily externalId:{request.PromotionPlayerId} retryCount:{retryCount} Exception:{exception}",
							request.PromotionPlayerId.ToString(),
							retryCount,
							exception))
					.ExecuteAsync(async () =>
					{
						if (!await _tgsContext.Transactions
								.AnyAsync(
									x => x.PlayerCode == request.PlayerCode
										&& x.ExternalTransactionId == request.PromotionPlayerId.ToString(),
									cancellationToken))
						{
							return await _cashServiceClient
								.PromotionAsync(
									defaultWalletId,
									request.BonusAmount,
									request.Description,
									request.PromotionPlayerId.ToString(),
									null,
									cancellationToken)
								.ConfigureAwait(false);
						}

						return null;
					});

			if (transactionResponse != null)
			{
				var transactionPromotion = new TransactionPromotion
				{
					TransactionId = transactionResponse.TransactionId,
					PromotionId = request.PromotionId,
					PromotionBonusId = request.PromotionBonusId,
					PromotionType = request.PromotionType
				};

				await _transactionRepository.CreateTransactionPromotionsAsync(new[]
				{
				transactionPromotion
			});
			}
		}
	}
}
