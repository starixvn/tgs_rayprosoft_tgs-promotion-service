using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.Service.MessageQueue;

public class NoopCampaignDetailProcessor : IMessageSender
{
	private readonly PromotionSerializer _promotionSerializer;

	public NoopCampaignDetailProcessor(PromotionSerializer promotionSerializer)
	{
		_promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));
	}

	public ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask PublishAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = _promotionSerializer.Deserialize<GameProviderSettings>(request.Settings)!;

		var response = new CampaignDetailResponse
		{
			CampaignDetail = _promotionSerializer.Serialize(settings),
			BonusAmountLimit = settings.BonusAmountLimit,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple,
			GameType = settings.GameType.ToString(),
		};

		return ValueTask.FromResult<ReadOnlyMemory<byte>>(response.ToByteArray());
	}

	public ValueTask SendAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
