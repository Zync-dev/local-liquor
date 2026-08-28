using System.Globalization;
using local_liquor.Data;
using local_liquor.Models;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Services;

/// <summary>Upcoming markets and events, resolved to one language.</summary>
public sealed class MarketService
{
    private readonly LocalLiquorContext _db;

    public MarketService(LocalLiquorContext db) => _db = db;

    /// <summary>
    /// Markets that have not finished yet. A market runs until the end of its last
    /// day, so a one-day market stays listed for the whole of that day.
    /// </summary>
    public async Task<IReadOnlyList<MarketView>> GetUpcomingAsync(int take = 4, CancellationToken ct = default)
    {
        var danish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "da";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var events = await _db.MarketEvents
            .AsNoTracking()
            .Where(m => m.IsPublished && (m.EndsOn ?? m.StartsOn) >= today)
            .OrderBy(m => m.StartsOn)
            .Take(take)
            .ToListAsync(ct);

        return events.Select(m => MarketView.From(m, danish)).ToList();
    }
}
