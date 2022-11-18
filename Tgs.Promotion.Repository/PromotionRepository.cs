using System.Data;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.Promotion;
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
	private readonly ITgsService<GameProvider> _gameProviderService;
	private readonly ITgsService<PromotionRow> _promotionService;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly ITgsService<VipLevel> _vipLevelService;

	public PromotionRepository(
			ITgsService<PromotionRow> promotionService,
			ITgsService<VipLevel> vipLevelService,
			ITgsService<GameProvider> gameProviderService,
			TgsContext dbContext,
			ISerialNumberGenerator serialNumberGenerator)
	{
		_promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
		_vipLevelService = vipLevelService ?? throw new ArgumentNullException(nameof(vipLevelService));
		_gameProviderService = gameProviderService ?? throw new ArgumentNullException(nameof(gameProviderService));
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public ValueTask ChangePromotionStatusAsync(PromotionRow promotion)
		=> _promotionService.UpdateEntityAsync(promotion);

	public async ValueTask<Guid> CreatePromotionAsync(
		CreatePromotionRequest request,
		PromotionType promotionTypeModel)
	{
		var promotionId = await _serialNumberGenerator.GenerateAsync().ConfigureAwait(false);

		var promotion = new PromotionRow
		{
			Id = promotionId,
			BrandId = request.BrandId,
			Name = request.Name,
			From = request.From.UtcDateTime,
			To = request.To.UtcDateTime,
			Status = (short)PromotionStatus.Enabled,
			AutoSentPlayerBonus = request.AutoSentPlayerBonus,
			PromotionType = request.PromotionType,
			BonusAmountLimit = request.BonusAmountLimit,
			SettlementIntervalType = promotionTypeModel.SettlementIntervalType,
			AmountUsed = 0,
			Unlimited = (short)(request.Unlimited ? 1 : 0),
			GameType = (short?)request.GameType,
			Percentage = request.Percentage,
			TotalAmountLimit = request.TotalAmountLimit ?? 0M,
			TurnoverMultiple = request.TurnoverMultiple,
			CreatedByUsername = request.EditorUsername,
			UpdatedByUsername = request.EditorUsername,
			CreatedOn = DateTimeOffset.Now.UtcDateTime,
			UpdatedOn = DateTimeOffset.Now.UtcDateTime,
			SubSubject = request.SubSubject,
			Content = request.Content,
			Terms = request.Terms,
			DesktopBanner = request.DesktopBanner,
			MobileBanner = request.MobileBanner,
			Localization = request.Localization is not null
				? JsonSerializer.Serialize(request.Localization,
					new JsonSerializerOptions { WriteIndented = false })
				: null
		};

		promotion.PromotionGameProviders = GeneratePromotionGameProviders(promotion, request);
		promotion.PromotionVipLevels = GeneratePromotionVipLevels(promotion, request);

		await _promotionService.CreateEntityAsync(promotion);

		return promotionId;
	}

	public async ValueTask<Guid> CreatePromotionAsync(
		CreatePromotionMainInfo createInfo,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotionId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotionsJson = await _dbContext.Settings.Where(x => x.Key == "PromotionTypes").FirstAsync(cancellationToken).ConfigureAwait(false);

		var promotionTypeGroups =
			JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

		var promotionType = promotionTypeGroups!.SelectMany(x => x.Types)
			.First(x => x.Name == createInfo.PromotionType);

		var now = DateTime.UtcNow;

		var promotion = new PromotionRow
		{
			Id = promotionId,
			BrandId = createInfo.BrandId,
			Name = createInfo.Name,
			From = createInfo.EventDuring.From.ToUniversalTime(),
			To = createInfo.EventDuring.To.ToUniversalTime(),
			Status = (short)PromotionStatus.Disabled,
			AutoSentPlayerBonus = createInfo.IsAutoSent,
			PromotionType = createInfo.PromotionType,
			SettlementIntervalType = promotionType.SettlementIntervalType,
			BonusAmountLimit = 0M,
			AmountUsed = 0,
			Unlimited = createInfo.PromotionLimitAmount is null ? (short)1 : (short)0,
			Percentage = 0M,
			TotalAmountLimit = createInfo.PromotionLimitAmount ?? 0M,
			TurnoverMultiple = 0M,
			CampaignDetail = "{}",
			CreatedByUsername = modifier,
			UpdatedByUsername = modifier,
			CreatedOn = now,
			UpdatedOn = now
		};

		_ = await _dbContext.PromotionRows
			.AddAsync(promotion, cancellationToken)
			.ConfigureAwait(false);
		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);

		return promotionId;
	}

	public async ValueTask CreatePromotionDistributeAsync(PromotionDistributeInfo createInfo, string? distributor, CancellationToken cancellationToken = default)
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
			.ConfigureAwait(false);

		if (promotion is null)
			throw new InvalidOperationException($"Promotion with id {promotionId} not found");

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

	public async ValueTask<bool> ExistsAsync(
		Expression<Func<PromotionRow, bool>> predicate)
		=> await _promotionService.GetQueries().AnyAsync(predicate).ConfigureAwait(false);

	public ValueTask<PromotionDistributePlanInfo?> FindPromotionDistributePlanAsync(Guid planId, CancellationToken cancellationToken = default)
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

	public async ValueTask<PromotionRow?> GetPromotionAsync(
			Expression<Func<PromotionRow, bool>> predicate,
		Func<IQueryable<PromotionRow>, IQueryable<PromotionRow>>? preQuery)
	{
		preQuery ??= c => c;

		return await preQuery(_promotionService.GetQueries()).FirstOrDefaultAsync(predicate);
	}

	public async ValueTask<PromotionRow?> GetPromotionAsync(
		Expression<Func<PromotionRow, bool>> predicate) =>
		await _promotionService.GetQueries().FirstOrDefaultAsync(predicate).ConfigureAwait(false);

	public async ValueTask<PromotionRow?> GetPromotionByIdAsync(Guid promotionId)
		=> await _promotionService.GetQueries().FirstOrDefaultAsync(x => x.Id == promotionId).ConfigureAwait(false);

	public async ValueTask<PromotionContentDetail?> GetPromotionContentAsync(Guid promotionId, CancellationToken cancellationToken = default)
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

	public async ValueTask<PromotionMainInfo?> GetPromotionMainInfoAsync(Guid promotionId, CancellationToken cancellationToken = default)
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

	public async ValueTask<PromotionRule?> GetPromotionRuleAsync(Guid promotionId, CancellationToken cancellationToken = default)
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

	public IAsyncEnumerable<PromotionView> ListPromotionsAsync(Guid brandId, CancellationToken cancellationToken = default)
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

	public async ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount)
	{
		const string sql = "select * from dbo.promotion_revert_amount_used(@promotion_id,@amount)";

		var conn = _dbContext.Database.GetDbConnection();
		var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

		return await conn.QueryFirstOrDefaultAsync<bool>(
				sql,
				new Dictionary<string, object>
				{
					{ "@promotion_id", promotionId },
					{ "@amount", amount }
				}, commandType: CommandType.Text, transaction: transaction)
			.ConfigureAwait(false);
	}

	public async ValueTask SetPromotionContentAsync(
		Guid promotionId,
		PromotionContentDetail content,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		promotion.SubSubject = content.SubSubject;
		promotion.DesktopBanner = content.PersonalComputerBanner;
		promotion.MobileBanner = content.MobileBanner;
		promotion.Content = content.Content;
		promotion.Terms = content.Convention;
		promotion.Localization = JsonSerializer.Serialize(content.Localization, _SerializerOptions);
		promotion.UpdatedByUsername = modifier;
		promotion.UpdatedOn = DateTime.UtcNow;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SetPromotionGameProvidersAsync(Guid promotionId, IEnumerable<Guid> gameProviderIds, CancellationToken cancellationToken = default)
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

	public async ValueTask SetPromotionRuleAsync(
		Guid promotionId,
		PromotionRule rule,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		promotion.CampaignDetail = JsonSerializer.Serialize(rule.CampaignDetail, _SerializerOptions);
		promotion.BonusAmountLimit = rule.BonusAmountLimit;
		promotion.Percentage = rule.Percentage;
		promotion.TurnoverMultiple = rule.TurnoverMultiple;
		promotion.GameType = (short?)rule.GameType;
		promotion.UpdatedByUsername = modifier;
		promotion.UpdatedOn = DateTime.UtcNow;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SetPromotionStatusAsync(
		Guid promotionId,
		PromotionStatus status,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		promotion.Status = (short)status;
		promotion.UpdatedByUsername = modifier;
		promotion.UpdatedOn = DateTime.UtcNow;

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

	public async ValueTask<bool> UpdatePromotionAmountAsync(Guid promotionId, decimal amount)
	{
		const string sql = "select * from dbo.promotion_update_amount_used(@promotion_id,@amount)";

		var conn = _dbContext.Database.GetDbConnection();
		var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

		return await conn.QueryFirstOrDefaultAsync<bool>(
				sql,
				new Dictionary<string, object>
				{
					{ "@promotion_id", promotionId },
					{ "@amount", amount }
				}, commandType: CommandType.Text, transaction: transaction)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdatePromotionAsync(
		PromotionRow promotion,
		UpdatePromotionRequest request)
	{
		var vips = await _vipLevelService.GetQueries()
			.Where(x => x.BrandId == promotion.BrandId && request.VipLevels.Contains(x.Level)).ToArrayAsync();

		using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

		_dbContext.PromotionGameProviders.RemoveRange(promotion.PromotionGameProviders);
		_dbContext.PromotionVipLevels.RemoveRange(promotion.PromotionVipLevels);
		_ = await _dbContext.SaveChangesAsync().ConfigureAwait(false);

		promotion.Name = request.Name;
		promotion.From = request.From.UtcDateTime;
		promotion.To = request.To.UtcDateTime;
		promotion.AutoSentPlayerBonus = request.AutoSentPlayerBonus;
		promotion.PromotionType = request.PromotionType;
		promotion.BonusAmountLimit = request.BonusAmountLimit;
		promotion.Unlimited = (short)(request.Unlimited ? 1 : 0);
		promotion.GameType = (short?)request.GameType;
		promotion.Percentage = request.Percentage;
		promotion.TotalAmountLimit = request.TotalAmountLimit;
		promotion.TurnoverMultiple = request.TurnoverMultiple;

		promotion.PromotionGameProviders =
			request.GameProviderIds!
				.Select(id => new PromotionGameProvider
				{
					GameProviderId = id,
					PromotionId = promotion.Id
				})
				.ToHashSet();

		promotion.PromotionVipLevels =
			vips.Select(x => new PromotionVipLevel
			{
				VipLevelId = x.Id,
				Level = x.Level,
				PromotionId = promotion.Id
			})
				.ToHashSet();
		promotion.UpdatedByUsername = request.EditorUsername;
		promotion.UpdatedOn = DateTime.UtcNow;
		promotion.SubSubject = request.SubSubject;
		promotion.Content = request.Content;
		promotion.Terms = request.Terms;
		promotion.DesktopBanner = request.DesktopBanner;
		promotion.MobileBanner = request.MobileBanner;
		promotion.Localization = request.Localization != null ? JsonSerializer.Serialize(request.Localization, new JsonSerializerOptions { WriteIndented = false }) : null;

		await _promotionService.UpdateEntityAsync(promotion);

		scope.Complete();
	}

	public async ValueTask UpdatePromotionDistributeAsync(Guid distributeId, DateTime sentOn, CancellationToken cancellationToken = default)
	{
		var distribute = await _dbContext.PromotionDistributes.FindAsync(
			new object?[] { distributeId },
			cancellationToken: cancellationToken).ConfigureAwait(false);

		if (distribute is null)
			return;

		distribute.SentOn = sentOn.ToUniversalTime();

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);
	}

	public async ValueTask UpdatePromotionMainInfoAsync(
		Guid promotionId,
		UpdatePromotionMainInfo updateInfo,
		string modifier,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		var promotionsJson = await _dbContext.Settings.Where(x => x.Key == "PromotionTypes").FirstAsync(cancellationToken).ConfigureAwait(false);

		var promotionTypeGroups =
			JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

		var promotionType = promotionTypeGroups!.SelectMany(x => x.Types)
			.First(x => x.Name == updateInfo.PromotionType);

		promotion.Name = updateInfo.Name;
		promotion.From = updateInfo.EventDuring.From;
		promotion.To = updateInfo.EventDuring.To;
		promotion.PromotionType = updateInfo.PromotionType;
		promotion.Unlimited = updateInfo.PromotionLimitAmount is null ? (short)1 : (short)0;
		promotion.TotalAmountLimit = updateInfo.PromotionLimitAmount ?? 0M;
		promotion.AutoSentPlayerBonus = updateInfo.IsAutoSent;
		promotion.SettlementIntervalType = promotionType.SettlementIntervalType;

		promotion.UpdatedByUsername = modifier;
		promotion.UpdatedOn = DateTime.UtcNow;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask UpdatePromotionsAsync(PromotionRow[] promotions) =>
			_promotionService.UpdateEntitiesAsync(promotions);

	public async ValueTask UpdatePromotionUsedAmountAsync(
		Guid promotionId,
		decimal totalAmount,
		Guid? snapshotId,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		if (promotion.AmountUsedSnapshotId is null
			|| promotion.AmountUsedSnapshotId.Value.CompareTo(snapshotId) < 0)
		{
			promotion.AmountUsed = totalAmount;
			promotion.AmountUsedSnapshotId = snapshotId;

			_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	private HashSet<PromotionGameProvider> GeneratePromotionGameProviders(
		PromotionRow promotion,
		CreatePromotionRequest request)
	{
		var promotionGameProviders = _gameProviderService.GetQueries()
			.Where(x => request.GameProviderIds!.Contains(x.Id))
			.Select(x => new PromotionGameProvider
			{
				PromotionId = promotion.Id,
				GameProviderId = x.Id
			});

		return new HashSet<PromotionGameProvider>(promotionGameProviders);
	}

	private HashSet<PromotionVipLevel> GeneratePromotionVipLevels(
		PromotionRow promotion,
		CreatePromotionRequest request)
	{
		var promotionVipLevels = _vipLevelService.GetQueries()
			.Where(x => x.BrandId == promotion.BrandId && request.VipLevels.Contains(x.Level)).Select(x =>
				new PromotionVipLevel
				{
					PromotionId = promotion.Id,
					VipLevelId = x.Id,
					Level = x.Level
				});

		return new HashSet<PromotionVipLevel>(promotionVipLevels);
	}
}
