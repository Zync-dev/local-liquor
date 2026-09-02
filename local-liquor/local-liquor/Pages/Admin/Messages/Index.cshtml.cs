using local_liquor.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Pages.Admin.Messages;

/// <summary>
/// The contact form's inbox. Messages are stored rather than mailed on, so this
/// is where they are actually read.
/// </summary>
public class MessagesModel : PageModel
{
    private readonly LocalLiquorContext _db;

    public MessagesModel(LocalLiquorContext db) => _db = db;

    public List<Data.Entities.ContactMessage> Messages { get; private set; } = [];

    public int Unread => Messages.Count(m => !m.IsRead);

    public async Task OnGetAsync(CancellationToken ct)
    {
        Messages = await _db.ContactMessages
            .AsNoTracking()
            .OrderByDescending(m => m.ReceivedAt)
            .Take(200)
            .ToListAsync(ct);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken ct)
    {
        var message = await _db.ContactMessages.FindAsync([id], ct);
        if (message is not null)
        {
            message.IsRead = !message.IsRead;
            await _db.SaveChangesAsync(ct);
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var message = await _db.ContactMessages.FindAsync([id], ct);
        if (message is not null)
        {
            _db.ContactMessages.Remove(message);
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = "Beskeden er slettet.";
        }

        return RedirectToPage();
    }
}
