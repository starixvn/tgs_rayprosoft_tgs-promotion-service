namespace Tgs.Promotion.Core.ApplicationServices;

public record TagPlayerDepositCommand(string PlayerCode, Guid? PromotionId);
