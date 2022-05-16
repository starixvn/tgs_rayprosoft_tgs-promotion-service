using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Domain.Interfaces
{
	public interface ITransactionRepository
	{
		ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotion);
	}
}
