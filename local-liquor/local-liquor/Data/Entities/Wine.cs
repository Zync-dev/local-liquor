using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

public enum StockStatus
{
    Available = 0,
    Low = 1,
    SoldOut = 2,
}

/// <summary>
/// One wine in the range. Copy is stored per language rather than in the resource
/// files, because wines are created and edited at runtime and .resx is compiled in.
/// </summary>
public class Wine
{
    public int Id { get; set; }

    /// <summary>URL segment, e.g. "jordbaer" in /vin/jordbaer.</summary>
    [Required, MaxLength(60)]
    public string Slug { get; set; } = "";

    /// <summary>
    /// The name as printed on the physical label. Always Danish and always upper
    /// case: the bottle is the bottle, whichever language the site is read in.
    /// </summary>
    [Required, MaxLength(40)]
    public string LabelName { get; set; } = "";

    [Required, MaxLength(60)] public string NameDa { get; set; } = "";
    [Required, MaxLength(60)] public string NameEn { get; set; } = "";

    [MaxLength(120)] public string TaglineDa { get; set; } = "";
    [MaxLength(120)] public string TaglineEn { get; set; } = "";

    [MaxLength(1200)] public string BodyDa { get; set; } = "";
    [MaxLength(1200)] public string BodyEn { get; set; } = "";

    [MaxLength(400)] public string ServingDa { get; set; } = "";
    [MaxLength(400)] public string ServingEn { get; set; } = "";

    /// <summary>
    /// The one variable in the brand system. Everything else about a label is
    /// fixed; a new fruit takes a free hue off the accent ladder — oklch(0.58
    /// 0.14 H), at least 60 degrees from every other fruit in the range — and
    /// changes nothing else.
    /// </summary>
    [Required, MaxLength(7)] public string AccentColor { get; set; } = "#c0453c";

    /// <summary>
    /// What is actually in the bottle, for the 3D render. Deliberately separate
    /// from the accent: the accent is a graphic decision and this is a physical
    /// fact — strawberry wine is amber where its accent is red.
    /// </summary>
    [Required, MaxLength(7)] public string LiquidColor { get; set; } = "#d07c33";

    /// <summary>English subtitle under the fruit name, set in mono caps.</summary>
    [MaxLength(60)] public string SubtitleEn { get; set; } = "";

    /// <summary>Batch, as printed on the back label. A label, not a number.</summary>
    [MaxLength(12)] public string Batch { get; set; } = "";

    [MaxLength(200)] public string IngredientsDa { get; set; } = "";
    [MaxLength(200)] public string IngredientsEn { get; set; } = "";

    [Range(0, 60)] public decimal AlcoholByVolume { get; set; } = 13m;

    [Range(1, 5000)] public int VolumeMl { get; set; } = 750;

    /// <summary>Month the fruit is picked, 1-12.</summary>
    [Range(1, 12)] public int HarvestMonth { get; set; } = 7;

    /// <summary>Bottles produced in the most recent batch.</summary>
    [Range(0, 100000)] public int BatchSize { get; set; }

    public StockStatus Stock { get; set; } = StockStatus.Available;

    /// <summary>Optional bottle count. Null means "we would rather not say".</summary>
    [Range(0, 100000)] public int? BottlesLeft { get; set; }

    /// <summary>Unpublished wines are hidden from the site but kept in the admin.</summary>
    public bool IsPublished { get; set; } = true;

    /// <summary>The one that starts centre stage in the hero. Exactly one wine holds this.</summary>
    public bool IsHero { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<WineNote> Notes { get; set; } = [];
}
