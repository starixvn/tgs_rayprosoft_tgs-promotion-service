using System.Data;
using System.Linq.Expressions;
using System.Text.Json;
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
		WriteIndented = false
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

	public async ValueTask<Guid> CreatePromotionDistributeAsync(
		Guid planId,
		string playerCode,
		decimal bonusAmount,
		decimal needTurnover,
		string? distributor,
		CancellationToken cancellationToken = default)
	{
		var plan = await _dbContext.PromotionDistributePlans
			.FindAsync(new object?[] { planId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (plan is null)
			throw new InvalidOperationException($"Promotion distribute plan with id {planId} not found");

		var distributeId = await _serialNumberGenerator
			.GenerateAsync(cancellationToken)
			.ConfigureAwait(false);

		_ = await _dbContext.PromotionDistributes.AddAsync(new PromotionDistribute
		{
			Id = distributeId,
			BrandId = plan.BrandId,
			PromotionId = plan.PromotionId,
			PlanId = planId,
			PlayerCode = playerCode,
			BonusAmount = bonusAmount,
			Bucket = needTurnover,
			Distributor = distributor,
			CreatedOn = DateTime.UtcNow,
			SentOn = DateTime.UtcNow
		}, cancellationToken).ConfigureAwait(false);

		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);

		return distributeId;
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

			_ = await _dbContext.PromotionDistributePlans.AddAsync(new PromotionDistributePlan
			{
				Id = createId,
				BrandId = promotion.BrandId,
				PromotionId = promotionId,
				ConditionHash = conditionHash,
				Detail = JsonSerializer.Serialize(promotion, _SerializerOptions)
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

		return promotion is null
			? null
			: new PromotionRule(
			JsonDocument.Parse(promotion.CampaignDetail),
			promotion.BonusAmountLimit,
			promotion.Percentage,
			promotion.TurnoverMultiple,
			(GameType?)promotion.GameType);
	}

	public async ValueTask<bool> RevertPromotionAmountAsync(Guid promotionId, decimal amount)
	{
		const string sql = "dbo.promotion_revert_amount_used";

		var conn = _dbContext.Database.GetDbConnection();
		var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

		return await conn.QueryFirstOrDefaultAsync<bool>(
				sql,
				new Dictionary<string, object>
				{
					{ "_promotion_id", promotionId },
					{ "_amount", amount }
				}, commandType: CommandType.StoredProcedure, transaction: transaction)
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
		const string sql = "dbo.promotion_update_amount_used";

		var conn = _dbContext.Database.GetDbConnection();
		var transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

		return await conn.QueryFirstOrDefaultAsync<bool>(
				sql,
				new Dictionary<string, object>
				{
					{ "_promotion_id", promotionId },
					{ "_amount", amount }
				}, commandType: CommandType.StoredProcedure, transaction: transaction)
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

		promotion.Name = updateInfo.Name;
		promotion.From = updateInfo.EventDuring.From;
		promotion.To = updateInfo.EventDuring.To;
		promotion.PromotionType = updateInfo.PromotionType;
		promotion.Unlimited = updateInfo.PromotionLimitAmount is null ? (short)1 : (short)0;
		promotion.TotalAmountLimit = updateInfo.PromotionLimitAmount ?? 0M;
		promotion.AutoSentPlayerBonus = updateInfo.IsAutoSent;

		promotion.UpdatedByUsername = modifier;
		promotion.UpdatedOn = DateTime.UtcNow;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public ValueTask UpdatePromotionsAsync(PromotionRow[] promotions) =>
			_promotionService.UpdateEntitiesAsync(promotions);

	public async ValueTask UpdatePromotionUsedAmountAsync(Guid promotionId, Guid snapshotId, decimal amountUsed,
		CancellationToken cancellationToken = default)
	{
		var promotion = await _dbContext.PromotionRows
			.FindAsync(new object?[] { promotionId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (promotion is null)
			return;

		promotion.AmountUsedSnapshotId = snapshotId;
		promotion.AmountUsed = amountUsed;

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
