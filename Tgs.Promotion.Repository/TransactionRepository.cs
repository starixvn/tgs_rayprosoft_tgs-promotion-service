using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Repository;

internal class TransactionRepository : ITransactionRepository
{
	private readonly ITgsService<TransactionPromotion> _transactionPromotionService;

	public TransactionRepository(ITgsService<TransactionPromotion> transactionPromotionService)
	{
		_transactionPromotionService = transactionPromotionService;
	}

	public ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotions)
		=> _transactionPromotionService.CreateEntitiesAsync(transactionPromotions);
}
