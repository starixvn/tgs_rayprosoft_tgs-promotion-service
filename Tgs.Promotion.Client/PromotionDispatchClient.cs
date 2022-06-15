using System.Text;
using System.Text.Json;
using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionDispatch;

namespace Tgs.Promotion.Clients;

public interface IPromotionDispatchClient
{
	Task<MessageResponse> DispatchPromotionAsync(DispatchPromotionBonusRequest request);

	Task<PagedResponse<PromotionDispatchModel>> GetPromotionDispatchesAsync(GetPromotionDispatchesRequest request);
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

		var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PagedResponse<PromotionDispatchModel>>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}
}
