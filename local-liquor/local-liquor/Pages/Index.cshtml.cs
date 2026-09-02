using System.ComponentModel.DataAnnotations;
using local_liquor.Data;
using local_liquor.Data.Entities;
using local_liquor.Models;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace local_liquor.Pages;

// Only the POST is limited; the policy lets every GET through, so the front page
// itself is never rate limited.
[EnableRateLimiting(Program.ContactRateLimit)]
public class IndexModel : PageModel
{
    private readonly WineService _wines;
    private readonly MediaService _media;
    private readonly LocalLiquorContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IndexModel(WineService wines, MediaService media, LocalLiquorContext db,
        IStringLocalizer<SharedResource> localizer)
    {
        _wines = wines;
        _media = media;
        _db = db;
        _localizer = localizer;
    }

    public IReadOnlyList<WineView> Wines { get; private set; } = [];

    public IReadOnlyList<MediaAsset> Photos { get; private set; } = [];

    /// <summary>Index of the wine that starts centre stage, so markup and 3D agree.</summary>
    public int HeroIndex { get; private set; }

    [BindProperty]
    public ContactForm Form { get; set; } = new();

    /// <summary>Set through TempData so a refresh does not resend the message.</summary>
    [TempData]
    public bool Sent { get; set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    public async Task<IActionResult> OnPostContactAsync(CancellationToken ct)
    {
        // A bot fills in every field it finds. A person cannot see this one.
        if (!string.IsNullOrWhiteSpace(Form.Website))
        {
            Sent = true;
            return RedirectToPage(null, null, "kontakt");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(ct);
            return Page();
        }

        _db.ContactMessages.Add(new ContactMessage
        {
            Name = Form.Name.Trim(),
            Email = Form.Email.Trim(),
            Body = Form.Message.Trim(),
        });
        await _db.SaveChangesAsync(ct);

        Sent = true;
        return RedirectToPage(null, null, "kontakt");
    }

    private async Task LoadAsync(CancellationToken ct)
    {
        Wines = await _wines.GetPublishedAsync(ct);
        Photos = await _media.GetForAsync(MediaUsage.Frontpage, ct);
        HeroIndex = WineService.HeroIndex(Wines);

        ViewData["Title"] = _localizer["meta.home.title"].Value;
        ViewData["Description"] = _localizer["meta.home.description"].Value;
    }

    public class ContactForm
    {
        [Required(ErrorMessage = "contact.error.name"), MaxLength(80)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "contact.error.email"), EmailAddress(ErrorMessage = "contact.error.email.format"), MaxLength(160)]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "contact.error.message"), MaxLength(4000)]
        public string Message { get; set; } = "";

        /// <summary>The honeypot. Always empty when a person sends the form.</summary>
        [MaxLength(200)]
        public string Website { get; set; } = "";
    }
}
