using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>
/// A message from the contact form. Kept in the database rather than mailed on:
/// the site has no mail server and no secrets to configure, and this way nothing
/// is lost if a forwarding address stops working. They are read in the admin.
/// </summary>
public class ContactMessage
{
    public int Id { get; set; }

    [Required, MaxLength(80)] public string Name { get; set; } = "";

    [Required, MaxLength(160)] public string Email { get; set; } = "";

    [Required, MaxLength(4000)] public string Body { get; set; } = "";

    public bool IsRead { get; set; }

    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
}
