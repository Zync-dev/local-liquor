using System.ComponentModel.DataAnnotations;
using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace local_liquor.Pages.Admin.Markets;

public class MarketEditModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public MarketEditModel(LocalLiquorContext db) => _db = db;

    [BindProperty]
    public MarketForm Form { get; set; } = new();

    public bool IsNew => Form.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is null)
        {
            Form = new MarketForm { StartsOn = DateOnly.FromDateTime(DateTime.UtcNow) };
            return Page();
        }

        var market = await _db.MarketEvents.FindAsync([id.Value], ct);
        if (market is null) return NotFound();

        Form = MarketForm.From(market);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (Form.EndsOn is { } end && end < Form.StartsOn)
        {
            ModelState.AddModelError("Form.EndsOn", "Sidste dag kan ikke ligge før første dag.");
        }

        if (!ModelState.IsValid) return Page();

        var market = Form.Id == 0
            ? new MarketEvent()
            : await _db.MarketEvents.FindAsync([Form.Id], ct);

        if (market is null) return NotFound();

        Form.ApplyTo(market);

        if (Form.Id == 0) _db.MarketEvents.Add(market);

        await _db.SaveChangesAsync(ct);
        TempData["Flash"] = $"{market.TitleDa} er gemt.";
        return RedirectToPage("/Admin/Markets/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var market = await _db.MarketEvents.FindAsync([id], ct);
        if (market is not null)
        {
            _db.MarketEvents.Remove(market);
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = "Markedet er slettet.";
        }

        return RedirectToPage("/Admin/Markets/Index");
    }

    public class MarketForm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Dansk titel mangler."), MaxLength(120)]
        public string TitleDa { get; set; } = "";

        [Required(ErrorMessage = "Engelsk titel mangler."), MaxLength(120)]
        public string TitleEn { get; set; } = "";

        [Required(ErrorMessage = "Stedet mangler."), MaxLength(160)]
        public string Place { get; set; } = "";

        [MaxLength(200)] public string? Address { get; set; }

        [Required(ErrorMessage = "Første dag mangler.")]
        public DateOnly StartsOn { get; set; }

        public DateOnly? EndsOn { get; set; }

        [MaxLength(60)] public string? Hours { get; set; }

        [MaxLength(400), Url(ErrorMessage = "Linket ser ikke ud som en adresse.")]
        public string? Url { get; set; }

        public bool IsPublished { get; set; } = true;

        public static MarketForm From(MarketEvent m) => new()
        {
            Id = m.Id,
            TitleDa = m.TitleDa,
            TitleEn = m.TitleEn,
            Place = m.Place,
            Address = m.Address,
            StartsOn = m.StartsOn,
            EndsOn = m.EndsOn,
            Hours = m.Hours,
            Url = m.Url,
            IsPublished = m.IsPublished,
        };

        public void ApplyTo(MarketEvent m)
        {
            m.TitleDa = TitleDa;
            m.TitleEn = string.IsNullOrWhiteSpace(TitleEn) ? TitleDa : TitleEn;
            m.Place = Place;
            m.Address = string.IsNullOrWhiteSpace(Address) ? null : Address;
            m.StartsOn = StartsOn;
            m.EndsOn = EndsOn;
            m.Hours = string.IsNullOrWhiteSpace(Hours) ? null : Hours;
            m.Url = string.IsNullOrWhiteSpace(Url) ? null : Url;
            m.IsPublished = IsPublished;
        }
    }
}
