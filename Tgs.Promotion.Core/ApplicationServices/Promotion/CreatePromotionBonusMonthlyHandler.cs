using Microsoft.EntityFrameworkCore;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public class
	CreatePromotionBonusMonthlyHandler : IGetService<CreatePromotionBonusMonthlyRequest,
		MessageResponse>
{
	private readonly IPromotionRepository _promotionRepository;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly Func<string, IDistribution> _distributionFunc;

	public CreatePromotionBonusMonthlyHandler(
		IPromotionRepository promotionRepository,
		IBrandServiceClient brandServiceClient,
		Func<string, IDistribution> distributionFunc)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_distributionFunc = distributionFunc ?? throw new ArgumentNullException(nameof(distributionFunc));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusMonthlyRequest request,
		CancellationToken cancellationToken)
	{
		var timeSpan = TimeSpan.FromMinutes(request.TimeSpanMinutes);

		var todayWithTimeZone = DateTimeOffset.UtcNow.ToOffset(timeSpan);

		var now = new DateTimeOffset(todayWithTimeZone.Year,
			todayWithTimeZone.Month,
			todayWithTimeZone.Day,
			0,
			0,
			0,
			todayWithTimeZone.Offset);

		var previousFirstDayOfMonth = now.AddMonths(-1).GetFirstDayOfMonth();
		var previousLastDayOfMonth = now.AddMonths(-1).GetLastDayOfMonth();

		var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(request.TimeSpanMinutes, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var promotions = await _promotionRepository.GetPromotionQueries()
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Monthly")
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.From >= previousFirstDayOfMonth.UtcDateTime && x.From <= previousLastDayOfMonth.UtcDateTime ||
				x.To >= previousFirstDayOfMonth.UtcDateTime && x.To <= previousLastDayOfMonth.UtcDateTime ||
				x.From < previousFirstDayOfMonth.UtcDateTime && x.To > previousLastDayOfMonth.UtcDateTime
			)
			.Where(x => !x.PromotionBonus.Any(pb => pb.PromotionId == x.Id &&
				pb.From >= (x.From > previousFirstDayOfMonth.UtcDateTime
					? x.From
					: previousFirstDayOfMonth.UtcDateTime) &&
				pb.To <= (x.To < previousLastDayOfMonth.UtcDateTime ? x.To : previousLastDayOfMonth.UtcDateTime)))
			.ToArrayAsync(cancellationToken);

		await promotions.ToAsyncEnumerable()
			.ForEachAwaitAsync(async x =>
			{
				var from = x.From > previousFirstDayOfMonth ? x.From : previousFirstDayOfMonth;
				var to = x.To < previousLastDayOfMonth ? x.To : previousLastDayOfMonth;

				var distributionService = _distributionFunc.Invoke(x.PromotionType);
				var distribution = distributionService.SetPromotion(x, from, to);
				_ = await distribution.SetConditionDataAsync(cancellationToken).ConfigureAwait(false);
				_ = await distribution.CalculateBonusAmountAsync(cancellationToken).ConfigureAwait(false);
				_ = await distribution.DistributeBonusAsync(cancellationToken).ConfigureAwait(false);
			}, cancellationToken: cancellationToken);

		return new() { Valid = true };
	}
}
