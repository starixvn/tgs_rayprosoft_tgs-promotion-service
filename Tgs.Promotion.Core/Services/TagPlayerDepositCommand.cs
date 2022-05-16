namespace Tgs.Promotion.Core.Services
{
	public record TagPlayerDepositCommand(string PlayerCode, Guid? PromotionId);
}
