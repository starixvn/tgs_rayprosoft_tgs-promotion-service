using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DateOnlyTimeOnly.AspNet.Converters;
using Tgs.Promotion.Clients.Models;
using Tgs.Promotion.Clients.Models.PromotionBonus;

namespace Tgs.Promotion.Clients;

public class PromotionBonusClient : IPromotionBonusClient
{
	private readonly HttpClient _client;

	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = false,
		Converters =
		{
			new JsonStringEnumConverter(),
			new DateOnlyJsonConverter()
		}
	};

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

		var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(_SerializerOptions)
			.ConfigureAwait(false);

		return messageResponse!;
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

		var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(_SerializerOptions)
			.ConfigureAwait(false);

		return messageResponse!;
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

		var messageResponse = await response.Content.ReadFromJsonAsync<MessageResponse>(_SerializerOptions)
			.ConfigureAwait(false);

		return messageResponse!;
	}
}
