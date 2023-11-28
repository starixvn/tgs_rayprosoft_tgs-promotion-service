using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.AspNetCore.Http.Extensions;
using Tgs.Promotion.Clients.Models;

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

	public async ValueTask ChangePromotionOrdersAsync(IEnumerable<PromotionOrder> orders, string modifier,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			Orders = orders,
			Modifier = modifier
		};

		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");

		var response = await _httpClient.PutAsync(
			$"/api/promotion-mgr/orders",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
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
		Guid? referenceId,
		IEnumerable<string>? bucketTypes,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			Amount = amount,
			NeedTurnover = needTurnover,
			ReferenceId = referenceId,
			Distributor = distributor,
			BucketTypes = bucketTypes
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

	public async ValueTask DistributePromotionBonusManuallyAsync(Guid promotionDistributionId,
		string playerCode,
		string? distributor,
		CancellationToken cancellationToken = default)
	{
		var request = new
		{
			Distributor = distributor
		};
		var requestContent = new StringContent(
			JsonSerializer.Serialize(request, _JsonSerializerOptions),
			Encoding.UTF8,
			"application/json");
		var response = await _httpClient.PutAsync(
			$"/api/distribute/{promotionDistributionId}/{playerCode}/manually",
			requestContent,
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();
	}

	public async ValueTask<PromotionContent?> GetPromotionContentAsync(Guid promotionId, CancellationToken cancellationToken = default)
	{
		var response = await _httpClient.GetAsync(
			$"/api/promotion-mgr/content/{promotionId}",
			cancellationToken).ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		return await response.Content
			.ReadFromJsonAsync<PromotionContent>(
				_JsonSerializerOptions,
				cancellationToken: cancellationToken)
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

	public ValueTask<TRule?> GetPromotionRuleAsync<TRule>(Guid promotionId, CancellationToken cancellationToken = default)
		where TRule : class
		=> _httpClient
			.GetAsync(
				$"/api/promotion-mgr/rule/{promotionId}",
				cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<JsonNode>(_JsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.Where(node => node is not null)
			.Select(node => node!)
			.Do(node =>
			{
				if (node["compaignDetail"] is not null && node["campaignDetail"] is null)
					node["campaignDetail"] = JsonNode.Parse(node["compaignDetail"]!.ToJsonString());
			})
			.Select(node => node.Deserialize<TRule>(_JsonSerializerOptions))
			.FirstOrDefaultAsync(cancellationToken);

	public ValueTask<decimal> GetPromotionUsedAmountAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
		=> _httpClient
			.GetAsync(
				$"/api/promotion-mgr/used/{promotionId}",
				cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<decimal>(_JsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable())
			.FirstOrDefaultAsync(cancellationToken);

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

	public IAsyncEnumerable<PromotionView> ListPromotionsAsync(
		Guid brandId,
		During? eventDuring,
		string? name,
		IEnumerable<string>? promotionTypes,
		PromotionStatus? status,
		CancellationToken cancellationToken = default)
	{
		var queryStrings = new QueryBuilder();

		foreach (var type in promotionTypes ?? Array.Empty<string>())
			queryStrings.Add("promotionType", type);

		if (!string.IsNullOrEmpty(name))
			queryStrings.Add("name", name);

		if (eventDuring is not null)
		{
			queryStrings.Add("from", eventDuring.Value.From.ToString("o"));
			queryStrings.Add("to", eventDuring.Value.To.ToString("o"));
		}

		if (status is not null)
			queryStrings.Add("status", status.Value.ToString());

		return _httpClient.GetAsync(
				$"/api/promotionview/{brandId}:filters{queryStrings.ToQueryString().Value}",
				cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.SelectMany(response => response.Content
				.ReadFromJsonAsync<IEnumerable<PromotionView>>(_JsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.Where(arr => arr is not null)
				.SelectMany(arr => arr!.ToAsyncEnumerable()))
			.ToArrayAsync(cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.SelectMany(arr => arr.ToAsyncEnumerable());
	}

	public IAsyncEnumerable<PromotionMainInfo> ListPromotionsByPromotionTypeAsync(string promotionType, CancellationToken cancellationToken = default)
			=> _httpClient.GetAsync(
			$"/api/promotion-mgr/{promotionType}",
			cancellationToken)
			.ToAsyncEnumerable()
			.Do(response => response.EnsureSuccessStatusCode())
			.Select(response => response.Content
				.ReadFromJsonAsync<IEnumerable<PromotionMainInfo>>(_JsonSerializerOptions, cancellationToken)
				.ToAsyncEnumerable()
				.Where(arr => arr is not null)
				.Select(arr => arr!.ToAsyncEnumerable())
				.Concat())
			.Concat()
			.ToArrayAsync(cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.Select(arr => arr.ToAsyncEnumerable())
			.Concat();

	public async IAsyncEnumerable<Guid> QueryPromotionIdsAsync(
			Guid brandId,
		string promotionName,
		string? promotionType,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var queryStrings = HttpUtility.ParseQueryString(string.Empty);
		if (!string.IsNullOrEmpty(promotionType))
			queryStrings.Add("promotionType", promotionType);

		var response = await _httpClient.GetAsync(
				$"/api/promotion-mgr/ids/{brandId}/{WebUtility.UrlEncode(promotionName)}?{queryStrings}",
				cancellationToken)
			.ConfigureAwait(false);

		_ = response.EnsureSuccessStatusCode();

		var promotionIds = await response.Content
			.ReadFromJsonAsync<IEnumerable<Guid>>(_JsonSerializerOptions, cancellationToken)
			.ConfigureAwait(false);

		foreach (var id in promotionIds ?? Array.Empty<Guid>())
			yield return id;
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
