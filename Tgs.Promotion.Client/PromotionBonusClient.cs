using System.Text;
using System.Text.Json;
using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionBonus;

namespace Tgs.Promotion.Clients;

public interface IPromotionBonusClient
{
	Task<MessageResponse> CreatePromotionSettleDataDailyAsync(CreatePromotionBonusDailyRequest request);

	Task<MessageResponse> CreatePromotionSettleDataWeeklyAsync(CreatePromotionBonusWeeklyRequest request);

	Task<MessageResponse> CreatePromotionSettleDataMonthlyAsync(CreatePromotionBonusMonthlyRequest request);
}

public class PromotionBonusClient : IPromotionBonusClient
{
	private readonly HttpClient _client;

	public PromotionBonusClient(HttpClient client)
	{
		_client = client ?? throw new ArgumentNullException(nameof(client));
	}

	public async Task<MessageResponse> CreatePromotionSettleDataDailyAsync(
		CreatePromotionBonusDailyRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotionBonus/settleDataDaily")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> CreatePromotionSettleDataWeeklyAsync(
		CreatePromotionBonusWeeklyRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotionBonus/settleDataWeekly")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}

	public async Task<MessageResponse> CreatePromotionSettleDataMonthlyAsync(
		CreatePromotionBonusMonthlyRequest request)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotionBonus/settleDataMonthly")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return JsonSerializer.Deserialize<MessageResponse>(await response.Content.ReadAsStringAsync(),
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}
}
