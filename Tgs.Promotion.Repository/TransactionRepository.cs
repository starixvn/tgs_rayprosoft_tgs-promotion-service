using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

internal class TransactionRepository(ITgsService<TransactionPromotion> transactionPromotionService) : ITransactionRepository
{
	public ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotions)
		=> transactionPromotionService.CreateEntitiesAsync(transactionPromotions);
}
