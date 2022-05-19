using System.Text.Json;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;

namespace Tgs.Promotion.Service;

public class NoopCampaingDetailProcessor : IMessageSender
{
	private JsonSerializerOptions _serializerOptions;

	public NoopCampaingDetailProcessor(IOptions<JsonOptions> optionsAccessor)
	{
		_serializerOptions = optionsAccessor.Value.JsonSerializerOptions;
	}

	public ValueTask<Answer> AskAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask PublishAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();

	public ValueTask<ReadOnlyMemory<byte>> RequestAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<OldPromotionSettings>(request.Settings, _serializerOptions)!;

		var response = new CampaignDetailResponse
		{
			CampaignDetail = "{}",
			BonusAmountLimit = settings.BonusAmountLimit,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple
		};

		return ValueTask.FromResult<ReadOnlyMemory<byte>>(response.ToByteArray());
	}

	public ValueTask SendAsync(string subject, ReadOnlyMemory<byte> data, IEnumerable<MessageHeaderValue> header, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
