using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Wines;

public partial class EditModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public EditModel(LocalLiquorContext db) => _db = db;

    [BindProperty]
    public WineForm Form { get; set; } = new();

    public bool IsNew => Form.Id == 0;

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken ct)
    {
        if (id is null)
        {
            Form = WineForm.Blank();
            return Page();
        }

        var wine = await _db.Wines.Include(w => w.Notes)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (wine is null) return NotFound();

        Form = WineForm.From(wine);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        Form.Slug = Slugify(string.IsNullOrWhiteSpace(Form.Slug) ? Form.NameDa : Form.Slug);

        if (string.IsNullOrEmpty(Form.Slug))
        {
            ModelState.AddModelError("Form.Slug", "Kunne ikke lave en adresse ud af navnet.");
        }
        else if (await _db.Wines.AnyAsync(w => w.Slug == Form.Slug && w.Id != Form.Id, ct))
        {
            ModelState.AddModelError("Form.Slug", "Den adresse er allerede taget af en anden vin.");
        }

        if (!ModelState.IsValid) return Page();

        var wine = Form.Id == 0
            ? new Wine()
            : await _db.Wines.Include(w => w.Notes).FirstOrDefaultAsync(w => w.Id == Form.Id, ct);

        if (wine is null) return NotFound();

        Form.ApplyTo(wine);
        wine.UpdatedAt = DateTimeOffset.UtcNow;

        if (Form.Id == 0)
        {
            wine.SortOrder = await _db.Wines.CountAsync(ct);
            _db.Wines.Add(wine);
        }

        // Exactly one wine is the hero, so setting it here clears it everywhere else.
        if (wine.IsHero)
        {
            await _db.Wines.Where(w => w.Id != wine.Id && w.IsHero)
                .ExecuteUpdateAsync(s => s.SetProperty(w => w.IsHero, false), ct);
        }

        await _db.SaveChangesAsync(ct);

        TempData["Flash"] = $"{wine.NameDa} er gemt.";
        return RedirectToPage("/Admin/Wines/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var wine = await _db.Wines.FindAsync([id], ct);
        if (wine is not null)
        {
            _db.Wines.Remove(wine);
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = $"{wine.NameDa} er slettet.";
        }

        return RedirectToPage("/Admin/Wines/Index");
    }

    [GeneratedRegex(@"[^a-z0-9]+")] private static partial Regex NonSlug();

    /// <summary>
    /// Danish letters have conventional transliterations — æ becomes ae, ø oe, å aa —
    /// which is what a Dane would expect /vin/blaabaer to look like.
    /// </summary>
    public static string Slugify(string input)
    {
        var lowered = input.Trim().ToLowerInvariant()
            .Replace("æ", "ae").Replace("ø", "oe").Replace("å", "aa")
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
            .Replace("é", "e").Replace("è", "e");

        return NonSlug().Replace(lowered, "-").Trim('-');
    }

    public class WineForm
    {
        public int Id { get; set; }

        [MaxLength(60)]
        public string Slug { get; set; } = "";

        [Required(ErrorMessage = "Navnet på etiketten mangler."), MaxLength(40)]
        public string LabelName { get; set; } = "";

        [Required(ErrorMessage = "Dansk navn mangler."), MaxLength(60)]
        public string NameDa { get; set; } = "";

        [Required(ErrorMessage = "Engelsk navn mangler."), MaxLength(60)]
        public string NameEn { get; set; } = "";

        [MaxLength(120)] public string TaglineDa { get; set; } = "";
        [MaxLength(120)] public string TaglineEn { get; set; } = "";
        [MaxLength(1200)] public string BodyDa { get; set; } = "";
        [MaxLength(1200)] public string BodyEn { get; set; } = "";
        [MaxLength(400)] public string ServingDa { get; set; } = "";
        [MaxLength(400)] public string ServingEn { get; set; } = "";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Farven skal være som #rrggbb.")]
        public string LiquidColor { get; set; } = "#d07c33";

        [Required, RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Farven skal være som #rrggbb.")]
        public string AccentColor { get; set; } = "#c0453c";

        [MaxLength(60)] public string SubtitleEn { get; set; } = "";
        [MaxLength(12)] public string Batch { get; set; } = "";
        [MaxLength(200)] public string IngredientsDa { get; set; } = "";
        [MaxLength(200)] public string IngredientsEn { get; set; } = "";

        [Range(0, 60, ErrorMessage = "Alkoholprocenten skal være mellem 0 og 60.")]
        public decimal AlcoholByVolume { get; set; } = 8m;

        [Range(1, 5000)] public int VolumeMl { get; set; } = 750;
        [Range(1, 12)] public int HarvestMonth { get; set; } = 7;
        [Range(0, 100000)] public int BatchSize { get; set; }

        public StockStatus Stock { get; set; } = StockStatus.Available;

        [Range(0, 100000)] public int? BottlesLeft { get; set; }

        public bool IsPublished { get; set; } = true;
        public bool IsHero { get; set; }

        /// <summary>Three note slots; blank ones are simply not saved.</summary>
        public List<NoteForm> Notes { get; set; } = [new(), new(), new()];

        public static WineForm Blank() => new();

        public static WineForm From(Wine wine)
        {
            var form = new WineForm
            {
                Id = wine.Id,
                Slug = wine.Slug,
                LabelName = wine.LabelName,
                NameDa = wine.NameDa,
                NameEn = wine.NameEn,
                TaglineDa = wine.TaglineDa,
                TaglineEn = wine.TaglineEn,
                BodyDa = wine.BodyDa,
                BodyEn = wine.BodyEn,
                ServingDa = wine.ServingDa,
                ServingEn = wine.ServingEn,
                LiquidColor = wine.LiquidColor,
                AccentColor = wine.AccentColor,
                SubtitleEn = wine.SubtitleEn,
                Batch = wine.Batch,
                IngredientsDa = wine.IngredientsDa,
                IngredientsEn = wine.IngredientsEn,
                AlcoholByVolume = wine.AlcoholByVolume,
                VolumeMl = wine.VolumeMl,
                HarvestMonth = wine.HarvestMonth,
                BatchSize = wine.BatchSize,
                Stock = wine.Stock,
                BottlesLeft = wine.BottlesLeft,
                IsPublished = wine.IsPublished,
                IsHero = wine.IsHero,
                Notes = wine.Notes.OrderBy(n => n.SortOrder)
                    .Select(n => new NoteForm { TextDa = n.TextDa, TextEn = n.TextEn })
                    .ToList(),
            };

            while (form.Notes.Count < 3) form.Notes.Add(new NoteForm());
            return form;
        }

        public void ApplyTo(Wine wine)
        {
            wine.Slug = Slug;
            wine.LabelName = LabelName.ToUpperInvariant();
            wine.NameDa = NameDa;
            wine.NameEn = NameEn;
            wine.TaglineDa = TaglineDa;
            wine.TaglineEn = TaglineEn;
            wine.BodyDa = BodyDa;
            wine.BodyEn = BodyEn;
            wine.ServingDa = ServingDa;
            wine.ServingEn = ServingEn;
            wine.LiquidColor = LiquidColor.ToLowerInvariant();
            wine.AccentColor = AccentColor.ToLowerInvariant();
            wine.SubtitleEn = SubtitleEn.ToUpperInvariant();
            wine.Batch = Batch;
            wine.IngredientsDa = IngredientsDa;
            wine.IngredientsEn = IngredientsEn;
            wine.AlcoholByVolume = AlcoholByVolume;
            wine.VolumeMl = VolumeMl;
            wine.HarvestMonth = HarvestMonth;
            wine.BatchSize = BatchSize;
            wine.Stock = Stock;
            wine.BottlesLeft = Stock == StockStatus.SoldOut ? 0 : BottlesLeft;
            wine.IsPublished = IsPublished;
            wine.IsHero = IsHero;

            wine.Notes.Clear();
            var order = 0;
            foreach (var note in Notes.Where(n => !string.IsNullOrWhiteSpace(n.TextDa)))
            {
                wine.Notes.Add(new WineNote
                {
                    TextDa = note.TextDa.Trim(),
                    TextEn = string.IsNullOrWhiteSpace(note.TextEn) ? note.TextDa.Trim() : note.TextEn.Trim(),
                    SortOrder = order++,
                });
            }
        }
    }

    public class NoteForm
    {
        [MaxLength(60)] public string TextDa { get; set; } = "";
        [MaxLength(60)] public string TextEn { get; set; } = "";
    }
}
