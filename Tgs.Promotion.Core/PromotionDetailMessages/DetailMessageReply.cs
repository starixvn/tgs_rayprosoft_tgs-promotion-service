using NATS.Client.Core;
using NATS.Client.Serializers.Json;
using TGS.Core.Infrastructures.Nats.Interface;

namespace Tgs.Promotion.PromotionDetailMessages;

public class DetailMessageReply(string promotionType, IMessageManager messageManager) : IPromotionDetailMessage
{
	public string PromotionType => promotionType;

	public async ValueTask<DistributedDetailResponse?> GetDetailAsync(Guid promotionId, Guid referenceId,
		TimeSpan? timeout,
		CancellationToken cancellationToken = default)
	{
		var response = await messageManager.RequestReplyAsync<DistributedDetailRequest,DistributedDetailResponse>(
			string.Format(PromotionDetailConst.DetailSubjectTemplate, promotionType.ToLowerInvariant()),
			new DistributedDetailRequest { PromotionId = promotionId, ReferenceId = referenceId },
			requestSerializer: NatsJsonSerializerRegistry.Default.GetSerializer<DistributedDetailRequest>(),
			replySerializer: NatsJsonSerializerRegistry.Default.GetDeserializer<DistributedDetailResponse>(),
			replyOpts: timeout == null ? null : new NatsSubOpts{Timeout = timeout },
			cancellationToken: cancellationToken);

		return response.Data;
	}
}
