using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Service.MessageQueue;

public class DummyCampaignDetailProcessor : IMessageSender
{
	public ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask PublishAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		var response = new CampaignDetailResponse
		{
			CampaignDetail = "{}"
		};

		return ValueTask.FromResult<ReadOnlyMemory<byte>>(response.ToByteArray());
	}

	public ValueTask SendAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
