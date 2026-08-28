using local_liquor.Data;
using local_liquor.Data.Entities;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace local_liquor.Services;

/// <summary>
/// Stores uploaded photos on disk and indexes them in the database.
///
/// Everything about an upload that the browser tells us is treated as a claim:
/// the file is decoded before it is trusted, re-encoded rather than passed
/// through, and written under a name we generate. That means a file renamed to
/// .jpg, or a valid image with a script payload appended, cannot survive.
/// </summary>
public sealed class MediaService
{
    /// <summary>Phone photos are big; anything past this is a mistake or an attack.</summary>
    public const long MaxUploadBytes = 12 * 1024 * 1024;

    /// <summary>Long edge, in pixels. Beyond this is wasted bytes on a web page.</summary>
    private const int MaxDimension = 2200;

    private readonly LocalLiquorContext _db;
    private readonly StoragePaths _paths;
    private readonly ILogger<MediaService> _log;

    public MediaService(LocalLiquorContext db, StoragePaths paths, ILogger<MediaService> log)
    {
        _db = db;
        _paths = paths;
        _log = log;
    }

    public Task<List<MediaAsset>> GetAllAsync(CancellationToken ct = default) =>
        _db.MediaAssets.AsNoTracking()
            .OrderBy(m => m.Usage).ThenBy(m => m.SortOrder).ThenByDescending(m => m.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MediaAsset>> GetForAsync(MediaUsage usage, CancellationToken ct = default) =>
        await _db.MediaAssets.AsNoTracking()
            .Where(m => m.Usage == usage)
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Id)
            .ToListAsync(ct);

    public sealed record UploadResult(bool Ok, string? Error, MediaAsset? Asset);

    public async Task<UploadResult> SaveAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file.Length == 0)
        {
            return new UploadResult(false, "The file is empty.", null);
        }

        if (file.Length > MaxUploadBytes)
        {
            return new UploadResult(false, $"That file is larger than {MaxUploadBytes / (1024 * 1024)} MB.", null);
        }

        try
        {
            await using var stream = file.OpenReadStream();

            // Decoding is the real content check: if ImageSharp cannot read it, it
            // is not an image whatever the name or the declared content type says.
            using var image = await Image.LoadAsync(stream, ct);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension),
                }));
            }

            // JPEG has no alpha, so a transparent PNG would encode its transparent
            // pixels as black. Composite onto white first.
            image.Mutate(x => x.BackgroundColor(Color.White));

            // Strip EXIF: phone photos carry GPS coordinates, and these end up public.
            image.Metadata.ExifProfile = null;
            image.Metadata.IptcProfile = null;
            image.Metadata.XmpProfile = null;

            var fileName = $"{Guid.NewGuid():n}.jpg";
            var fullPath = Path.Combine(_paths.Uploads, fileName);

            await image.SaveAsync(fullPath, new JpegEncoder { Quality = 82 }, ct);

            var asset = new MediaAsset
            {
                FileName = fileName,
                OriginalName = Path.GetFileName(file.FileName),
                ContentType = "image/jpeg",
                Width = image.Width,
                Height = image.Height,
                ByteSize = new FileInfo(fullPath).Length,
            };

            _db.MediaAssets.Add(asset);
            await _db.SaveChangesAsync(ct);
            return new UploadResult(true, null, asset);
        }
        catch (UnknownImageFormatException)
        {
            return new UploadResult(false, "That does not look like an image we can read.", null);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Upload failed for {Name}", file.FileName);
            return new UploadResult(false, "Something went wrong saving that file.", null);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var asset = await _db.MediaAssets.FindAsync([id], ct);
        if (asset is null) return;

        // Guard against a stored name ever escaping the uploads directory.
        var fullPath = Path.Combine(_paths.Uploads, Path.GetFileName(asset.FileName));
        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                _log.LogWarning(ex, "Could not delete {Path}; removing the row anyway", fullPath);
            }
        }

        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(ct);
    }
}
