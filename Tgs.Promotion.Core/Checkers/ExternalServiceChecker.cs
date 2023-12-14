using System.Text.Json;
using Google.Protobuf;
using Tgs.Promotion.Clients.Protos;
using Valhalla.MessageQueue;

namespace Tgs.Promotion.Checkers;

internal class ExternalServiceChecker(string promotionType, IMessageSender messageSender) : IPromotionChecker
{
	public string PromotionType => promotionType;

	public async ValueTask<bool> CheckAsync(
		string playerCode,
		PromotionMainInfo promotion,
		JsonDocument? extend,
		CancellationToken cancellationToken = default)
	{
		var request = new CheckPromotionRequest
		{
			PlayerCode = playerCode,
			PromotionId = promotion.Id,
		};

		if (extend is not null)
			request.Extend = JsonSerializer.Serialize(extend);

		var responseData = await messageSender.RequestAsync(
			string.Format(PromotionCheckConsts.CheckSubjectTemplate, PromotionType.ToLowerInvariant()),
			request.ToByteArray(),
			cancellationToken).ConfigureAwait(false);

		var response = CheckPromotionResponse.Parser.ParseFrom(responseData.Span);

		return response.Result;
	}

	public ValueTask CommitTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		var request = new CheckPromotionRequest
		{
			PlayerCode = playerCode,
			PromotionId = promotionId,
		};

		return messageSender.SendAsync(
			string.Format(PromotionCheckConsts.CommitSubjectTemplate, PromotionType.ToLowerInvariant()),
			request.ToByteArray(),
			cancellationToken);
	}

	public ValueTask RollbackTagAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		var request = new CheckPromotionRequest
		{
			PlayerCode = playerCode,
			PromotionId = promotionId,
		};

		return messageSender.SendAsync(
			string.Format(PromotionCheckConsts.RollbackSubjectTemplate, PromotionType.ToLowerInvariant()),
			request.ToByteArray(),
			cancellationToken);
	}

	public ValueTask TagPromotionTypeAsync(string playerCode, Guid? promotionId, CancellationToken cancellationToken = default)
	{
		var request = new CheckPromotionRequest
		{
			PlayerCode = playerCode,
			PromotionId = promotionId,
		};

		return messageSender.SendAsync(
			string.Format(PromotionCheckConsts.TagSubjectTemplate, PromotionType.ToLowerInvariant()),
			request.ToByteArray(),
			cancellationToken);
	}
}
