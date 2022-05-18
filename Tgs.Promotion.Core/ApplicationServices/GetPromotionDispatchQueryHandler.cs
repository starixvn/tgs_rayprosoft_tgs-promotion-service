using System.Text.Json;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class
	GetPromotionDispatchQueryHandler : IGetService<GetPromotionDispatchesRequest, PagedResponse<PromotionDispatchModel>>
{
	private readonly IPromotionBonusRepository _promotionBonusService;
	private readonly ISettingsRepository _settingRepository;
	private readonly IPlayerRepository _tgsPlayerService;
	private readonly IPromotionRepository _tgsPromotionService;

	public GetPromotionDispatchQueryHandler(IPromotionBonusRepository promotionBonusService,
		IPromotionRepository tgsPromotionService,
		IPlayerRepository tgsPlayerService,
		ISettingsRepository settingRepository)
	{
		_promotionBonusService = promotionBonusService;
		_tgsPromotionService = tgsPromotionService;
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
			return new PagedResponse<PromotionDispatchModel>(0, Array.Empty<PromotionDispatchModel>()) { Valid = false, Message = "promotion types is not found." };

		var promotionTypeGroups = JsonSerializer
			.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

		if (promotionTypeGroups is null)
			return new PagedResponse<PromotionDispatchModel>(0, Array.Empty<PromotionDispatchModel>()) { Valid = false, Message = "promotion group type is not found." };

		var promotionGroup = promotionTypeGroups.FirstOrDefault(x => x.Name == request.PromotionGroup);
		var promotionTypes = promotionGroup?.Types.Select(x => x.Name).ToArray() ?? Array.Empty<string>();

		var promotion = await _tgsPromotionService
			.GetPromotionAsync(x => x.Name == request.PromotionName)
			.ConfigureAwait(false);

		var promotionDispatchPages = await _promotionBonusService.GetPromotionBonusPlayersQueries()
			.Where(x => x.PromotionBonus.BrandId == request.BrandId)
			.Where(request.From, x => request.From <= x.PromotionBonus.CreatedOn)
			.Where(request.To, x => request.To >= x.PromotionBonus.CreatedOn)
			.Where(promotion is not null, x => x.PromotionBonus.PromotionId == promotion!.Id)
			.Where(promotionTypes, x => promotionTypes.Contains(x.PromotionBonus.PromotionType))
			.Join(_tgsPlayerService.GetPlayerQueries().Where(request.Username, x => x.Username == request.Username),
				x => x.PlayerCode, p => p.PlayerCode,
				(x, p) => new
				{
					PromotionName = x.PromotionBonus.Promotion.Name,
					x.PromotionBonus,
					x.PlayerCode,
					x.BonusAmount,
					x.SentOn,
					Player = p
				})
			.Select(x => new PromotionDispatchModel
			{
				PromotionBonusId = x.PromotionBonus.Id,
				PromotionId = x.PromotionBonus.PromotionId,
				PromotionName = x.PromotionName,
				PromotionType = x.PromotionBonus.PromotionType,
				CreatedOn = x.PromotionBonus.CreatedOn,
				Username = x.Player.Username,
				PlayerCode = x.PlayerCode,
				BonusAmount = x.BonusAmount,
				SentOn = x.SentOn
			})
			.CreatePagedResponseAsync(request);

		foreach (var item in promotionDispatchPages.Items)
		{
			item.PromotionGroup = promotionTypeGroups
				.First(p => p.Types.Select(t => t.Name).Contains(item.PromotionType)).Name;
		}

		return promotionDispatchPages;
	}
}
