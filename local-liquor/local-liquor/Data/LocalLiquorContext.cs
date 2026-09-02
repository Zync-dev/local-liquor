using local_liquor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Data;

public class LocalLiquorContext : DbContext
{
    public LocalLiquorContext(DbContextOptions<LocalLiquorContext> options) : base(options) { }

    public DbSet<Wine> Wines => Set<Wine>();
    public DbSet<WineNote> WineNotes => Set<WineNote>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<BatchStep> BatchSteps => Set<BatchStep>();

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

        model.Entity<MediaAsset>()
            .HasIndex(m => m.Usage);

        // The unread ones are what the admin opens on; the rest is history. There
        // is deliberately no index on ReceivedAt: SQLite cannot order or compare a
        // DateTimeOffset, so nothing could ever use it. The inbox sorts by Id.
        model.Entity<ContactMessage>()
            .HasIndex(m => m.IsRead);

        model.Entity<Batch>(batch =>
        {
            batch.HasIndex(b => b.Stage);
            batch.HasIndex(b => b.StartedOn);

            // Deleting a wine must not take its history with it: the batch stays,
            // it just stops pointing at a listing.
            batch.HasOne(b => b.Wine)
                .WithMany()
                .HasForeignKey(b => b.WineId)
                .OnDelete(DeleteBehavior.SetNull);

            batch.HasMany(b => b.Steps)
                .WithOne(s => s.Batch!)
                .HasForeignKey(s => s.BatchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Same reason as the wine's strength: SQLite has no decimal.
            foreach (var property in new[]
            {
                nameof(Batch.Litres), nameof(Batch.FruitKg), nameof(Batch.SugarKg),
                nameof(Batch.StartGravity), nameof(Batch.EndGravity),
            })
            {
                batch.Property(property).HasConversion<string>();
            }
        });

        // The dashboard's whole job is "what is due", so this is the index it runs on.
        model.Entity<BatchStep>()
            .HasIndex(s => new { s.DoneOn, s.DueOn });
    }
}
