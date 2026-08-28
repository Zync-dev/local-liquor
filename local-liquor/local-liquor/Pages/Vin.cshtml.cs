using local_liquor.Models;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace local_liquor.Pages;

public class VinModel : PageModel
{
    private readonly WineService _wines;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public VinModel(WineService wines, IStringLocalizer<SharedResource> localizer)
    {
        _wines = wines;
        _localizer = localizer;
    }

    public WineView Wine { get; private set; } = default!;

    /// <summary>The rest of the range, for the strip at the foot of the page.</summary>
    public IReadOnlyList<WineView> Others { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken ct)
    {
        var wine = await _wines.FindAsync(slug, ct);
        if (wine is null)
        {
            return NotFound();
        }

        Wine = wine;
        Others = (await _wines.GetPublishedAsync(ct)).Where(w => w.Slug != wine.Slug).ToList();

        ViewData["Title"] = $"{wine.Name} — Local Liquor";
        ViewData["Description"] = $"{wine.Name}: {wine.Tagline} {wine.Body}";
        return Page();
    }
}
