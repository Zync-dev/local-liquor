using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

public enum MediaUsage
{
    Unused = 0,
    Story = 1,
    Craft = 2,
}

/// <summary>
/// An uploaded photo. The bytes live on disk (on Railway, the mounted volume);
/// this row is the index over them.
/// </summary>
public class MediaAsset
{
    public int Id { get; set; }

    /// <summary>Generated name on disk. Never the name the browser supplied.</summary>
    [Required, MaxLength(80)] public string FileName { get; set; } = "";

    [MaxLength(200)] public string? OriginalName { get; set; }

    [Required, MaxLength(60)] public string ContentType { get; set; } = "";

    public int Width { get; set; }
    public int Height { get; set; }
    public long ByteSize { get; set; }

    [MaxLength(200)] public string AltDa { get; set; } = "";
    [MaxLength(200)] public string AltEn { get; set; } = "";

    public MediaUsage Usage { get; set; } = MediaUsage.Unused;

    public int SortOrder { get; set; }

    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
}
