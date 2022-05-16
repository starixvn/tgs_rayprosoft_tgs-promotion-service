using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Core
{
	public interface ITransactionRepository
	{
		ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotion);
	}
}
