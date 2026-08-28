using System.Globalization;
using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin;

public class AdminIndexModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public AdminIndexModel(LocalLiquorContext db) => _db = db;

    public List<Wine> Wines { get; private set; } = [];
    public int PublishedWines { get; private set; }
    public int HiddenWines { get; private set; }
    public int SoldOutWines { get; private set; }
    public int UpcomingMarkets { get; private set; }
    public string? NextMarket { get; private set; }
    public int Photos { get; private set; }

    /// <summary>Things worth saying out loud on the front page of the admin.</summary>
    public List<string> Attention { get; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        Wines = await _db.Wines.AsNoTracking()
            .Include(w => w.Notes)
            .OrderBy(w => w.SortOrder).ThenBy(w => w.Id)
            .ToListAsync(ct);

        PublishedWines = Wines.Count(w => w.IsPublished);
        HiddenWines = Wines.Count(w => !w.IsPublished);
        SoldOutWines = Wines.Count(w => w.Stock == StockStatus.SoldOut && w.IsPublished);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var markets = await _db.MarketEvents.AsNoTracking()
            .Where(m => m.IsPublished && (m.EndsOn ?? m.StartsOn) >= today)
            .OrderBy(m => m.StartsOn)
            .ToListAsync(ct);

        UpcomingMarkets = markets.Count;
        var da = CultureInfo.GetCultureInfo("da-DK");
        NextMarket = markets.Count == 0
            ? null
            : $"{markets[0].StartsOn.ToDateTime(TimeOnly.MinValue).ToString("d. MMM", da)} · {markets[0].Place}";

        Photos = await _db.MediaAssets.CountAsync(ct);

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
}
