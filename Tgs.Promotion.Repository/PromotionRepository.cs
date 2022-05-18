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
	private readonly ITgsService<Domain.Entities.Promotion> _promotionService;
	private readonly ITgsService<VipLevel> _vipLevelService;
	private readonly ITgsService<GameProvider> _gameProviderService;
	private readonly TgsContext _dbContext;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public PromotionRepository(
		ITgsService<Domain.Entities.Promotion> promotionService,
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

	public async ValueTask<Domain.Entities.Promotion?> GetPromotionAsync(
		Expression<Func<Domain.Entities.Promotion, bool>> predicate,
		Func<IQueryable<Domain.Entities.Promotion>, IQueryable<Domain.Entities.Promotion>>? preQuery)
	{
		preQuery ??= c => c;

		return await preQuery(_promotionService.GetQueries()).FirstOrDefaultAsync(predicate);
	}

	public ValueTask ChangePromotionStatusAsync(Domain.Entities.Promotion promotion) =>
			_promotionService.UpdateEntityAsync(promotion);

	public async ValueTask<Guid> CreatePromotionAsync(
		CreatePromotionRequest request,
		PromotionType promotionTypeModel)
	{
		var promotionId = await _serialNumberGenerator.GenerateAsync().ConfigureAwait(false);

		var promotion = new Domain.Entities.Promotion
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
			TotalAmountLimit = request.TotalAmountLimit,
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

	public async ValueTask<bool> ExistsAsync(
		Expression<Func<Domain.Entities.Promotion, bool>> predicate)
		=> await _promotionService.GetQueries().AnyAsync(predicate).ConfigureAwait(false);

	public async ValueTask<Domain.Entities.Promotion?> GetPromotionAsync(
		Expression<Func<Domain.Entities.Promotion, bool>> predicate) =>
		await _promotionService.GetQueries().FirstOrDefaultAsync(predicate).ConfigureAwait(false);

	public async ValueTask<Domain.Entities.Promotion?> GetPromotionByIdAsync(Guid promotionId)
		=> await _promotionService.GetQueries().FirstOrDefaultAsync(x => x.Id == promotionId).ConfigureAwait(false);

	public IQueryable<Domain.Entities.Promotion> GetPromotionQueries(
		Func<IQueryable<Domain.Entities.Promotion>, IQueryable<Domain.Entities.Promotion>>? preQuery,
		bool tracking)
	{
		preQuery ??= c => c;
		return tracking
			? preQuery(_promotionService.GetQueries()).AsTracking()
			: preQuery(_promotionService.GetQueries()).AsNoTracking();
	}

	public async ValueTask UpdatePromotionAsync(
		Domain.Entities.Promotion promotion,
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
			request.GameProviderIds
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

	public ValueTask UpdatePromotionsAsync(Domain.Entities.Promotion[] promotions) =>
		_promotionService.UpdateEntitiesAsync(promotions);

	public async ValueTask<Guid> CreatePromotionAsync(CreatePromotionInfo createInfo, CancellationToken cancellationToken = default)
	{
		var promotionId = await _serialNumberGenerator.GenerateAsync().ConfigureAwait(false);

		var now = DateTime.UtcNow;

		var promotion = new Domain.Entities.Promotion
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
			CreatedByUsername = createInfo.Modifier,
			UpdatedByUsername = createInfo.Modifier,
			CreatedOn = now,
			UpdatedOn = now
		};

		_ = await _dbContext.Promotions
			.AddAsync(promotion, cancellationToken)
			.ConfigureAwait(false);
		_ = await _dbContext
			.SaveChangesAsync(cancellationToken)
			.ConfigureAwait(false);

		return promotionId;
	}

	private HashSet<PromotionGameProvider> GeneratePromotionGameProviders(
			Domain.Entities.Promotion promotion,
		CreatePromotionRequest request)
	{
		var promotionGameProviders = _gameProviderService.GetQueries()
			.Where(x => request.GameProviderIds.Contains(x.Id)).Select(x =>
				new PromotionGameProvider
				{
					PromotionId = promotion.Id,
					GameProviderId = x.Id
				});

		return new(promotionGameProviders);
	}

	private HashSet<PromotionVipLevel> GeneratePromotionVipLevels(
		Domain.Entities.Promotion promotion,
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

		return new(promotionVipLevels);
	}
}
