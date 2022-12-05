using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public class CreatePromotionBonusWeeklyHandler
	: IGetService<CreatePromotionBonusWeeklyRequest, MessageResponse>
{
	private readonly Func<string, IDistribution> _distributionFunc;
	private readonly IPromotionRepository _promotionRepository;

	public CreatePromotionBonusWeeklyHandler(
		IPromotionRepository promotionRepository,
		Func<string, IDistribution> distributionFunc)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_distributionFunc = distributionFunc;
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusWeeklyRequest request,
		CancellationToken cancellationToken)
	{
		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			new(8, 0, 0));

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		if (!string.IsNullOrEmpty(request.From) &&
			DateTimeOffset.TryParse($"{request.From} 00:00 +08:00", out var resultFrom))
		{
			previousMonday = new(resultFrom.Year,
				resultFrom.Month,
				resultFrom.Day,
				0,
				0,
				0,
				resultFrom.Offset);
		}

		if (!string.IsNullOrEmpty(request.To) &&
			DateTimeOffset.TryParse($"{request.To} 00:00 +08:00", out var resultTo))
		{
			previousSunday = new(resultTo.Year,
				resultTo.Month,
				resultTo.Day,
				0,
				0,
				0,
				resultTo.Offset);
		}

		var promotions = await _promotionRepository.GetPromotionQueries()
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Weekly")
			.Where(x => x.From >= previousMonday.UtcDateTime && x.From <= previousSunday.UtcDateTime ||
				x.To >= previousMonday.UtcDateTime && x.To <= previousSunday.UtcDateTime ||
				x.From < previousMonday.UtcDateTime && x.To > previousSunday.UtcDateTime
			)
			.Where(x => !x.PromotionBonus.Any(pb => pb.PromotionId == x.Id && pb.From == (x.From > previousMonday.UtcDateTime ? x.From : previousMonday.UtcDateTime) &&
				pb.To == (x.To < previousSunday.UtcDateTime ? x.To : previousSunday.UtcDateTime)))
			.ToArrayAsync(cancellationToken);

		await promotions.ToAsyncEnumerable()
				.ForEachAwaitAsync(async x =>
				{
					var from = x.From > previousMonday ? x.From : previousMonday;
					var to = x.To < previousSunday ? x.To : previousSunday;

					var distributionService = _distributionFunc.Invoke(x.PromotionType);
					var distribution = distributionService.SetPromotion(x, from, to);
					_ = await distribution.SetConditionDataAsync(cancellationToken).ConfigureAwait(false);
					_ = await distribution.CalculateBonusAmountAsync(cancellationToken).ConfigureAwait(false);
					_ = await distribution.DistributeBonusAsync(cancellationToken).ConfigureAwait(false);
				}, cancellationToken: cancellationToken);

		return new MessageResponse { Valid = true };
	}
}
