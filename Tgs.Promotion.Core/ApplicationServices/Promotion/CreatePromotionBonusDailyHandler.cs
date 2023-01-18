using Microsoft.EntityFrameworkCore;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionBonus;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices.Promotion;

public class
	CreatePromotionBonusDailyQueryHandler : IGetService<CreatePromotionBonusDailyRequest, MessageResponse>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly Func<string, IDistribution> _distributionFunc;
	private readonly IPromotionRepository _promotionRepository;

	public CreatePromotionBonusDailyQueryHandler(
		IPromotionRepository promotionRepository,
		IBrandServiceClient brandServiceClient,
		Func<string, IDistribution> distributionFunc)
	{
		_promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_distributionFunc = distributionFunc ?? throw new ArgumentNullException(nameof(distributionFunc));
	}

	public async ValueTask<MessageResponse> GetAsync(
		CreatePromotionBonusDailyRequest request,
		CancellationToken cancellationToken)
	{
		var timeSpan = TimeSpan.FromMinutes(request.TimeSpanMinutes);

		var previousDay = DateTimeOffset.Now.ToOffset(timeSpan).AddDays(-1);

		var startDate = new DateTimeOffset(previousDay.Year,
			previousDay.Month,
			previousDay.Day,
			0,
			0,
			0,
			previousDay.Offset);

		if (request.Date != null)
		{
			var result = new DateTimeOffset(request.Date.Value.ToDateTime(TimeOnly.MinValue), timeSpan);

			startDate = new(result.Year,
				result.Month,
				result.Day,
				0,
				0,
				0,
				result.Offset);
		}

		var brandIds = await _brandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(request.TimeSpanMinutes, cancellationToken).ToArrayAsync(cancellationToken);

		var promotions = await _promotionRepository.GetPromotionQueries()
			.Where(x => x.Status == (short)PromotionStatus.Enabled)
			.Where(x => x.SettlementIntervalType == "Daily")
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.From <= startDate.UtcDateTime && x.To >= startDate.UtcDateTime)
			.Where(x => !x.PromotionBonus.Any(pb => pb.PromotionId == x.Id && pb.From == (x.From > startDate.UtcDateTime ? x.From : startDate.UtcDateTime) &&
				pb.To == (x.To < startDate.UtcDateTime ? x.To : startDate.UtcDateTime)))
			.ToArrayAsync(cancellationToken);

		await promotions.ToAsyncEnumerable()
			.ForEachAwaitAsync(async x =>
			{
				var distributionService = _distributionFunc.Invoke(x.PromotionType);
				var distribution = distributionService.SetPromotion(x, startDate, startDate);
				_ = await distribution.SetConditionDataAsync(cancellationToken).ConfigureAwait(false);
				_ = await distribution.CalculateBonusAmountAsync(cancellationToken).ConfigureAwait(false);
				_ = await distribution.DistributeBonusAsync(cancellationToken).ConfigureAwait(false);
			}, cancellationToken: cancellationToken);

		return new() { Valid = true };
	}
}
