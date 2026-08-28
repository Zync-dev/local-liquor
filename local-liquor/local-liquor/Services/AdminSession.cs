using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace local_liquor.Services;

/// <summary>
/// Issues the admin cookie. There is one account, so the principal carries only
/// enough to identify it — no roles, no user store lookup on every request.
/// </summary>
public static class AdminSession
{
    public static Task SignInAsync(HttpContext http, bool persistent = true)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "Local Liquor")],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return http.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = persistent });
    }
}
