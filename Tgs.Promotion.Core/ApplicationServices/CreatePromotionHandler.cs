using System.Text.Json;
using Tgs.Promotion.Core.Models;
using Tgs.Promotion.Core.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.ApplicationServices;

public class CreatePromotionHandler : IGetService<CreatePromotionRequest, MessageResponse>
{
	private readonly IPromotionRepository _service;
	private readonly ISettingsRepository _settingsService;

	public CreatePromotionHandler(IPromotionRepository service, ISettingsRepository settingsService)
	{
		_service = service;
		_settingsService = settingsService;
	}

	public async ValueTask<MessageResponse> GetAsync(CreatePromotionRequest request,
		CancellationToken cancellationToken)
	{
		if (await _service.ExistsAsync(x =>
				x.BrandId == request.BrandId && x.Name == request.Name))
		{
			return new MessageResponse { Valid = false, Message = "Name is existed." };
		}

		var promotionsJson = await _settingsService.GetSettingAsync(x => x != null && x.Key == "PromotionTypes");

		if (promotionsJson is null)
			return new MessageResponse { Valid = false, Message = "promotion types is not found." };

		var promotionTypeGroups =
			JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson!.Value);

		if (promotionTypeGroups is null || promotionTypeGroups.SelectMany(x => x.Types).All(x => x.Name != request.PromotionType))
			return new MessageResponse { Valid = false, Message = "Promotion type is not found." };

		var promotionType = promotionTypeGroups.SelectMany(x => x.Types)
			.First(x => x.Name == request.PromotionType);

		_ = await _service.CreatePromotionAsync(request, promotionType);

		return new MessageResponse { Valid = true };
	}
}
