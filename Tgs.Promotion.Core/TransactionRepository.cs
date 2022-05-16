using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Core
{
	public class TransactionRepository : ITransactionRepository
	{
		private readonly ITgsService<TransactionPromotion> _transactionPromotionService;

		public TransactionRepository(ITgsService<TransactionPromotion> transactionPromotionService)
		{
			_transactionPromotionService = transactionPromotionService;
		}

		public ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotions) =>
			_transactionPromotionService.CreateEntitiesAsync(transactionPromotions);
	}
}
