using Api.Domain.Contracts;
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.EntityFramework;

public class PremPointsDbContext : DbContext
{
    public PremPointsDbContext(DbContextOptions<PremPointsDbContext> options) : base(options) { }

    public DbSet<UserEntity> Users { get; set; }
    public DbSet<TeamEntity> Teams { get; set; }
    public DbSet<SeasonEntity> Seasons { get; set; }
    public DbSet<UserSeasonEntity> UserSeasons { get; set; }
    public DbSet<TeamSeasonEntity> TeamSeasons { get; set; }
    public DbSet<SeasonPeriodEntity> SeasonPeriods { get; set; }
    public DbSet<PriceEntity> Prices { get; set; }
    public DbSet<TradeEntity> Trades { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<SeasonEntity>(entity =>
        {
            entity.HasIndex(e => new { e.StartYear }).IsUnique();
            entity.Property(x => x.StartYear).IsRequired();
        });
        modelBuilder.Entity<SeasonPeriodEntity>(entity =>
        {
            entity.Property(x => x.PeriodStartDate).IsRequired();
            entity.Property(x => x.PeriodEndDate).IsRequired();
            entity.HasIndex(e => new { e.PeriodStartDate }).IsUnique();
            entity.HasIndex(e => new { e.PeriodEndDate }).IsUnique();
        });
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.Property(x => x.Username).IsRequired();
            entity.Property(x => x.FirstName).IsRequired();
            entity.Property(x => x.Role).IsRequired();

            entity.HasIndex(e => new { e.Username }).IsUnique();
        });
        modelBuilder.Entity<TeamEntity>(entity =>
        {
            entity.Property(x => x.TeamName).IsRequired();
            entity.HasIndex(e => new { e.TeamName }).IsUnique();
        });
        modelBuilder.Entity<PriceEntity>(entity =>
        {
            entity.Property(x => x.Bid).HasPrecision(18, 2).IsRequired();
            entity.Property(x => x.Ask).HasPrecision(18, 2).IsRequired();

            // Computed and stored by SQL Server, so the mid cannot drift from
            // the spread it comes from and can still be filtered and ordered on.
            entity.Property(x => x.Mid)
                  .HasPrecision(18, 4)
                  .HasComputedColumnSql("(([Bid] + [Ask]) / 2.0)", stored: true);

            // One price per team per value date. The upsert in CreatePriceHandler
            // relies on this being true.
            entity.HasIndex(e => new { e.TeamId, e.ValueDate }).IsUnique();
        });
        modelBuilder.Entity<TradeEntity>(entity =>
        {
            entity.Property(x => x.PriceId).IsRequired();
            entity.Property(x => x.TradeType).IsRequired();
            entity.Property(x => x.Exposure).IsRequired();
            entity.Property(x => x.TimezoneIana).IsRequired();
            //One user per price per week 
            entity.HasIndex(e => new { e.UserId, e.PriceId }).IsUnique();
        });

        foreach (var relationship in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            relationship.DeleteBehavior = DeleteBehavior.Restrict;
        }

        //Stops the db generating primary keys
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(entityType => typeof(IAuditableEntity).IsAssignableFrom(entityType.ClrType))
        )
        {
            var idProperty = entityType.FindProperty("Id");
            if (idProperty != null && idProperty.ClrType == typeof(Guid))
            {
                idProperty.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }
    }
}
