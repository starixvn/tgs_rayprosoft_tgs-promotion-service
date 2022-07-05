namespace Tgs.Promotion.ApplicationServices;

public record CancelTagPlayerDepositCommand(string PlayerCode, Guid? PromotionId);
