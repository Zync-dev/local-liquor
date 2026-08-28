using System.ComponentModel.DataAnnotations;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace local_liquor.Pages.Admin;

[EnableRateLimiting(Program.LoginRateLimit)]
public class LoginModel : PageModel
{
    private readonly AdminAccountService _accounts;

    public LoginModel(AdminAccountService accounts) => _accounts = accounts;

    [BindProperty, Required(ErrorMessage = "Skriv adgangskoden.")]
    public string Password { get; set; } = "";

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        // No account yet: send them to create one instead of a login they cannot pass.
        if (!await _accounts.ExistsAsync(ct))
        {
            return RedirectToPage("/Admin/Setup");
        }

        return User.Identity?.IsAuthenticated == true
            ? RedirectToPage("/Admin/Index")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!await _accounts.VerifyAsync(Password, ct))
        {
            // Deliberately vague: there is only one account, so naming what was
            // wrong would only help someone guessing.
            ModelState.AddModelError(string.Empty, "Forkert adgangskode.");
            return Page();
        }

        await AdminSession.SignInAsync(HttpContext);

        return Url.IsLocalUrl(ReturnUrl) ? LocalRedirect(ReturnUrl!) : RedirectToPage("/Admin/Index");
    }
}
