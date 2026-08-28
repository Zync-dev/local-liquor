using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>
/// The single operator account. There is deliberately no registration flow: the
/// password is set once, on first run, from the setup page.
/// </summary>
public class AdminUser
{
    public int Id { get; set; }

    [Required, MaxLength(400)] public string PasswordHash { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastSignedInAt { get; set; }
}
