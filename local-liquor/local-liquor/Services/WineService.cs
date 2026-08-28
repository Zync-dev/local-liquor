using System.Globalization;
using local_liquor.Data;
using local_liquor.Data.Entities;
using local_liquor.Models;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Services;

/// <summary>Reads the range for the public site, already resolved to one language.</summary>
public sealed class WineService
{
    private readonly LocalLiquorContext _db;

    public WineService(LocalLiquorContext db) => _db = db;

    private static bool Danish =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "da";

    private IQueryable<Wine> Published => _db.Wines
        .AsNoTracking()
        .Include(w => w.Notes)
        .Where(w => w.IsPublished)
        .OrderBy(w => w.SortOrder)
        .ThenBy(w => w.Id);

    public async Task<IReadOnlyList<WineView>> GetPublishedAsync(CancellationToken ct = default)
    {
        var danish = Danish;
        var wines = await Published.ToListAsync(ct);
        return wines.Select(w => WineView.From(w, danish)).ToList();
    }

    public async Task<WineView?> FindAsync(string? slug, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(slug)) return null;
        var danish = Danish;
        var wine = await Published.FirstOrDefaultAsync(
            w => EF.Functions.Like(w.Slug, slug), ct);
        return wine is null ? null : WineView.From(wine, danish);
    }

    /// <summary>
    /// Index of the wine that starts centre stage, within the published list, so
    /// the markup and the 3D scene agree on which bottle is in front.
    /// </summary>
    public static int HeroIndex(IReadOnlyList<WineView> wines)
    {
        var index = wines.ToList().FindIndex(w => w.IsHero);
        if (index >= 0) return index;
        // Nothing flagged: put the middle bottle in front rather than the first.
        return wines.Count == 0 ? 0 : wines.Count / 2;
    }
}
