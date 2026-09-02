using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin;

/// <summary>
/// The production board. It answers one question first — what has to happen to
/// the wine this week — and only then reports on the site.
/// </summary>
public class AdminIndexModel : PageModel
{
    /// <summary>How far ahead the board looks. Beyond this it is not this week's problem.</summary>
    private const int HorizonDays = 21;

    private readonly LocalLiquorContext _db;

    public AdminIndexModel(LocalLiquorContext db) => _db = db;

    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);

    public List<BatchStep> Due { get; private set; } = [];
    public List<Batch> Active { get; private set; } = [];

    public int ActiveBatches { get; private set; }
    public decimal LitresWorking { get; private set; }
    public int BottlesOnHand { get; private set; }
    public int UnreadMessages { get; private set; }

    public List<Wine> Wines { get; private set; } = [];
    public int PublishedWines { get; private set; }
    public int HiddenWines { get; private set; }
    public int SoldOutWines { get; private set; }
    public int Photos { get; private set; }

    /// <summary>Things worth saying out loud on the front page of the admin.</summary>
    public List<string> Attention { get; } = [];

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    /// <summary>Ticks a step off without leaving the board.</summary>
    public async Task<IActionResult> OnPostDoneAsync(int id, CancellationToken ct)
    {
        var step = await _db.BatchSteps.FindAsync([id], ct);
        if (step is not null)
        {
            step.DoneOn = DateOnly.FromDateTime(DateTime.Now);
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = $"{step.Title} er sat som klaret.";
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        var horizon = Today.AddDays(HorizonDays);

        // Everything still open and either late or coming up. Steps belonging to a
        // finished batch are left out — they are history, not work.
        Due = await _db.BatchSteps
            .AsNoTracking()
            .Include(s => s.Batch)
            .Where(s => s.DoneOn == null
                        && s.Batch!.Stage < BatchStage.Bottled
                        && s.DueOn <= horizon)
            .OrderBy(s => s.DueOn)
            .ToListAsync(ct);

        Active = await _db.Batches
            .AsNoTracking()
            .Include(b => b.Wine)
            .Include(b => b.Steps)
            .Where(b => b.Stage < BatchStage.Bottled)
            .OrderBy(b => b.StartedOn)
            .ToListAsync(ct);

        ActiveBatches = Active.Count;
        LitresWorking = Active.Sum(b => b.Litres);

        BottlesOnHand = await _db.Batches
            .Where(b => b.Stage == BatchStage.Bottled)
            .SumAsync(b => b.BottleCount, ct);

        UnreadMessages = await _db.ContactMessages.CountAsync(m => !m.IsRead, ct);

        Wines = await _db.Wines.AsNoTracking()
            .Include(w => w.Notes)
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Id)
            .ToListAsync(ct);

        PublishedWines = Wines.Count(w => w.IsPublished);
        HiddenWines = Wines.Count(w => !w.IsPublished);
        SoldOutWines = Wines.Count(w => w.Stock == StockStatus.SoldOut && w.IsPublished);
        Photos = await _db.MediaAssets.CountAsync(ct);

        var late = Due.Count(s => s.DueOn < Today);
        if (late > 0)
        {
            Attention.Add(late == 1
                ? "Én opgave er overskredet."
                : $"{late} opgaver er overskredet.");
        }

        foreach (var batch in Active.Where(b => b.Steps.All(s => s.DoneOn != null)))
        {
            Attention.Add($"Batch {batch.Code} har ingen næste opgave — er den klar til næste trin?");
        }

        if (UnreadMessages > 0)
        {
            Attention.Add(UnreadMessages == 1
                ? "Der er én ulæst besked."
                : $"Der er {UnreadMessages} ulæste beskeder.");
        }

        if (!Wines.Any(w => w.IsHero && w.IsPublished))
        {
            Attention.Add("Ingen vin er valgt som hovedflaske — forsiden vælger selv den midterste.");
        }

        foreach (var wine in Wines.Where(w => w.IsPublished && w.Stock == StockStatus.SoldOut))
        {
            Attention.Add($"{wine.NameDa} er markeret som udsolgt.");
        }

        foreach (var wine in Wines.Where(w => w.IsPublished && string.IsNullOrWhiteSpace(w.BodyEn)))
        {
            Attention.Add($"{wine.NameDa} mangler engelsk beskrivelse.");
        }
    }

    /// <summary>Danish label for a stage, so the enum never reaches the screen.</summary>
    public static string StageName(BatchStage stage) => stage switch
    {
        BatchStage.Planned => "Planlagt",
        BatchStage.Primary => "Hovedgæring",
        BatchStage.Secondary => "Eftergæring",
        BatchStage.Ageing => "Lagring",
        BatchStage.Bottled => "Aftappet",
        BatchStage.Archived => "Arkiveret",
        _ => stage.ToString(),
    };

    /// <summary>"om 3 dage", "i dag", "4 dage over" — the whole point of the board.</summary>
    public static string Countdown(int days) => days switch
    {
        0 => "i dag",
        1 => "i morgen",
        -1 => "1 dag over",
        < 0 => $"{-days} dage over",
        _ => $"om {days} dage",
    };
}
