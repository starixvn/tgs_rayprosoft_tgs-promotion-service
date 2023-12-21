using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Tgs.Promotion.Domain.Entities;
using Tgs.SerialNumberGenerator;

namespace Tgs.Promotion.Repository;

public class PromotionRepository : IPromotionRepository
{
	private static readonly JsonSerializerOptions _SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		ReferenceHandler = ReferenceHandler.IgnoreCycles
	};

	private readonly TgsContext _dbContext;
	private readonly ITgsService<PromotionRow> _promotionService;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public PromotionRepository(
		ITgsService<PromotionRow> promotionService,
		TgsContext dbContext,
		ISerialNumberGenerator serialNumberGenerator)
	{
		_promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async ValueTask CreatePromotionDistributeAsync(
		PromotionDistributeInfo createInfo,
		string? distributor,
		CancellationToken cancellationToken = default)
	{
		_ = await _dbContext.PromotionDistributes.AddAsync(new PromotionDistribute
		{
			Id = createInfo.Id,
			BrandId = createInfo.BrandId,
			PromotionId = createInfo.PromotionId,
			PlanId = createInfo.PlanId,
			PlayerCode = createInfo.PlayerCode,
			BonusAmount = createInfo.BonusAmount,
			Bucket = createInfo.Bucket,
			ReferenceId = createInfo.ReferenceId,
			Distributor = distributor,
			CreatedOn = DateTime.UtcNow,
			SentOn = createInfo.SentOn?.ToUniversalTime()
		}, cancellationToken).ConfigureAwait(false);

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask<PromotionDistributePlanInfo> CreatePromotionDistributePlanAsync(
		Guid promotionId,
		long conditionHash,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false)
			?? throw new InvalidOperationException($"Promotion with id {promotionId} not found");

		var plan = await _dbContext.PromotionDistributePlans
			.SingleOrDefaultAsync(
				row => row.PromotionId == promotionId && row.ConditionHash == conditionHash,
				cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (plan is null)
		{
			var createId = await _serialNumberGenerator
				.GenerateAsync(cancellationToken)
				.ConfigureAwait(false);

			var promotionData = new
			{
				promotion.Id,
				promotion.BrandId,
				promotion.From,
				promotion.To,
				Status = (PromotionStatus)promotion.Status,
				promotion.Name,
				promotion.AutoSentPlayerBonus,
				promotion.PromotionType,
				promotion.BonusAmountLimit,
				promotion.AmountUsed,
				Unlimited = promotion.Unlimited == 1,
				GameType = (GameType?)promotion.GameType,
				promotion.Percentage,
				promotion.TotalAmountLimit,
				promotion.TurnoverMultiple,
				GameProviderIds = promotion.PromotionGameProviders.Select(g => g.GameProviderId).ToArray(),
				VipLevels = promotion.PromotionVipLevels.Select(v => v.Level).OrderBy(level => level).ToArray(),
				promotion.CreatedOn,
				promotion.UpdatedOn,
				promotion.CreatedByUsername,
				promotion.UpdatedByUsername,
				promotion.SubSubject,
				promotion.Content,
				promotion.Terms,
				promotion.DesktopBanner,
				promotion.MobileBanner,
				promotion.Localization,
				Number = promotion.DisplayOrder,
				promotion.CampaignDetail
			};

			_ = await _dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
			{
				Id = createId,
				BrandId = promotion.BrandId,
				PromotionId = promotionId,
				ConditionHash = conditionHash,
				Detail = JsonSerializer.Serialize(promotionData, _SerializerOptions)
			}, cancellationToken).ConfigureAwait(false);

			_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

			return new PromotionDistributePlanInfo(
				createId,
				promotion.BrandId,
				promotion.Id,
				conditionHash);
		}

		return new PromotionDistributePlanInfo(
			plan.Id,
			plan.BrandId,
			plan.Id,
			plan.ConditionHash);
	}

	public async ValueTask CreatePromotionEventAsync(
		Guid eventId,
		Guid promotionId,
		PromotionEventAction action,
		JsonDocument changeSets,
		string? modifier,
		CancellationToken cancellationToken = default)
	{
		_ = await _dbContext.PromotionEventSources.AddAsync(
			new PromotionEventSource
			{
				Id = eventId,
				PromotionId = promotionId,
				Action = action.ToString(),
				ChangeSets = JsonSerializer.Serialize(changeSets),
				ModifyUser = modifier,
				CreatedOn = DateTime.UtcNow
			},
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask<PromotionDistributePlanInfo?> FindPromotionDistributePlanAsync(
		Guid planId,
		CancellationToken cancellationToken = default)
		=> _dbContext.PromotionDistributePlans
			.FindAsync(new object?[] { planId }, cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.Where(plan => plan is not null)
			.Select(plan => plan!)
			.Select(plan => new PromotionDistributePlanInfo(
				plan.Id,
				plan.BrandId,
				plan.PromotionId,
				plan.ConditionHash))
			.SingleOrDefaultAsync(cancellationToken: cancellationToken);

	public async ValueTask<PromotionSnapshot?> GetCurrentPromotionSnapshotAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var promotionRow = await _dbContext.PromotionRows.FindAsync(
				new object[] { promotionId },
				cancellationToken)
			.ConfigureAwait(false);

		return promotionRow is null
			? null
			: new PromotionSnapshot(
				promotionRow.Id,
				promotionRow.Name,
				promotionRow.BrandId,
				promotionRow.From,
				promotionRow.To,
				(PromotionStatus)promotionRow.Status,
				promotionRow.PromotionType,
				promotionRow.SettlementIntervalType,
				promotionRow.AutoSentPlayerBonus,
				promotionRow.BonusAmountLimit,
				promotionRow.Unlimited,
				(GameType?)promotionRow.GameType,
				promotionRow.Percentage,
				promotionRow.TotalAmountLimit,
				promotionRow.TurnoverMultiple,
				JsonDocument.Parse(promotionRow.CampaignDetail),
				promotionRow.Content,
				promotionRow.Localization,
				promotionRow.DesktopBanner,
				promotionRow.MobileBanner,
				promotionRow.SubSubject,
				promotionRow.Terms,
				promotionRow.CreatedByUsername,
				promotionRow.CreatedOn,
				promotionRow.UpdatedByUsername,
				promotionRow.UpdatedOn,
				promotionRow.AmountUsed,
				promotionRow.AmountUsedSnapshotId,
				promotionRow.DisplayOrder,
				promotionRow.VipLevelId,
				promotionRow.SnapshotEventId);
	}

	public async ValueTask<EventInfo?> GetEventDetailAsync(Guid promotionId,
		Guid eventId,
		CancellationToken cancellationToken = default)
	{
		var eventSource = await _dbContext.PromotionEventSources
			.FirstOrDefaultAsync(
				p => p.Id == eventId && p.PromotionId == promotionId,
				cancellationToken)
			.ConfigureAwait(false);

		return eventSource is null
			? null
			: new EventInfo(
				eventSource.Id,
				eventSource.PromotionId,
				Enum.Parse<PromotionEventAction>(eventSource.Action),
				JsonDocument.Parse(eventSource.ChangeSets),
				eventSource.ModifyUser,
				eventSource.CreatedOn);
	}

	public async ValueTask<PromotionRow?> GetPromotionByIdAsync(Guid promotionId)
		=> await _promotionService.GetQueries().FirstOrDefaultAsync(x => x.Id == promotionId).ConfigureAwait(false);

	public async ValueTask<PromotionContentDetail?> GetPromotionContentAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return promotion is null
			? null
			: new PromotionContentDetail(
				promotion.SubSubject,
				promotion.DesktopBanner,
				promotion.MobileBanner,
				promotion.Content,
				promotion.Terms,
				JsonDocument.Parse(string.IsNullOrWhiteSpace(promotion.Localization)
					? "{}"
					: promotion.Localization));
	}

	public async ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return promotion is null
			? null
			: new PromotionMainInfo(
				promotion.Id,
				promotion.BrandId,
				promotion.Name,
				(promotion.From, promotion.To),
				promotion.PromotionType,
				promotion.Unlimited == 1 ? null : promotion.TotalAmountLimit,
				promotion.AutoSentPlayerBonus);
	}

	public IQueryable<PromotionRow> GetPromotionQueries(
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>? preQuery,
		bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionService.GetQueries()).AsTracking()
			: preQuery(_promotionService.GetQueries()).AsNoTracking();
	}

	public async ValueTask<PromotionRule?> GetPromotionRuleAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var promotionVipLevels = await _dbContext.PromotionVipLevels
			.Where(vip => vip.PromotionId == promotionId)
			.Select(vip => vip.Level)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		return promotion is null
			? null
			: new PromotionRule(
			JsonDocument.Parse(promotion.CampaignDetail),
			promotion.BonusAmountLimit,
			promotion.Percentage,
			promotion.TurnoverMultiple,
			(GameType?)promotion.GameType,
			promotionVipLevels);
	}

	public async ValueTask<decimal> GetPromotionUsedAmountAsync(
		Guid promotionId,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return promotion?.AmountUsed ?? 0m;
	}

	public async ValueTask InsertPromotionDistributeLogAsync(PromotionDistributeLog log, CancellationToken cancellationToken = default)
	{
		_ = await _dbContext.PromotionDistributeLogs.AddAsync(
			new Domain.Entities.PromotionDistributeLog
			{
				Id = log.Id,
				DistributeId = log.DistributeId,
				Action = log.Action.ToString(),
				ChangeSets = JsonSerializer.Serialize(log.ChangeSets),
				CreatedOn = DateTime.UtcNow
			},
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public IAsyncEnumerable<PromotionView> ListPromotionsAsync(
		Guid brandId,
		CancellationToken cancellationToken = default)
	{
		var current = DateTime.UtcNow;

		var promotions = (from p in _dbContext.PromotionRows
						  where p.BrandId == brandId
							  && p.Status != (short)PromotionStatus.Archived
							  && p.From <= current
							  && p.To > current
						  select new
						  {
							  p.Id,
							  p.Name,
							  p.From,
							  p.To,
							  p.PromotionType,
							  p.TotalAmountLimit,
							  p.AmountUsed,
							  p.SubSubject,
							  p.Content,
							  p.DesktopBanner,
							  p.MobileBanner,
							  p.Terms,
							  p.Localization,
							  p.CampaignDetail,
							  p.Status,
							  p.DisplayOrder
						  }).ToArray();

		var promotionVipLevels = _dbContext.PromotionVipLevels
			.Where(vip => promotions
				.Select(p => p.Id)
				.ToArray()
				.Contains(vip.PromotionId))
			.ToLookup(
				vip => vip.PromotionId,
				vip => vip.Level);

		return promotions
			.Select(p => new PromotionView(
				p.Id,
				p.Name,
				(p.From, p.To),
				(PromotionStatus)p.Status,
				p.PromotionType,
				p.TotalAmountLimit,
				p.AmountUsed,
				promotionVipLevels[p.Id],
				p.SubSubject,
				p.DesktopBanner,
				p.MobileBanner,
				p.Content,
				p.Terms,
				JsonDocument.Parse(string.IsNullOrWhiteSpace(p.Localization)
					? "{}"
					: p.Localization),
				JsonDocument.Parse(p.CampaignDetail),
				p.DisplayOrder))
			.ToAsyncEnumerable();
	}

	public async IAsyncEnumerable<PromotionView> ListPromotionsByFiltersAsync(
		Guid brandId,
		During? during,
		string? name,
		IEnumerable<string> promotionTypes,
		PromotionStatus? status,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var types = promotionTypes.ToArray();
		var query = _dbContext.PromotionRows.Where(p =>
			p.BrandId == brandId && p.Status != (short)PromotionStatus.Archived);

		if (during is not null)
			query = query.Where(p => p.From >= during.From && p.To < during.To);

		if (!string.IsNullOrEmpty(name))
			query = query.Where(p => EF.Functions.ILike(p.Name, $"%{name}%"));

		if (status is not null)
			query = query.Where(p => p.Status == (short)status);

		if (types.Length > 0)
			query = query.Where(p => types.Contains(p.PromotionType));

		var promotions = await query.Select(
			p => new
			{
				p.Id,
				p.Name,
				p.From,
				p.To,
				p.PromotionType,
				p.Unlimited,
				p.TotalAmountLimit,
				p.AmountUsed,
				p.SubSubject,
				p.Content,
				p.DesktopBanner,
				p.MobileBanner,
				p.Terms,
				p.Localization,
				p.CampaignDetail,
				p.Status,
				p.DisplayOrder
			}).ToArrayAsync(cancellationToken).ConfigureAwait(false);

		var promotionVipLevels = _dbContext.PromotionVipLevels
			.Where(vip => promotions
				.Select(p => p.Id)
				.Contains(vip.PromotionId))
			.ToLookup(
				vip => vip.PromotionId,
				vip => vip.Level);

		foreach (var promotion in promotions)
			yield return new PromotionView(
				promotion.Id,
				promotion.Name,
				(promotion.From, promotion.To),
				(PromotionStatus)promotion.Status,
				promotion.PromotionType,
				promotion.Unlimited == 1 ? null : promotion.TotalAmountLimit,
				promotion.AmountUsed,
				promotionVipLevels[promotion.Id],
				promotion.SubSubject,
				promotion.DesktopBanner,
				promotion.MobileBanner,
				promotion.Content,
				promotion.Terms,
				JsonDocument.Parse(string.IsNullOrWhiteSpace(promotion.Localization)
					? "{}"
					: promotion.Localization),
				JsonDocument.Parse(promotion.CampaignDetail),
				promotion.DisplayOrder);
	}

	public async IAsyncEnumerable<EventInfo> QueryPromotionEventsAsync(
		Guid promotionId,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var events = await _dbContext.PromotionEventSources
			.Where(p => p.PromotionId == promotionId
				&& p.CreatedOn >= DateTime.UtcNow.AddDays(-90))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var evt in events)
			yield return new EventInfo(
				evt.Id,
				evt.PromotionId,
				Enum.Parse<PromotionEventAction>(evt.Action),
				JsonDocument.Parse(evt.ChangeSets),
				evt.ModifyUser,
				evt.CreatedOn);
	}

	public async IAsyncEnumerable<PromotionMainInfo> QueryPromotionsAsync(
		Guid brandId,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var promotionRows = await _dbContext.PromotionRows
			.Where(promotion => promotion.BrandId == brandId
				&& promotion.Status != (short)PromotionStatus.Archived)
			.Select(promotion => new
			{
				promotion.Id,
				promotion.BrandId,
				promotion.Name,
				promotion.From,
				promotion.To,
				promotion.PromotionType,
				promotion.Unlimited,
				promotion.TotalAmountLimit,
				promotion.AutoSentPlayerBonus
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var promotion in promotionRows)
			yield return new PromotionMainInfo(
				promotion.Id,
				promotion.BrandId,
				promotion.Name,
				(promotion.From, promotion.To),
				promotion.PromotionType,
				promotion.Unlimited == 1 ? null : promotion.TotalAmountLimit,
				promotion.AutoSentPlayerBonus);
	}

	public async IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(
		Guid brandId,
		string promotionType,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var promotionRows = await _dbContext.PromotionRows
			.Where(promotion => promotion.BrandId == brandId
								&& promotion.Status != (short)PromotionStatus.Archived
								&& promotion.PromotionType == promotionType)
			.Select(promotion => new
			{
				promotion.Id,
				promotion.BrandId,
				promotion.Name,
				promotion.From,
				promotion.To,
				promotion.PromotionType,
				promotion.Unlimited,
				promotion.TotalAmountLimit,
				promotion.AutoSentPlayerBonus
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		foreach (var promotion in promotionRows)
			yield return new PromotionMainInfo(
				promotion.Id,
				promotion.BrandId,
				promotion.Name,
				(promotion.From, promotion.To),
				promotion.PromotionType,
				promotion.Unlimited == 1 ? null : promotion.TotalAmountLimit,
				promotion.AutoSentPlayerBonus);
	}

	public IAsyncEnumerable<PromotionMainInfo> QueryPromotionsByPromotionTypeAsync(
		string promotionType,
		CancellationToken cancellationToken = default)
		=> _dbContext.PromotionRows.AsQueryable()
			.Where(p => p.PromotionType == promotionType
				&& p.Status == (short)PromotionStatus.Enabled)
			.Select(p => new
			{
				p.Id,
				p.BrandId,
				p.Name,
				p.From,
				p.To,
				p.PromotionType,
				p.Unlimited,
				p.TotalAmountLimit,
				p.AutoSentPlayerBonus
			})
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ToAsyncEnumerable()
			.Select(arr => arr.ToAsyncEnumerable())
			.Concat()
			.Select(p => new PromotionMainInfo(
				p.Id,
				p.BrandId,
				p.Name,
				(p.From, p.To),
				p.PromotionType,
				p.Unlimited == 1 ? null : p.TotalAmountLimit,
				p.AutoSentPlayerBonus));

	public async ValueTask SetPromotionGameProvidersAsync(
		Guid promotionId,
		IEnumerable<Guid> gameProviderIds,
		CancellationToken cancellationToken = default)
	{
		var gpArray = gameProviderIds.ToArray();

		var promotionGameProviders = await _dbContext.GameProviders
			.Where(x => gpArray.Contains(x.Id))
			.Select(row => row.Id)
			.Distinct()
			.Select(id => new PromotionGameProvider
			{
				PromotionId = promotionId,
				GameProviderId = id
			})
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var oldSettings = await _dbContext.PromotionGameProviders
			.Where(row => row.PromotionId == promotionId)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		_dbContext.PromotionGameProviders.RemoveRange(oldSettings);
		await _dbContext.PromotionGameProviders
			.AddRangeAsync(promotionGameProviders)
			.ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SetPromotionVipLevelsAsync(
		Guid promotionId,
		IEnumerable<int> vipLevels,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		var vipLevelRows = await _dbContext.VipLevels
			.Where(row => row.BrandId == promotion.BrandId
				&& vipLevels.Contains(row.Level))
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var originVipLevels = await _dbContext.PromotionVipLevels
			.Where(row => row.PromotionId == promotionId)
			.ToArrayAsync(cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		_dbContext.PromotionVipLevels.RemoveRange(originVipLevels);

		await _dbContext.PromotionVipLevels.AddRangeAsync(
			vipLevelRows.Select(
				x => new PromotionVipLevel
				{
					PromotionId = promotion.Id,
					VipLevelId = x.Id,
					Level = x.Level
				}),
				cancellationToken).ConfigureAwait(false);

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdatePromotionDistributeAsync(
		Guid distributeId,
		DateTime sentOn,
		string? distributor,
		CancellationToken cancellationToken = default)
	{
		var distribute = await _dbContext.PromotionDistributes.FindAsync(
			new object?[] { distributeId },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (distribute is null)
			return;

		distribute.SentOn = sentOn.ToUniversalTime();
		distribute.Distributor = distributor;

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdatePromotionSnapshotAsync(
		PromotionSnapshot snapshot,
		Guid snapshotId,
		DateTime updatedOn,
		CancellationToken cancellationToken = default)
	{
		var dbSnapshot = await _dbContext.PromotionRows
							 .FindAsync(new object[] { snapshot.Id }, cancellationToken)
							 .ConfigureAwait(false)
						 ?? (await _dbContext.PromotionRows
							 .AddAsync(new PromotionRow { Id = snapshot.Id }, cancellationToken)
							 .ConfigureAwait(false)).Entity;

		if (dbSnapshot.SnapshotEventId is null || dbSnapshot.SnapshotEventId.Value.CompareTo(snapshotId) < 0)
		{
			dbSnapshot.BrandId = snapshot.BrandId;
			dbSnapshot.Name = snapshot.Name;
			dbSnapshot.From = snapshot.From;
			dbSnapshot.To = snapshot.To;
			dbSnapshot.Status = (short)snapshot.Status;
			dbSnapshot.PromotionType = snapshot.PromotionType;
			dbSnapshot.SettlementIntervalType = snapshot.SettlementIntervalType;
			dbSnapshot.AutoSentPlayerBonus = snapshot.AutoSentPlayerBonus;
			dbSnapshot.BonusAmountLimit = snapshot.BonusAmountLimit;
			dbSnapshot.Unlimited = snapshot.Unlimited;
			dbSnapshot.GameType = (short?)snapshot.GameType;
			dbSnapshot.Percentage = snapshot.Percentage;
			dbSnapshot.TotalAmountLimit = snapshot.TotalAmountLimit;
			dbSnapshot.TurnoverMultiple = snapshot.TurnoverMultiple;
			dbSnapshot.CampaignDetail = JsonSerializer.Serialize(snapshot.CampaignDetail, _SerializerOptions);
			dbSnapshot.Content = snapshot.Content;
			dbSnapshot.Localization = snapshot.Localization;
			dbSnapshot.DesktopBanner = snapshot.DesktopBanner;
			dbSnapshot.MobileBanner = snapshot.MobileBanner;
			dbSnapshot.SubSubject = snapshot.SubSubject;
			dbSnapshot.Terms = snapshot.Terms;
			dbSnapshot.CreatedByUsername = snapshot.CreatedByUsername;
			dbSnapshot.CreatedOn = snapshot.CreatedOn;
			dbSnapshot.UpdatedByUsername = snapshot.UpdatedByUsername;
			dbSnapshot.UpdatedOn = snapshot.UpdatedOn;
			dbSnapshot.SnapshotEventId = snapshotId;
			dbSnapshot.AmountUsed = snapshot.AmountUsed;
			dbSnapshot.AmountUsedSnapshotId = snapshot.AmountUsedSnapshotId;
			dbSnapshot.DisplayOrder = snapshot.DisplayOrder;
			dbSnapshot.VipLevelId = snapshot.VipLevelId;
		}

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<PromotionDistributePlanDetailInfo> FindPromotionDistributePlanDetailAsync(Guid planId,
		CancellationToken cancellationToken = default)
	{
		return await _dbContext.PromotionDistributePlans.Where(x => x.Id == planId)
			.Select(x => new PromotionDistributePlanDetailInfo(x.Id, x.Detail))
			.FirstAsync(cancellationToken);
	}
}
