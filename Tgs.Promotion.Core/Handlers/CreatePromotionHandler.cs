using System.Text.Json;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers
{
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
                return new MessageResponse{Valid=false, Message = "Name is existed." };
            }

            var promotionsJson = await _settingsService.GetSettingAsync(x => x.Key == "PromotionTypes");
            var promotionTypeGroups =
                JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

            if (promotionTypeGroups is null || promotionTypeGroups.SelectMany(x => x.Types).All(x => x.Name != request.PromotionType))
            {
                return new MessageResponse { Valid = false, Message = "Promotion type is not found." };
            }

            var promotionType = promotionTypeGroups.SelectMany(x => x.Types)
                .First(x => x.Name == request.PromotionType);

            await _service.CreatePromotionAsync(request, promotionType);

            return new MessageResponse { Valid = true };
        }
    }
}