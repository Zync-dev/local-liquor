using System.ComponentModel.DataAnnotations;
using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Batches;

public class BatchEditModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public BatchEditModel(LocalLiquorContext db) => _db = db;

    [BindProperty]
    public BatchForm Form { get; set; } = new();

    /// <summary>The saved steps, read-only. They are edited through their own handlers.</summary>
    public List<BatchStep> Steps { get; private set; } = [];

    public List<SelectListItem> WineOptions { get; private set; } = [];

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);

    public bool IsNew => Form.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        await LoadWinesAsync(ct);

        if (id is null)
        {
            Form = BatchForm.Blank(await NextCodeAsync(ct));
            return Page();
        }

        var batch = await _db.Batches
            .Include(b => b.Steps)
            .FirstOrDefaultAsync(b => b.Id == id, ct);

        if (batch is null) return NotFound();

        Form = BatchForm.From(batch);
        Steps = Ordered(batch.Steps);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return await BackToFormAsync(ct);

        var batch = Form.Id == 0
            ? new Batch()
            : await _db.Batches.Include(b => b.Steps).FirstOrDefaultAsync(b => b.Id == Form.Id, ct);

        if (batch is null) return NotFound();

        var isNew = Form.Id == 0;
        Form.ApplyTo(batch);
        batch.UpdatedAt = DateTimeOffset.UtcNow;

        if (isNew)
        {
            _db.Batches.Add(batch);

            // A new batch starts with the steps a fruit wine normally needs, dated
            // from the day the fruit went in. They are a starting point: every one
            // of them can be moved, renamed or deleted afterwards.
            if (Form.AddDefaultSteps)
            {
                var order = 0;
                foreach (var template in StepTemplate.Default)
                {
                    batch.Steps.Add(new BatchStep
                    {
                        Title = template.Title,
                        DueOn = batch.StartedOn.AddDays(template.DayOffset),
                        SortOrder = order++,
                    });
                }
            }
        }

        await _db.SaveChangesAsync(ct);

        TempData["Flash"] = $"Batch {batch.Code} er gemt.";
        return RedirectToPage(new { id = batch.Id });
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

        return RedirectToPage("Index");
    }

    /* ------------------------------------------------------------- steps -- */

    public async Task<IActionResult> OnPostAddStepAsync(int id, string title, DateOnly dueOn,
        string? notes, CancellationToken ct)
    {
        var batch = await _db.Batches.Include(b => b.Steps).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (batch is null) return NotFound();

        if (string.IsNullOrWhiteSpace(title))
        {
            TempData["Flash"] = "Trinnet mangler en titel.";
            return RedirectToPage(new { id });
        }

        batch.Steps.Add(new BatchStep
        {
            Title = title.Trim(),
            DueOn = dueOn == default ? Today : dueOn,
            Notes = notes?.Trim() ?? "",
            SortOrder = batch.Steps.Count == 0 ? 0 : batch.Steps.Max(s => s.SortOrder) + 1,
        });

        await _db.SaveChangesAsync(ct);
        return RedirectToPage(new { id });
    }

    /// <summary>Ticks a step off, or puts it back if it was ticked by mistake.</summary>
    public async Task<IActionResult> OnPostToggleStepAsync(int id, int stepId, CancellationToken ct)
    {
        var step = await _db.BatchSteps.FindAsync([stepId], ct);
        if (step is not null)
        {
            step.DoneOn = step.DoneOn is null ? Today : null;
            await _db.SaveChangesAsync(ct);
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteStepAsync(int id, int stepId, CancellationToken ct)
    {
        var step = await _db.BatchSteps.FindAsync([stepId], ct);
        if (step is not null)
        {
            _db.BatchSteps.Remove(step);
            await _db.SaveChangesAsync(ct);
        }

        return RedirectToPage(new { id });
    }

    /* ------------------------------------------------------------- plumbing */

    private async Task<IActionResult> BackToFormAsync(CancellationToken ct)
    {
        await LoadWinesAsync(ct);
        if (Form.Id != 0)
        {
            Steps = Ordered(await _db.BatchSteps.AsNoTracking()
                .Where(s => s.BatchId == Form.Id)
                .ToListAsync(ct));
        }

        return Page();
    }

    private async Task LoadWinesAsync(CancellationToken ct)
    {
        var wines = await _db.Wines.AsNoTracking()
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Id)
            .Select(w => new { w.Id, w.NameDa })
            .ToListAsync(ct);

        WineOptions =
        [
            new SelectListItem("— ikke knyttet til en vin —", ""),
            .. wines.Select(w => new SelectListItem(w.NameDa, w.Id.ToString())),
        ];
    }

    /// <summary>Suggests the next batch number, so nobody has to look up the last one.</summary>
    private async Task<string> NextCodeAsync(CancellationToken ct)
    {
        var codes = await _db.Batches.AsNoTracking().Select(b => b.Code).ToListAsync(ct);
        var highest = codes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return (highest + 1).ToString("00");
    }

    /// <summary>Open steps by date, then the finished ones underneath.</summary>
    private static List<BatchStep> Ordered(IEnumerable<BatchStep> steps) =>
        steps.OrderBy(s => s.DoneOn is not null)
             .ThenBy(s => s.DueOn)
             .ThenBy(s => s.SortOrder)
             .ToList();

    public class BatchForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Batchet mangler et nummer."), MaxLength(12)]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "Skriv hvad det er."), MaxLength(80)]
        public string Title { get; set; } = "";

        public int? WineId { get; set; }

        public BatchStage Stage { get; set; } = BatchStage.Planned;

        [DataType(DataType.Date)] public DateOnly StartedOn { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        [DataType(DataType.Date)] public DateOnly? BottledOn { get; set; }

        [Range(0, 10000)] public decimal Litres { get; set; }
        [Range(0, 10000)] public decimal FruitKg { get; set; }
        [Range(0, 10000)] public decimal SugarKg { get; set; }

        [Range(0.9, 1.3, ErrorMessage = "Vægtfylden skal være mellem 0,9 og 1,3.")]
        public decimal? StartGravity { get; set; }

        [Range(0.9, 1.3, ErrorMessage = "Vægtfylden skal være mellem 0,9 og 1,3.")]
        public decimal? EndGravity { get; set; }

        [Range(0, 100000)] public int BottleCount { get; set; }

        [MaxLength(4000)] public string Notes { get; set; } = "";

        /// <summary>Only offered on a new batch.</summary>
        public bool AddDefaultSteps { get; set; } = true;

        public static BatchForm Blank(string code) => new() { Code = code };

        public static BatchForm From(Batch batch) => new()
        {
            Id = batch.Id,
            Code = batch.Code,
            Title = batch.Title,
            WineId = batch.WineId,
            Stage = batch.Stage,
            StartedOn = batch.StartedOn,
            BottledOn = batch.BottledOn,
            Litres = batch.Litres,
            FruitKg = batch.FruitKg,
            SugarKg = batch.SugarKg,
            StartGravity = batch.StartGravity,
            EndGravity = batch.EndGravity,
            BottleCount = batch.BottleCount,
            Notes = batch.Notes,
        };

        public void ApplyTo(Batch batch)
        {
            batch.Code = Code.Trim();
            batch.Title = Title.Trim();
            batch.WineId = WineId;
            batch.Stage = Stage;
            batch.StartedOn = StartedOn;
            batch.BottledOn = BottledOn;
            batch.Litres = Litres;
            batch.FruitKg = FruitKg;
            batch.SugarKg = SugarKg;
            batch.StartGravity = StartGravity;
            batch.EndGravity = EndGravity;
            batch.BottleCount = BottleCount;
            batch.Notes = Notes;
        }
    }
}
