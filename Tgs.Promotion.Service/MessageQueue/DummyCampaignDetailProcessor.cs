using System.Runtime.CompilerServices;
using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Service.MessageQueue;

public class DummyCampaignDetailProcessor : IMessageSender
{
	public ValueTask<Answer<TReply>> AskAsync<TMessage, TReply>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask PublishAsync<TMessage>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask<TReply> RequestAsync<TMessage, TReply>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		var response = new CampaignDetailResponse
		{
			CampaignDetail = "{}"
		};

		var responseData = new ReadOnlyMemory<byte>(response.ToByteArray());

		return ValueTask.FromResult(Unsafe.As<ReadOnlyMemory<byte>, TReply>(ref responseData));
	}

	public ValueTask SendAsync<TMessage>(string subject, TMessage data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
