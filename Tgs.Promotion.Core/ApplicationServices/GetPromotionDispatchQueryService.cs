using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Extensions;
using Tgs.Promotion.Models;
using Tgs.Promotion.Models.PromotionDispatch;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.ApplicationServices;

internal class GetPromotionDispatchQueryService(
	IPromotionBonusRepository promotionBonusService,
	IPromotionDistributionRepository promotionDistributionRepository,
	IPlayerRepository tgsPlayerService,
	ISettingsRepository settingRepository,
	IPromotionDetailFactory promotionDetailFactory) : IGetService<GetPromotionDispatchesQuery, PagedResponse<PromotionDispatchModel>>
{
	public async ValueTask<PagedResponse<PromotionDispatchModel>> GetAsync(
		GetPromotionDispatchesQuery request,
		CancellationToken cancellationToken)
	{
		var promotionsJson = await settingRepository
			.GetSettingAsync(x => x.Key == "PromotionTypes")
			.ConfigureAwait(false);

		if (promotionsJson is null)
		{
			return new(0, [])
			{
				Valid = false,
				Message = "promotion types is not found."
			};
		}

		var promotionTypeGroups = JsonSerializer
			.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

		if (promotionTypeGroups is null)
		{
			return new(0, [])
			{
				Valid = false,
				Message = "promotion group type is not found."
			};
		}

		var before60Day = DateTime.UtcNow.AddDays(-60);

		var promotionGroup = promotionTypeGroups.FirstOrDefault(x => x.Name == request.PromotionGroup);

		var promotionTypes = promotionGroup?.Types.Select(x => x.Name).ToArray() ?? [];

		var promotionDistributes = promotionDistributionRepository.GetPromotionDistributionQueries()
			.Include(x => x.Promotion)
			.Where(x => x.BrandId == request.BrandId)
			.Where(x => x.CreatedOn >= before60Day)
			.Where(request.From, x => request.From <= x.CreatedOn)
			.Where(request.To, x => request.To >= x.CreatedOn)
			.Where(!string.IsNullOrEmpty(request.PromotionName), x => x.Promotion.Name == request.PromotionName)
			.Where(promotionTypes, x => promotionTypes.Contains(x.Promotion.PromotionType))
			.Join(tgsPlayerService.GetPlayerQueries().Where(request.Username, x => x.Username == request.Username),
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
					x.BonusAmount,
					x.ReferenceId
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
				BonusAmount = x.BonusAmount.ToString(),
				ReferenceId = x.ReferenceId
			});

		var promotionPlayers = promotionBonusService.GetPromotionBonusPlayersQueries()
			.Include(x => x.PromotionBonus)
			.ThenInclude(x => x.Promotion)
			.Where(x => x.PromotionBonus.BrandId == request.BrandId)
			.Where(x => x.PromotionBonus.CreatedOn >= before60Day)
			.Where(request.From, x => request.From <= x.SentOn)
			.Where(request.To, x => request.To >= x.SentOn)
			.Where(
				!string.IsNullOrEmpty(request.PromotionName),
				x => x.PromotionBonus.Promotion.Name == request.PromotionName)
			.Where(promotionTypes, x => promotionTypes.Contains(x.PromotionBonus.PromotionType))
			.Join(tgsPlayerService.GetPlayerQueries().Where(request.Username, x => x.Username == request.Username),
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
				BonusAmount = x.BonusAmount.ToString(),
				ReferenceId = null
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
				BonusAmount = x.BonusAmount,
				ReferenceId = x.ReferenceId
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

		foreach (var item in items)
		{
			item.Detail = item.ReferenceId == null
				? null
				: (await promotionDetailFactory.GetPromotionDetail(item.PromotionType)
					.GetDetailAsync(item.PromotionId, item.ReferenceId.Value, TimeSpan.FromSeconds(10),
						cancellationToken: cancellationToken))
				?.Detail;
		}

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
			BonusAmount = decimal.Parse(x.BonusAmount),
			Detail = x.Detail == null ? null : JsonDocument.Parse(x.Detail)
		}).ToArray();

		var promotionDispatchPages =
			new PagedResponse<PromotionDispatchModel>(totalCount, promotionDispatches) { Valid = true };

		return promotionDispatchPages;
	}
}
