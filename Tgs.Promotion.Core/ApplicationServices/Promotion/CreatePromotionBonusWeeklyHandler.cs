using Microsoft.EntityFrameworkCore;
using Tgs.BrandCertificate.Clients;
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
	private readonly IBrandServiceClient _brandServiceClient;

	public CreatePromotionBonusWeeklyHandler(
		IPromotionRepository promotionRepository,
		IBrandServiceClient brandServiceClient,
		Func<string, IDistribution> distributionFunc)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_distributionFunc = distributionFunc ?? throw new ArgumentNullException(nameof(distributionFunc));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusWeeklyRequest request,
		CancellationToken cancellationToken)
	{
		var timeSpan = new TimeSpan(0, request.TimeSpanMinutes, 0);

		var now = new DateTimeOffset(DateTimeOffset.Now.Year,
			DateTimeOffset.Now.Month,
			DateTimeOffset.Now.Day,
			0,
			0,
			0,
			timeSpan);

		var previousMonday = now.AddDays(-7).GetMonday();
		var previousSunday = now.AddDays(-7).GetSunday();

		if (request.From != null && request.To != null)
		{
			var resultFrom = new DateTimeOffset(request.From.Value.ToDateTime(TimeOnly.MinValue), timeSpan);
			var resultTo = new DateTimeOffset(request.To.Value.ToDateTime(TimeOnly.MinValue), timeSpan);

			previousMonday = new(resultFrom.Year,
				resultFrom.Month,
				resultFrom.Day,
				0,
				0,
				0,
				resultFrom.Offset);

			previousSunday = new(resultTo.Year,
				resultTo.Month,
				resultTo.Day,
				0,
				0,
				0,
				resultTo.Offset);
		}

		var brandIds = await _brandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(request.TimeSpanMinutes, cancellationToken).ToArrayAsync(cancellationToken);

		var promotions = await _promotionRepository.GetPromotionQueries()
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Weekly")
			.Where(x => brandIds.Contains(x.BrandId))
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
