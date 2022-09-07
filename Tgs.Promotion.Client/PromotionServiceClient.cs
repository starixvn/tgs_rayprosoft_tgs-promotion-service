using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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
		Converters = { new JsonStringEnumConverter() }
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

	public async ValueTask<Guid> DistributePromotionBonusAsync(
		Guid planId,
		string playerCode,
		decimal amount,
		decimal needTurnover,
		string? distributor,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			Amount = amount,
			NeedTurnover = needTurnover,
			Distributor = distributor
		};
		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");
		var response = await _httpClient.PutAsync(
			$"/api/distribute/{planId}/{playerCode}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync<Guid>(_JsonSerializerOptions, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<PromotionDistributePlanInfo> GetPromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.PutAsync(
			$"/api/distribute/plan/{promotionId}/{conditionHash}",
			null,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var result = await response.Content
			.ReadFromJsonAsync<PromotionDistributePlanInfo>(
				_JsonSerializerOptions,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return result!;
	}

	public async ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync(
			$"/api/promotion-mgr/{promotionId}",
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync<PromotionMainInfo>(
				_JsonSerializerOptions,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<TRule?> GetPromotionRuleAsync<TRule>(Guid promotionId, CancellationToken cancellationToken = default)
		where TRule : class
	{
		var response = await _httpClient.GetAsync(
			$"/api/promotion-mgr/rule/{promotionId}",
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync<TRule>(_JsonSerializerOptions, cancellationToken)
			.ConfigureAwait(false);
	}

	public async IAsyncEnumerable<PromotionView> ListPromotionsAsync(
		Guid brandId,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync(
			$"/api/promotionview/{brandId}",
			cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var json = await response.Content
			.ReadAsStringAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotions = JsonSerializer.Deserialize<PromotionView[]>(json, _JsonSerializerOptions);

		foreach (var promotion in promotions ?? Array.Empty<PromotionView>())
			yield return promotion;
	}

	public async IAsyncEnumerable<PromotionView> ListPromotionsAsync(
		Guid brandId,
		string playerCode,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync(
			$"/api/promotionview/{brandId}/{playerCode}",
			cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var json = await response.Content
			.ReadAsStringAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotions = JsonSerializer.Deserialize<PromotionView[]>(json, _JsonSerializerOptions);

		foreach (var promotion in promotions ?? Array.Empty<PromotionView>())
			yield return promotion;
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

	public async ValueTask SetPromotionStatusAsync(
		Guid promotionId,
		PromotionStatus status,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var requestContent = new StringContent(string.Empty);
		requestContent.Headers.Add("Modifier", modifier);

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/status/{promotionId}/{status}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
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

	public async ValueTask UpdatePromotionMainInfoAsync(
		Guid promotionId,
		CreatePromotionMainInfo updateInfo,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			updateInfo.Name,
			updateInfo.PromotionType,
			updateInfo.EventDuring,
			updateInfo.PromotionLimitAmount,
			updateInfo.IsAutoSent,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PatchAsync(
			$"/api/promotion-mgr/{promotionId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask UpdatePromotionUsedAmountAsync(
		Guid promotionId,
		decimal totalAmount,
		Guid? snapshotId,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			TotalAmount = totalAmount,
			SnapshotId = snapshotId
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/used/{promotionId}",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}
}
