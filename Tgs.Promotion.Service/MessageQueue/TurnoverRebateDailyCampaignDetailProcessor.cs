using System.Runtime.CompilerServices;
using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Service.MessageQueue;

public class TurnoverRebateDailyCampaignDetailProcessor(PromotionSerializer promotionSerializer) : IMessageSender
{
	private readonly PromotionSerializer _promotionSerializer = promotionSerializer ?? throw new ArgumentNullException(nameof(promotionSerializer));

	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();

	public ValueTask PublishAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
	{
		var msg = Unsafe.As<TMessage, ReadOnlyMemory<byte>>(ref data);
		var request = CampaignDetailRequest.Parser.ParseFrom(msg.Span);

		var settings = _promotionSerializer.Deserialize<TurnoverRebateDailySettings>(request.Settings)!;

		var response = new CampaignDetailResponse
		{
			CampaignDetail = _promotionSerializer.Serialize(settings),
			BonusAmountLimit = settings.BonusAmountLimit,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple,
			GameType = settings.GameType.ToString(),
		};

		var responseData = new ReadOnlyMemory<byte>(response.ToByteArray());
		return ValueTask.FromResult(Unsafe.As<ReadOnlyMemory<byte>, TReply>(ref responseData));
	}

	public ValueTask SendAsync<TMessage>(
		string subject,
		TMessage data,
		IEnumerable<MessageHeaderValue> header,
		CancellationToken cancellationToken = default)
		=> throw new NotImplementedException();
}
