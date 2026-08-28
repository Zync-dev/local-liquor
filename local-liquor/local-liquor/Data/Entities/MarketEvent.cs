using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>A market or event where the wine can be bought in person.</summary>
public class MarketEvent
{
    public int Id { get; set; }

    [Required, MaxLength(120)] public string TitleDa { get; set; } = "";
    [Required, MaxLength(120)] public string TitleEn { get; set; } = "";

    [Required, MaxLength(160)] public string Place { get; set; } = "";

    [MaxLength(200)] public string? Address { get; set; }

    public DateOnly StartsOn { get; set; }

    /// <summary>Null for a single-day market.</summary>
    public DateOnly? EndsOn { get; set; }

    /// <summary>Free text, e.g. "10–16". Times vary too much to model properly.</summary>
    [MaxLength(60)] public string? Hours { get; set; }

    [MaxLength(400)] public string? Url { get; set; }

    public bool IsPublished { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
