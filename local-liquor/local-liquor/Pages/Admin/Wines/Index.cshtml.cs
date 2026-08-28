using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Wines;

public class WinesIndexModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public WinesIndexModel(LocalLiquorContext db) => _db = db;

    public List<Wine> Wines { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct) => Wines = await LoadAsync(ct);

    /// <summary>
    /// Swaps a wine with its neighbour. Positions are renumbered from scratch each
    /// time, so a list that arrived with duplicate or gapped SortOrders sorts itself
    /// out rather than getting stuck.
    /// </summary>
    public async Task<IActionResult> OnPostMoveAsync(int id, int direction, CancellationToken ct)
    {
        var wines = await LoadAsync(ct, tracking: true);
        var index = wines.FindIndex(w => w.Id == id);
        var target = index + Math.Sign(direction);

        if (index >= 0 && target >= 0 && target < wines.Count)
        {
            (wines[index], wines[target]) = (wines[target], wines[index]);
        }

        for (var i = 0; i < wines.Count; i++)
        {
            wines[i].SortOrder = i;
        }

        await _db.SaveChangesAsync(ct);
        return RedirectToPage();
    }

    private Task<List<Wine>> LoadAsync(CancellationToken ct, bool tracking = false)
    {
        var query = _db.Wines.OrderBy(w => w.SortOrder).ThenBy(w => w.Id);
        return (tracking ? query : query.AsNoTracking()).ToListAsync(ct);
    }
}
