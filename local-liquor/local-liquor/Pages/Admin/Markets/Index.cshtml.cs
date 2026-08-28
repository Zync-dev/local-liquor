using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Markets;

public class MarketsIndexModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public MarketsIndexModel(LocalLiquorContext db) => _db = db;

    public List<MarketEvent> Upcoming { get; private set; } = [];
    public List<MarketEvent> Past { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _db.MarketEvents.AsNoTracking()
            .OrderBy(m => m.StartsOn)
            .ToListAsync(ct);

        Upcoming = all.Where(m => (m.EndsOn ?? m.StartsOn) >= today).ToList();
        Past = all.Where(m => (m.EndsOn ?? m.StartsOn) < today)
            .OrderByDescending(m => m.StartsOn)
            .ToList();
    }
}
