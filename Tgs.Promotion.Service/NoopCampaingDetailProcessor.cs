using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.Service;

public class NoopCampaingDetailProcessor : IMessageSender
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
