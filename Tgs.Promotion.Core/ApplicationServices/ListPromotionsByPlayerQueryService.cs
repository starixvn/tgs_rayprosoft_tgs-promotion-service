using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Tgs.BrandCertificate.Clients;
using Tgs.PlayerService.Client;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class ListPromotionsByPlayerQueryService : IQueryService<ListPromotionsByPlayerQuery, PromotionView>
{
	private readonly IGetService<GetBrandVipLevelMapQuery, IImmutableDictionary<Guid, VipLevelInfo>> _getVipMapService;
	private readonly IPlayerServiceClient _playerServiceClient;
	private readonly IPromotionCheckerFactory _promotionCheckerFactory;
	private readonly IQueryService<ListPromotionsByBrandQuery, PromotionView> _promotionQueryService;

	public ListPromotionsByPlayerQueryService(
		IPlayerServiceClient playerServiceClient,
		IGetService<GetBrandVipLevelMapQuery, IImmutableDictionary<Guid, VipLevelInfo>> getVipMapService,
		IQueryService<ListPromotionsByBrandQuery, PromotionView> promotionQueryService,
		IPromotionCheckerFactory promotionCheckerFactory)
	{
		_promotionQueryService = promotionQueryService ?? throw new ArgumentNullException(nameof(promotionQueryService));
		_promotionCheckerFactory = promotionCheckerFactory ?? throw new ArgumentNullException(nameof(promotionCheckerFactory));
		_playerServiceClient = playerServiceClient ?? throw new ArgumentNullException(nameof(playerServiceClient));
		_getVipMapService = getVipMapService ?? throw new ArgumentNullException(nameof(getVipMapService));
	}

	public async IAsyncEnumerable<PromotionView> QueryAsync(
		ListPromotionsByPlayerQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var player = await _playerServiceClient
			.GetPlayerInfoAsync(query.PlayerCode, cancellationToken)
			.ConfigureAwait(false);

		if (player is null)
			yield break;

		var vipDict = await _getVipMapService
			.GetAsync(new GetBrandVipLevelMapQuery(player.BrandId), cancellationToken)
			.ConfigureAwait(false);

		var playerVipLevel = vipDict[player.VipLevelId!.Value].Level;

		var promotions = _promotionQueryService
			.QueryAsync(new ListPromotionsByBrandQuery(player.BrandId), cancellationToken)
			.Where(p => !p.VipLevels.Any()
				|| p.VipLevels.Contains(playerVipLevel));

		await foreach (var promotion in promotions)
		{
			var checker = _promotionCheckerFactory.GetPromotionChecker(promotion.PromotionType);

			if (await checker.CheckAsync(
				query.PlayerCode,
				new PromotionMainInfo(
					promotion.Id,
					query.BrandId,
					promotion.Name,
					promotion.EventDuring,
					promotion.PromotionType,
					promotion.PromotionLimitAmount,
					true))
				.ConfigureAwait(false))
				yield return promotion;
		}
	}
}
