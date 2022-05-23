using System.Text;
using System.Text.Json;

namespace Tgs.Promotion.Clients;

public class PromotionDistributeClient : IPromotionDistributeClient
{
	private readonly HttpClient _client;

	public PromotionDistributeClient(HttpClient client)
		=> _client = client ?? throw new ArgumentNullException(nameof(client));

	public async Task<Guid?> DistributePromotionAsync(Guid brandId, Guid promotionId, int conditionHash,
		string playerCode,
		decimal amount, string? distributor)
	{
		var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/promotionDistribute")
		{
			Content =
				new StringContent(JsonSerializer.Serialize(new
				{
					brandId,
					promotionId,
					conditionHash,
					playerCode,
					amount,
					distributor
				}), Encoding.UTF8, "application/json")
		};
		var response = await _client.SendAsync(httpRequest).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var json = await response.Content
			.ReadAsStringAsync()
			.ConfigureAwait(false);

		return string.IsNullOrEmpty(json)
			? null
			: JsonSerializer.Deserialize<Guid?>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
	}
}