using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;

namespace Tgs.Promotion.Core;

public class PromotionTypeRepository : IPromotionTypeRepository
{
	private readonly ITgsRepository _repository;

	public PromotionTypeRepository(ITgsRepository repository)
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
