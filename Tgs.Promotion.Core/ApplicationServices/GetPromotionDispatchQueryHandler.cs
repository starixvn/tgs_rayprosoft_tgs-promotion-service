using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionDispatchQueryHandler : IGetService<GetPromotionDispatchesRequest, PagedResponse<PromotionDispatchModel>>
{
	private readonly IPromotionBonusRepository _promotionBonusService;
	private readonly IPromotionDistributionRepository _promotionDistributionRepository;
	private readonly ISettingsRepository _settingRepository;
	private readonly IPlayerRepository _tgsPlayerService;

	public GetPromotionDispatchQueryHandler(
		IPromotionBonusRepository promotionBonusService,
		IPromotionDistributionRepository promotionDistributionRepository,
		IPlayerRepository tgsPlayerService,
		ISettingsRepository settingRepository)
	{
		_promotionBonusService = promotionBonusService;
		_promotionDistributionRepository = promotionDistributionRepository;
		_tgsPlayerService = tgsPlayerService;
		_settingRepository = settingRepository;
	}

	public async ValueTask<PagedResponse<PromotionDispatchModel>> GetAsync(
		GetPromotionDispatchesRequest request,
		CancellationToken cancellationToken)
	{
		var promotionsJson = await _settingRepository
			.GetSettingAsync(x => x.Key == "PromotionTypes")
			.ConfigureAwait(false);

		if (promotionsJson is null)
		{
			return new(0, Array.Empty<PromotionDispatchModel>())
			{
				Valid = false,
				Message = "promotion types is not found."
			};
		}

		var promotionTypeGroups = JsonSerializer
			.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

		if (promotionTypeGroups is null)
		{
			return new(0, Array.Empty<PromotionDispatchModel>())
			{
				Valid = false,
				Message = "promotion group type is not found."
			};
		}

		var before60Day = DateTime.UtcNow.AddDays(-60);

		var promotionGroup = promotionTypeGroups.FirstOrDefault(x => x.Name == request.PromotionGroup);

		var promotionTypes = promotionGroup?.Types.Select(x => x.Name).ToArray() ?? Array.Empty<string>();

		var promotionDistributes = _promotionDistributionRepository.GetPromotionDistributionQueries()
			.Include(x => x.Promotion)
			.Where(x => x.BrandId == request.BrandId)
			.Where(x => x.CreatedOn >= before60Day)
			.Where(request.From, x => request.From <= x.CreatedOn)
			.Where(request.To, x => request.To >= x.CreatedOn)
			.Where(!request.PromotionName.IsNullOrEmpty(), x => x.Promotion.Name == request.PromotionName)
			.Where(promotionTypes, x => promotionTypes.Contains(x.Promotion.PromotionType))
			.Join(_tgsPlayerService.GetPlayerQueries().Where(request.Username, x => x.Username == request.Username),
				x => x.PlayerCode, p => p.PlayerCode,
				(x, p) => new
				{
					PromotionBonusId = x.Id,
					x.PromotionId,
					PromotionName = x.Promotion.Name,
					x.Promotion.PromotionType,
					x.CreatedOn,
					x.SentOn,
					p.Username,
					p.PlayerCode,
					x.BonusAmount
				})
			.Select(x => new PromotionDispatchConcatModel
			{
				PromotionBonusId = x.PromotionBonusId,
				PromotionId = x.PromotionId,
				PromotionName = x.PromotionName,
				PromotionType = x.PromotionType,
				CreatedOn = x.CreatedOn,
				SentOn = x.SentOn,
				Username = x.Username,
				PlayerCode = x.PlayerCode,
				BonusAmount = x.BonusAmount.ToString()
			});

		var promotionPlayers = _promotionBonusService.GetPromotionBonusPlayersQueries()
			.Include(x => x.PromotionBonus)
			.ThenInclude(x => x.Promotion)
			.Where(x => x.PromotionBonus.BrandId == request.BrandId)
			.Where(x => x.PromotionBonus.CreatedOn >= before60Day || x.SentOn == null)
			.Where(request.From, x => request.From <= x.SentOn)
			.Where(request.To, x => request.To >= x.SentOn)
			.Where(!request.PromotionName.IsNullOrEmpty(),
				x => x.PromotionBonus.Promotion.Name == request.PromotionName)
			.Where(promotionTypes, x => promotionTypes.Contains(x.PromotionBonus.PromotionType))
			.Join(_tgsPlayerService.GetPlayerQueries().Where(request.Username, x => x.Username == request.Username),
				x => x.PlayerCode, p => p.PlayerCode,
				(x, p) => new
				{
					x.PromotionBonusId,
					x.PromotionBonus.PromotionId,
					PromotionName = x.PromotionBonus.Promotion.Name,
					x.PromotionBonus.PromotionType,
					x.PromotionBonus.CreatedOn,
					x.BonusAmount,
					x.SentOn,
					p.Username,
					p.PlayerCode
				})
			.Select(x => new PromotionDispatchConcatModel
			{
				PromotionBonusId = x.PromotionBonusId,
				PromotionId = x.PromotionId,
				PromotionName = x.PromotionName,
				PromotionType = x.PromotionType,
				CreatedOn = x.CreatedOn,
				SentOn = x.SentOn,
				Username = x.Username,
				PlayerCode = x.PlayerCode,
				BonusAmount = x.BonusAmount.ToString()
			});

		promotionDistributes = promotionDistributes.Concat(promotionPlayers)
			.Select(x => new PromotionDispatchConcatModel
			{
				PromotionBonusId = x.PromotionBonusId,
				PromotionId = x.PromotionId,
				PromotionName = x.PromotionName,
				PromotionType = x.PromotionType,
				CreatedOn = x.CreatedOn,
				Username = x.Username,
				PlayerCode = x.PlayerCode,
				SentOn = x.SentOn,
				BonusAmount = x.BonusAmount
			}).AsNoTracking();

		var totalCount = await promotionDistributes.CountAsync(cancellationToken);

		var page = request.Page <= 0 ? 0 : request.Page - 1;

		var pageSize = request.Rows <= 0 ? 10 : request.Rows;

		var items = await promotionDistributes
			.OrderByDescending(x => !x.SentOn.HasValue)
			.ThenByDescending(x => x.SentOn)
			.Skip(page * pageSize)
			.Take(pageSize)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var promotionDispatches = items.Select(x => new PromotionDispatchModel
		{
			PromotionBonusId = x.PromotionBonusId,
			PromotionId = x.PromotionId,
			PromotionName = x.PromotionName,
			PromotionType = x.PromotionType,
			PromotionGroup = promotionTypeGroups
				.First(p => p.Types.Select(t => t.Name).Contains(x.PromotionType)).Name,
			CreatedOn = x.CreatedOn,
			Username = x.Username,
			PlayerCode = x.PlayerCode,
			SentOn = x.SentOn,
			BonusAmount = decimal.Parse(x.BonusAmount)
		}).ToArray();

		var promotionDispatchPages =
			new PagedResponse<PromotionDispatchModel>(totalCount, promotionDispatches) { Valid = true };

		return promotionDispatchPages;
	}
}