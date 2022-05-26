using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.MessageQueue;

namespace Tgs.Promotion.MessageQueue;

public class TotalUsedAmountMessageHandler : IMessageHandler
{
	private readonly ILogger<TotalUsedAmountMessageHandler> _logger;
	private readonly IPromotionRepository _promotionRepository;

	public TotalUsedAmountMessageHandler(
		IPromotionRepository promotionRepository,
		ILogger<TotalUsedAmountMessageHandler> logger)
	{
		_promotionRepository =
			promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		_logger.LogInformation("update total used amount event.");

		var json = Encoding.UTF8.GetString(data.Span);
		var request = JsonSerializer.Deserialize<TotalUsedAmountMessageData>(json);

		if (request == null)
		{
			return;
		}

		_logger.LogInformation("formEvent: ({JsonSerializer.Deserialize<TotalUsedAmountMessageData>(json)})", request);

		await _promotionRepository
			.UpdatePromotionUsedAmountAsync(request.PromotionId, request.SnapshotId, request.TotalAmount, cancellationToken)
			.ConfigureAwait(false);
	}
}