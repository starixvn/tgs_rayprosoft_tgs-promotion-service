namespace Tgs.Promotion.ApplicationServices;

public record TagPlayerDepositCommand(string PlayerCode, Guid? PromotionId);
