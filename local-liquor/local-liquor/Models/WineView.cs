using local_liquor.Data.Entities;

namespace local_liquor.Models;

/// <summary>
/// A wine with its copy already resolved to one language, which is all any page
/// on the public site needs. Keeps culture handling in one place instead of
/// scattering "danish ? x : y" through the views.
/// </summary>
public sealed record WineView
{
    public required int Id { get; init; }
    public required string Slug { get; init; }
    public required string LabelName { get; init; }
    public required string Name { get; init; }
    public required string Tagline { get; init; }
    public required string Body { get; init; }
    public required string Serving { get; init; }
    public required IReadOnlyList<string> Notes { get; init; }

    /// <summary>The wine's accent hue — the only variable in the brand system.</summary>
    public required string AccentColor { get; init; }

    /// <summary>What is in the bottle, for the 3D render. Not the accent.</summary>
    public required string LiquidColor { get; init; }

    public required string SubtitleEn { get; init; }
    public required string Batch { get; init; }
    public required string Ingredients { get; init; }
    public required decimal AlcoholByVolume { get; init; }
    public required int VolumeMl { get; init; }
    public required int HarvestMonth { get; init; }
    public required int BatchSize { get; init; }
    public required StockStatus Stock { get; init; }
    public required int? BottlesLeft { get; init; }
    public required bool IsHero { get; init; }

    public bool IsSoldOut => Stock == StockStatus.SoldOut;

    /// <summary>
    /// The label is a printed object: it says the same thing in the same
    /// language whichever way the site is being read. These carry the Danish
    /// copy through regardless of the current culture.
    /// </summary>
    public required string BodyDaForLabel { get; init; }
    public required string BodyEnForLabel { get; init; }
    public required string IngredientsDaForLabel { get; init; }

    /// <summary>
    /// The label breaks a long fruit name across two lines with a hyphen —
    /// JORD-/BÆR. Splitting near the middle rather than at a fixed point keeps
    /// the two lines close in width, which is what the manual asks for.
    /// </summary>
    public (string Top, string Bottom) LabelLines => SplitLabelName(LabelName);

    /// <inheritdoc cref="LabelLines"/>
    /// <remarks>Static so the admin preview splits a name the same way.</remarks>
    public static (string Top, string Bottom) SplitLabelName(string name)
    {
        if (name.Length <= 5) return (name, "");
        var split = (int)Math.Ceiling(name.Length / 2.0);
        return (name[..split] + "-", name[split..]);
    }

    /// <summary>The harvest month, named in the reader's language.</summary>
    public string HarvestMonthName(System.Globalization.CultureInfo culture) =>
        culture.TextInfo.ToTitleCase(culture.DateTimeFormat.GetMonthName(HarvestMonth));

    public static WineView From(Wine wine, bool danish) => new()
    {
        Id = wine.Id,
        Slug = wine.Slug,
        LabelName = wine.LabelName,
        Name = danish ? wine.NameDa : wine.NameEn,
        Tagline = danish ? wine.TaglineDa : wine.TaglineEn,
        Body = danish ? wine.BodyDa : wine.BodyEn,
        Serving = danish ? wine.ServingDa : wine.ServingEn,
        Notes = wine.Notes.OrderBy(n => n.SortOrder)
                          .Select(n => danish ? n.TextDa : n.TextEn)
                          .Where(t => !string.IsNullOrWhiteSpace(t))
                          .ToList(),
        AccentColor = wine.AccentColor,
        LiquidColor = wine.LiquidColor,
        SubtitleEn = wine.SubtitleEn,
        Batch = wine.Batch,
        Ingredients = danish ? wine.IngredientsDa : wine.IngredientsEn,
        BodyDaForLabel = wine.BodyDa,
        BodyEnForLabel = wine.BodyEn,
        IngredientsDaForLabel = wine.IngredientsDa,
        AlcoholByVolume = wine.AlcoholByVolume,
        VolumeMl = wine.VolumeMl,
        HarvestMonth = wine.HarvestMonth,
        BatchSize = wine.BatchSize,
        Stock = wine.Stock,
        BottlesLeft = wine.BottlesLeft,
        IsHero = wine.IsHero,
    };
}
