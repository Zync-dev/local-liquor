using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;

namespace local_liquor.Pages;

public class PrivacyModel : PageModel
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public PrivacyModel(IStringLocalizer<SharedResource> localizer) => _localizer = localizer;

    public void OnGet() => ViewData["Title"] = $"{_localizer["meta.privacy.title"].Value} — Local Liquor";
}
