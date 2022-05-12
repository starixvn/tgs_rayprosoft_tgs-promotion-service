using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Tgs.Promotion.Domain.Entities;
using Tgs.Promotion.Domain.Interfaces;

namespace Tgs.Promotion.Repository.Repositories
{
    public class TgsRepository : DbContext, ITgsRepository
    {
        public TgsRepository(DbContextOptions<TgsRepository> options) : base(options)
        {
        }

        public IDbContextTransaction? CurrentTransaction =>
            Database.CurrentTransaction;

        public virtual DbSet<BrandGame> BrandGames { get; set; } = null!;
        public virtual DbSet<GameProvider> GameProviders { get; set; } = null!;
        public virtual DbSet<Player> Players { get; set; } = null!;
        public virtual DbSet<PlayerBucket> PlayerBuckets { get; set; } = null!;
        public virtual DbSet<PlayerGameRebateAggregate> PlayerGameRebateAggregates { get; set; } = null!;
        public virtual DbSet<PlayerVipLevelSeasonPromotion> PlayerVipLevelSeasonPromotions { get; set; } = null!;
        public virtual DbSet<PlayerVipLevelUpgradePromotion> PlayerVipLevelUpgradePromotions { get; set; } = null!;
        public virtual DbSet<Domain.Entities.Promotion> Promotions { get; set; } = null!;
        public virtual DbSet<PromotionBonus> PromotionBonuses { get; set; } = null!;
        public virtual DbSet<PromotionGameProvider> PromotionGameProviders { get; set; } = null!;
        public virtual DbSet<PromotionParticipant> PromotionParticipants { get; set; } = null!;
        public virtual DbSet<PromotionPlayer> PromotionPlayers { get; set; } = null!;
        public virtual DbSet<PromotionVipLevel> PromotionVipLevels { get; set; } = null!;
        public virtual DbSet<Setting> Settings { get; set; } = null!;
        public virtual DbSet<TransactionPromotion> TransactionPromotions { get; set; } = null!;
        public virtual DbSet<VipLevel> VipLevels { get; set; } = null!;

        public IDbConnection GetDbConnection() =>
            Database.GetDbConnection();

        public Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel level) =>
            Database.BeginTransactionAsync(level);

        public Task<bool> CanConnectionAsync() => Database.CanConnectAsync();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("dbo");
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);

            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.BrandGameConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.GameProviderConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PlayerConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PlayerBucketConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PlayerGameRebateAggregateConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PlayerVipLevelSeasonPromotionConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PlayerVipLevelUpgradePromotionConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionBonusConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionGameProviderConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionParticipantConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionPlayerConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.PromotionVipLevelConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.SettingConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.TransactionPromotionConfiguration());
            modelBuilder.ApplyConfiguration(new Domain.Entities.Configurations.VipLevelConfiguration());
        }
    }
}