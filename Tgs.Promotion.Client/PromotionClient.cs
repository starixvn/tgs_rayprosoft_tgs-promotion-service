using System.Text;
using System.Text.Json;
using Tgs.Promotion.Client.Models;
using Tgs.Promotion.Client.Models.Promotion;
using TGS.Core.Infrastructures.Utility;

namespace Tgs.Promotion.Client;

public class PromotionClient : IPromotionClient
{
	private readonly HttpClient _client;

	public PromotionClient(HttpClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async Task<MessageResponse> ChangeOrderAsync(ChangeOrderRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Put, "/api/promotion/changeorder")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> ChangePromotionStatusAsync(UpdatePromotionStatusRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/promotion/{request.PromotionId}/{request.Status}")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<bool> CheckCanJoinDepositPromotionAsync(
		Guid brandId, string playerCode,
		Guid promotionId, decimal amount)
	{
		var response = await _client
			.GetAsync($"/api/promotion/{promotionId}/check/{brandId}/{playerCode}/{amount}")
			.ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<bool>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> CreatePromotionAsync(CreatePromotionRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotion")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> DepositJoinAsync(Guid promotionId, string playerCode)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/promotion/{promotionId}/depositAddPlayer/{playerCode}")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(new { promotionId, playerCode }), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<PagedResponse<JoinedPromotionModel>> GetJoinedPromotionsAsync(
		GetJoinedPromotionsRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/promotion/joined?playerCode={request.PlayerCode}&from={request.From.ToUniversalTime():u}&to={request.To.ToUniversalTime():u}");
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PagedResponse<JoinedPromotionModel>>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<PromotionBonusOfDepositModel[]> GetPromotionBonusOfDepositAsync(
		int[] promotionPlayerIds)
	{
		if (promotionPlayerIds?.Length > 0)
		{
			var request = new { promotionPlayerIds };

			var path = ClientPath.AppendWithProperties("/api/promotionBonus/bonusAmount?", request);

			var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
			var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

			response.EnsureSuccessStatusCode();

			return JsonSerializer.Deserialize<PromotionBonusOfDepositModel[]>(await response.Content.ReadAsStringAsync(),
				new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
		}

		return Array.Empty<PromotionBonusOfDepositModel>();
	}

	public async Task<PromotionNameModel[]> GetPromotionNamesAsync(GetPromotionNamesRequest request)
	{
		if (request.PromotionIds is null || request.PromotionIds.Length == 0)
			return Array.Empty<PromotionNameModel>();

		var path = ClientPath.AppendWithProperties("/api/promotion/names?", request);

		var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PromotionNameModel[]>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<PagedResponse<PromotionModel>> GetPromotionsAsync(GetPromotionsRequest request)
	{
		var path = ClientPath.AppendWithProperties("/api/promotion?", request);

		var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PagedResponse<PromotionModel>>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<PagedResponse<PromotionModel>> GetValidPromotionsAsync(GetValidPromotionsRequest request)
	{
		var path = ClientPath.AppendWithProperties("/api/promotion/valid?", request);

		var httpRequest = new HttpRequestMessage(HttpMethod.Get, path);
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<PagedResponse<PromotionModel>>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> JoinAsync(JoinPromotionRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/promotion/{request.PromotionId}/addPlayer/{request.PlayerCode}")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task RevertPromotionAsync(Guid promotionId, decimal amount)
	{
		var request = new HttpRequestMessage(
			HttpMethod.Delete,
			$"/api/promotion/{promotionId}/{amount}");

		request.Headers.Remove("Accept");
		request.Headers.Add("Accept", "application/json");

		var content = new StringContent(string.Empty);
		content.Headers.Remove("Content-Type");
		content.Headers.Add("Content-Type", "application/json");

		request.Content = content;

		var response = await _client.SendAsync(request).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();
	}

	public async Task TagPlayerDepositAsync(string playerCode, Guid? promotionId)
	{
		var path = promotionId == null
			? $"/api/promotion/deposit/{playerCode}"
			: $"/api/promotion/deposit/{playerCode}/{promotionId}";

		var request = new HttpRequestMessage(
			HttpMethod.Put,
			path);

		request.Headers.Remove("Accept");
		request.Headers.Add("Accept", "application/json");

		var content = new StringContent(string.Empty);
		content.Headers.Remove("Content-Type");
		content.Headers.Add("Content-Type", "application/json");

		request.Content = content;

		var response = await _client.SendAsync(request).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();
	}

	public async Task<MessageResponse> UpdatePromotionAsync(UpdatePromotionRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Put, $"/api/promotion/{request.Id}")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}
}
