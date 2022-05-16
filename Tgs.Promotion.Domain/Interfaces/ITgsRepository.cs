using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.Promotion.Domain.Entities;

namespace Tgs.Promotion.Domain.Interfaces
{
	public interface ITgsRepository
	{
		DbSet<BrandGame> BrandGames { get; set; }

		DbSet<GameProvider> GameProviders { get; set; }

		DbSet<Player> Players { get; set; }

		DbSet<PlayerBucket> PlayerBuckets { get; set; }

		DbSet<PlayerGameRebateAggregate> PlayerGameRebateAggregates { get; set; }

		DbSet<PlayerVipLevelSeasonPromotion> PlayerVipLevelSeasonPromotions { get; set; }

		DbSet<PlayerVipLevelUpgradePromotion> PlayerVipLevelUpgradePromotions { get; set; }

		DbSet<Entities.Promotion> Promotions { get; set; }

		DbSet<PromotionBonus> PromotionBonuses { get; set; }

		DbSet<PromotionGameProvider> PromotionGameProviders { get; set; }

		DbSet<PromotionParticipant> PromotionParticipants { get; set; }

		DbSet<PromotionPlayer> PromotionPlayers { get; set; }

		DbSet<PromotionVipLevel> PromotionVipLevels { get; set; }

		DbSet<Setting> Settings { get; set; }

		DbSet<TransactionPromotion> TransactionPromotions { get; set; }

		DbSet<VipLevel> VipLevels { get; set; }

		IDbContextTransaction? CurrentTransaction { get; }

		DbSet<T> Set<T>() where T : class;

		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

		IDbConnection GetDbConnection();
	}
}
