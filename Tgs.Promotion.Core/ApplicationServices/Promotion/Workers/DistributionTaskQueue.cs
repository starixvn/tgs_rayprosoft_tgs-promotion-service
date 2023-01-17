using System.Threading.Channels;

namespace Tgs.Promotion.ApplicationServices.Promotion.Workers;

public interface IDistributionTaskQueue
{
	ValueTask<DistributionQueueModel> DequeueAsync(
		CancellationToken cancellationToken);

	ValueTask QueuePlayerRebateAsync(
			DistributionQueueModel model);
}

public class DistributionTaskQueue : IDistributionTaskQueue
{
	private readonly Channel<DistributionQueueModel> _queue;

	public DistributionTaskQueue()
	{
		var options = new UnboundedChannelOptions
		{
			SingleReader = true
		};
		_queue = Channel
			.CreateUnbounded<DistributionQueueModel>(options);
	}

	public async ValueTask<DistributionQueueModel>
		DequeueAsync(
			CancellationToken cancellationToken)
	{
		var workItem = await _queue.Reader.ReadAsync(cancellationToken);

		return workItem;
	}

	public async ValueTask QueuePlayerRebateAsync(
			DistributionQueueModel model)
	{
		if (model == null)
		{
			throw new ArgumentNullException(nameof(model));
		}

		await _queue.Writer.WriteAsync(model);
	}
}
