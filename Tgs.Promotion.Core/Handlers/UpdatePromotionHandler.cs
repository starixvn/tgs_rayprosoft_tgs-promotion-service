using System.Text.Json;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;
using Tgs.Promotion.Domain.Models;
using Tgs.Promotion.Domain.Models.Promotion;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.Core.Handlers
{
    public class UpdatePromotionHandler : IGetService<UpdatePromotionRequest, MessageResponse>
    {
        private readonly IPromotionRepository _service;
        private readonly ISettingsRepository _settingsService;

        public UpdatePromotionHandler(IPromotionRepository service, ISettingsRepository settingsService)
        {
            _service = service;
            _settingsService = settingsService;
        }

        public  async ValueTask<MessageResponse> GetAsync(UpdatePromotionRequest request,
            CancellationToken cancellationToken)
        {
            var promotion = await _service.GetPromotionAsync(q => q.Id == request.Id);

            if (promotion == null)
            {
                return new MessageResponse { Valid = false, Message = "Promotion is not found." };
            }

            if (await _service.ExistsAsync(x => request.Id != x.Id && x.Name == request.Name))
            {
                return new MessageResponse { Valid = false, Message = "name is existed." };
            }

            var promotionsJson = await _settingsService.GetSettingAsync(x => x.Key == "PromotionTypes");
            var promotionTypeGroups =
                JsonSerializer.Deserialize<PromotionTypeGroup[]>(promotionsJson.Value);

            if (promotionTypeGroups.SelectMany(x => x.Types).All(x => x.Name != request.PromotionType))
            {
                return new MessageResponse { Valid = false, Message = "Promotion type is not found" };
            }

            var promotionType = promotionTypeGroups.SelectMany(x => x.Types)
                .First(x => x.Name == request.PromotionType);

            promotion.SettlementIntervalType = promotionType.SettlementIntervalType;

            await _service.UpdatePromotionAsync(promotion, request);

            return new MessageResponse { Valid = true };
        }
    }
}