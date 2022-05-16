using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Core.Models;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Core;

public class PromotionTypeRepository : IPromotionTypeRepository
{
	private readonly TgsContext _repository;

	public PromotionTypeRepository(TgsContext repository)
	{
		_repository = repository;
	}

	public async ValueTask<bool> IsDepositTypeAsync(string promotionType)
	{
		var promotionsJson = await _repository.Settings.AsQueryable()
			.FirstOrDefaultAsync(x => x.Key == "PromotionTypes");

		var promotionTypeGroups =
			JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson!.Value);

		if (promotionTypeGroups == null)
		{
			return false;
		}

		var promotionGroupType =
			promotionTypeGroups.FirstOrDefault(x => x.Types.Select(t => t.Name).Contains(promotionType));

		return promotionGroupType?.Name == "Deposit";
	}
}
