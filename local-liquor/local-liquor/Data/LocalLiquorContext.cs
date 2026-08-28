using local_liquor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Data;

public class LocalLiquorContext : DbContext
{
    public LocalLiquorContext(DbContextOptions<LocalLiquorContext> options) : base(options) { }

    public DbSet<Wine> Wines => Set<Wine>();
    public DbSet<WineNote> WineNotes => Set<WineNote>();
    public DbSet<MarketEvent> MarketEvents => Set<MarketEvent>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Wine>(wine =>
        {
            wine.HasIndex(w => w.Slug).IsUnique();
            wine.HasMany(w => w.Notes)
                .WithOne(n => n.Wine!)
                .HasForeignKey(n => n.WineId)
                .OnDelete(DeleteBehavior.Cascade);

            // SQLite has no decimal type; store strength as a string so the value
            // round-trips exactly rather than through a float.
            wine.Property(w => w.AlcoholByVolume).HasConversion<string>();
        });

        model.Entity<MarketEvent>()
            .HasIndex(m => m.StartsOn);

        model.Entity<MediaAsset>()
            .HasIndex(m => m.Usage);
    }
}
