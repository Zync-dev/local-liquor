using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Services;

/// <summary>
/// The single operator account.
///
/// There is no registration and no default password: until someone visits the
/// setup page and chooses one, no account exists and nothing can be signed into.
/// Hashing is ASP.NET's own <see cref="PasswordHasher{T}"/> (PBKDF2, salted,
/// 210k iterations by default) — the same primitive Identity uses, without the
/// user-management machinery a one-person site has no use for.
/// </summary>
public sealed class AdminAccountService
{
    /// <summary>Short enough to be typed, long enough not to be guessed.</summary>
    public const int MinimumPasswordLength = 12;

    private readonly LocalLiquorContext _db;
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public AdminAccountService(LocalLiquorContext db) => _db = db;

    public Task<bool> ExistsAsync(CancellationToken ct = default) =>
        _db.AdminUsers.AnyAsync(ct);

    /// <summary>Creates the account. Refuses if one already exists.</summary>
    public async Task<bool> CreateAsync(string password, CancellationToken ct = default)
    {
        if (password.Length < MinimumPasswordLength) return false;
        if (await ExistsAsync(ct)) return false;

        var user = new AdminUser();
        user.PasswordHash = _hasher.HashPassword(user, password);
        _db.AdminUsers.Add(user);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> VerifyAsync(string password, CancellationToken ct = default)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync(ct);
        if (user is null) return false;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return false;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        user.LastSignedInAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ChangePasswordAsync(string current, string next, CancellationToken ct = default)
    {
        if (next.Length < MinimumPasswordLength) return false;
        if (!await VerifyAsync(current, ct)) return false;

        var user = await _db.AdminUsers.FirstAsync(ct);
        user.PasswordHash = _hasher.HashPassword(user, next);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
