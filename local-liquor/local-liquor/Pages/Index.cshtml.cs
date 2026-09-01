using local_liquor.Data.Entities;
using local_liquor.Models;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace local_liquor.Pages;

public class IndexModel : PageModel
{
    private readonly WineService _wines;
    private readonly MarketService _markets;
    private readonly MediaService _media;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(WineService wines, MarketService markets, MediaService media,
        IStringLocalizer<SharedResource> localizer)
    {
        _wines = wines;
        _markets = markets;
        _media = media;
        _localizer = localizer;
    }

    public IReadOnlyList<WineView> Wines { get; private set; } = [];

    public IReadOnlyList<MarketView> Markets { get; private set; } = [];

    public IReadOnlyList<MediaAsset> Photos { get; private set; } = [];

    /// <summary>Index of the wine that starts centre stage, so markup and 3D agree.</summary>
    public int HeroIndex { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        Wines = await _wines.GetPublishedAsync(ct);
        Markets = await _markets.GetUpcomingAsync(4, ct);
        Photos = await _media.GetForAsync(MediaUsage.Frontpage, ct);
        HeroIndex = WineService.HeroIndex(Wines);

        ViewData["Title"] = _localizer["meta.home.title"].Value;
        ViewData["Description"] = _localizer["meta.home.description"].Value;
    }
}
