using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Promotion.Clients;

internal class PromotionServiceClient : IPromotionServiceClient
{
	private static readonly JsonSerializerOptions _JsonSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
	};

	private readonly HttpClient _httpClient;

	public PromotionServiceClient(HttpClient httpClient)
	{
		_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
	}

	public async ValueTask<Guid> CreatePromotionAsync(
		Guid brandId,
		CreatePromotionMainInfo createInfo,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			createInfo.Name,
			createInfo.PromotionType,
			createInfo.EventDuring,
			createInfo.PromotionLimitAmount,
			createInfo.IsAutoSent,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/{brandId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var json = await response.Content
			.ReadAsStringAsync(cancellationToken)
			.ConfigureAwait(false);

		return JsonSerializer.Deserialize<Guid>(json, _JsonSerializerOptions);
	}

	public async ValueTask SetPromotionVipLevelsAsync(
		Guid promotionId,
		IEnumerable<int> vipLevels,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			VipLevels = vipLevels,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/vip/{promotionId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask SetPromotionContentAsync(
		Guid promotionId,
		PromotionContent content,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			content.SubSubject,
			content.PersonalComputerBanner,
			content.MobileBanner,
			content.Content,
			content.Convention,
			content.Localization,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/content/{promotionId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask SetPromotionRuleAsync<TRule>(
		Guid promotionId,
		TRule rule,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			CampaignDetail = rule,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/rule/{promotionId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}
}
