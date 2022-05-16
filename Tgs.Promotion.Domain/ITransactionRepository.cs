using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Domain
{
	public interface ITransactionRepository
	{
		ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotion);
	}
}
