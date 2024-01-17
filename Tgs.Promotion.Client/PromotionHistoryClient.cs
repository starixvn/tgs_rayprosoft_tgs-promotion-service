using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.Clients;

internal class PromotionHistoryClient : IPromotionHistoryClient
{
	private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
	};

	private readonly HttpClient _httpClient;

	public PromotionHistoryClient(
		HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public ValueTask<PromotionEventDetailInfo?> GetPromotionEventDetailAsync(
		Guid promotionId,
		Guid eventId,
		CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"api/promotionHistory/detail/{promotionId}/{eventId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response =>
				response.Content.ReadFromJsonAsync<PromotionEventDetailInfo?>(_JsonSerializerOptions, cancellationToken)
					.ToAsyncEnumerable())
			.FirstOrDefaultAsync(cancellationToken);

	public IAsyncEnumerable<PromotionEventMainInfo> QueryPromotionEventsAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
		=> _httpClient.GetAsync($"api/promotionHistory/{promotionId}", cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response =>
				response.Content
					.ReadFromJsonAsync<IEnumerable<PromotionEventMainInfo>>(_JsonSerializerOptions, cancellationToken)
					.ToAsyncEnumerable()
					.Where(arr => arr is not null)
					.SelectMany(arr => arr!.ToAsyncEnumerable()));
}
