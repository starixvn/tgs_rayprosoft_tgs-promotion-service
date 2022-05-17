using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion;

public interface ITransactionRepository
{
	ValueTask CreateTransactionPromotionsAsync(TransactionPromotion[] transactionPromotion);
}
