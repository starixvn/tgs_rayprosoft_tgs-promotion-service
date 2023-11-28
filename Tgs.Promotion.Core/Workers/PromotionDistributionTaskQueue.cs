using System.Threading.Channels;

namespace Tgs.Promotion.Workers;

public interface IPromotionDistributionTaskQueue
{
    ValueTask QueuePromotionDistributeTaskQueueAsync(PromotionDistributionQueueModel model);
    ValueTask<PromotionDistributionQueueModel> DequeueAsync(
        CancellationToken cancellationToken);
}

public class PromotionDistributionTaskQueue : IPromotionDistributionTaskQueue
{
    private readonly Channel<PromotionDistributionQueueModel> _queue = Channel
        .CreateUnbounded<PromotionDistributionQueueModel>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask QueuePromotionDistributeTaskQueueAsync(
	    PromotionDistributionQueueModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        await _queue.Writer.WriteAsync(model);
    }
    public async ValueTask<PromotionDistributionQueueModel> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}