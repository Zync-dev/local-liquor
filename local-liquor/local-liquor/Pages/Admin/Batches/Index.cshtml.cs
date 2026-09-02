using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Batches;

public class BatchIndexModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public BatchIndexModel(LocalLiquorContext db) => _db = db;

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Still in a vessel.</summary>
    public List<Batch> Working { get; private set; } = [];

    /// <summary>Bottled or archived. The record of what has been made.</summary>
    public List<Batch> Finished { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var all = await _db.Batches
            .AsNoTracking()
            .Include(b => b.Wine)
            .Include(b => b.Steps)
            .OrderByDescending(b => b.StartedOn)
            .ToListAsync(ct);

        Working = all.Where(b => !b.IsFinished).OrderBy(b => b.StartedOn).ToList();
        Finished = all.Where(b => b.IsFinished).ToList();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var batch = await _db.Batches.FindAsync([id], ct);
        if (batch is not null)
        {
            _db.Batches.Remove(batch);
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = $"Batch {batch.Code} er slettet.";
        }

        return RedirectToPage();
    }
}
