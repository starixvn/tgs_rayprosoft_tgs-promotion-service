using System.Threading.Channels;

namespace Tgs.Promotion.Workers;

public interface IPromotionDispatchTaskQueue
{
	ValueTask<PromotionDispatchQueueModel> DequeueAsync(
		CancellationToken cancellationToken);

	ValueTask QueuePromotionDispatchTaskQueueAsync(PromotionDispatchQueueModel model);
}

public class PromotionDispatchTaskQueue : IPromotionDispatchTaskQueue
{
	private readonly Channel<PromotionDispatchQueueModel> _queue = Channel
		.CreateUnbounded<PromotionDispatchQueueModel>(new UnboundedChannelOptions
		{
			SingleReader = true
		});

	public async ValueTask<PromotionDispatchQueueModel> DequeueAsync(
		CancellationToken cancellationToken)
	{
		var workItem = await _queue.Reader.ReadAsync(cancellationToken);

		return workItem;
	}

	public async ValueTask QueuePromotionDispatchTaskQueueAsync(
			PromotionDispatchQueueModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		await _queue.Writer.WriteAsync(model);
	}
}
