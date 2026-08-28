using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace local_liquor.Pages;

/// <summary>
/// Stores the visitor's language choice in a cookie and sends them back where they were.
/// Has no view of its own — the header posts to it.
/// </summary>
public class SprogModel : PageModel
{
    private static readonly string[] Supported = ["da", "en"];

    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost(string? culture, string? returnUrl)
    {
        if (Supported.Contains(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture!)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    HttpOnly = true,
                });
        }

        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToPage("/Index");
    }
}
