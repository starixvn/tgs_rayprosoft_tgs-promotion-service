namespace Tgs.Promotion.Models.PromotionBucket;

public record CreatePlayerBucketWithDepositRequest(
	Guid BrandId,
	Guid? PromotionId,
	string PlayerCode,
	Guid DefaultWalletId,
	decimal DepositAmount,
	string Remark,
	Guid FormId,
	Guid? UserId);
