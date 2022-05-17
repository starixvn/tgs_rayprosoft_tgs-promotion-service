using System.Text;
using System.Text.Json;
using Tgs.Promotion.Client.Models;
using Tgs.Promotion.Client.Models.PromotionDispatch;
using TGS.Core.Infrastructures.Utility;

namespace Tgs.Promotion.Client;

public interface IPromotionDispatchClient
{
	Task<PagedResponse<PromotionDispatchModel>> GetPromotionDispatchesAsync(GetPromotionDispatchesRequest request);

	Task<MessageResponse> DispatchPromotionAsync(DispatchPromotionBonusRequest request);
}

public class PromotionDispatchClient : IPromotionDispatchClient
{
	private readonly HttpClient _client;

	public PromotionDispatchClient(HttpClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async Task<MessageResponse> DispatchPromotionAsync(
		DispatchPromotionBonusRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotionDispatch")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<PagedResponse<PromotionDispatchModel>> GetPromotionDispatchesAsync(
		GetPromotionDispatchesRequest request)
	{
		var path = ClientPath.AppendWithProperties("/api/promotionDispatch?", request);

		var httpRequest = new HttpRequestMessage(HttpMethod.Post, path);
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PagedResponse<PromotionDispatchModel>>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}
}
