using System.Runtime.CompilerServices;
using Tgs.Promotion.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetValidPromotionsByPlayerQueryService : IQueryService<GetValidPromotionsByPlayerQuery, PromotionModel>
{
	private readonly IPlayerRepository _playerService;
	private readonly IPromotionCheckerFactory _promotionCheckerFactory;
	private readonly IQueryService<GetValidPromotionsQuery, PromotionModel> _promotionQueryService;

	public GetValidPromotionsByPlayerQueryService(
		IQueryService<GetValidPromotionsQuery, PromotionModel> promotionQueryService,
		IPlayerRepository playerService,
		IPromotionCheckerFactory promotionCheckerFactory)
	{
		_promotionQueryService =
			promotionQueryService ?? throw new ArgumentNullException(nameof(promotionQueryService));
		_playerService = playerService ?? throw new ArgumentNullException(nameof(playerService));
		_promotionCheckerFactory =
			promotionCheckerFactory ?? throw new ArgumentNullException(nameof(promotionCheckerFactory));
	}

	public async IAsyncEnumerable<PromotionModel> QueryAsync(
		GetValidPromotionsByPlayerQuery query,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var player = await _playerService
			.GetPlayerAsync(x => x.PlayerCode == query.PlayerCode)
			.ConfigureAwait(false);

		var promotions = GetPromotionsAsync(
			query.BrandId,
			query.PlayerCode,
			player?.Vip?.Level,
			cancellationToken);

		await foreach (var promotion in promotions)
			yield return promotion;
	}

	private async IAsyncEnumerable<PromotionModel> GetPromotionsAsync(
		Guid brandId,
		string playerCode,
		int? vipLevel,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		var promotions = _promotionQueryService
			.QueryAsync(
				new(brandId),
				cancellationToken)
			.Where(row => row.VipLevels.Length == 0
				|| vipLevel is null
				|| row.VipLevels.Contains(vipLevel.Value));

		await foreach (var promotion in promotions)
		{
			var checker = _promotionCheckerFactory.GetPromotionChecker(promotion.PromotionType);

			if (await checker.CheckAsync(
				playerCode,
				new PromotionMainInfo(
					promotion.Id,
					promotion.BrandId,
					promotion.Name,
					(promotion.From.UtcDateTime, promotion.To.UtcDateTime),
					promotion.PromotionType,
					promotion.TotalAmountLimit,
					promotion.AutoSentPlayerBonus))
				.ConfigureAwait(false))
			{
				yield return promotion;
			}
		}
	}
}
