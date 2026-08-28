using System.ComponentModel.DataAnnotations;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace local_liquor.Pages.Admin;

/// <summary>
/// First-run account creation. Reachable only while no administrator exists, so
/// it cannot be used to take over an account later.
/// </summary>
[EnableRateLimiting(Program.LoginRateLimit)]
public class SetupModel : PageModel
{
    private readonly AdminAccountService _accounts;

    public SetupModel(AdminAccountService accounts) => _accounts = accounts;

    [BindProperty, Required(ErrorMessage = "Skriv en adgangskode.")]
    [MinLength(AdminAccountService.MinimumPasswordLength,
        ErrorMessage = "Adgangskoden skal være mindst 12 tegn.")]
    public string Password { get; set; } = "";

    [BindProperty, Required(ErrorMessage = "Gentag adgangskoden.")]
    [Compare(nameof(Password), ErrorMessage = "De to koder er ikke ens.")]
    public string Confirm { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(CancellationToken ct) =>
        await _accounts.ExistsAsync(ct) ? RedirectToPage("/Admin/Login") : Page();

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (await _accounts.ExistsAsync(ct))
        {
            return RedirectToPage("/Admin/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await _accounts.CreateAsync(Password, ct))
        {
            ModelState.AddModelError(string.Empty, "Kunne ikke oprette administratoren.");
            return Page();
        }

        await AdminSession.SignInAsync(HttpContext);
        return RedirectToPage("/Admin/Index");
    }
}
