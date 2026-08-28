using local_liquor.Data;
using local_liquor.Data.Entities;
using local_liquor.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace local_liquor.Pages.Admin.Media;

public class MediaIndexModel : PageModel
{
    private readonly LocalLiquorContext _db;
    private readonly MediaService _media;

    public MediaIndexModel(LocalLiquorContext db, MediaService media)
    {
        _db = db;
        _media = media;
    }

    public List<MediaAsset> Assets { get; private set; } = [];

    public string? Error { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => Assets = await _media.GetAllAsync(ct);

    public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, CancellationToken ct)
    {
        var saved = 0;
        var failures = new List<string>();

        foreach (var file in files)
        {
            var result = await _media.SaveAsync(file, ct);
            if (result.Ok)
            {
                saved++;
            }
            else
            {
                failures.Add($"{file.FileName}: {result.Error}");
            }
        }

        if (saved > 0)
        {
            TempData["Flash"] = saved == 1 ? "Billedet er lagt op." : $"{saved} billeder er lagt op.";
        }

        if (failures.Count > 0)
        {
            Error = string.Join(" ", failures);
            Assets = await _media.GetAllAsync(ct);
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostSaveAsync(int id, string? altDa, string? altEn,
        MediaUsage usage, CancellationToken ct)
    {
        var asset = await _db.MediaAssets.FindAsync([id], ct);
        if (asset is not null)
        {
            asset.AltDa = altDa?.Trim() ?? "";
            asset.AltEn = string.IsNullOrWhiteSpace(altEn) ? asset.AltDa : altEn.Trim();
            asset.Usage = usage;
            await _db.SaveChangesAsync(ct);
            TempData["Flash"] = "Billedet er gemt.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        await _media.DeleteAsync(id, ct);
        TempData["Flash"] = "Billedet er slettet.";
        return RedirectToPage();
    }
}
