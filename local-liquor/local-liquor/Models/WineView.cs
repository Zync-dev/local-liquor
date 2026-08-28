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

    public required string LiquidColor { get; init; }
    public required string TintColor { get; init; }
    public required decimal AlcoholByVolume { get; init; }
    public required int VolumeMl { get; init; }
    public required int HarvestMonth { get; init; }
    public required int BatchSize { get; init; }
    public required StockStatus Stock { get; init; }
    public required int? BottlesLeft { get; init; }
    public required bool IsHero { get; init; }

    public bool IsSoldOut => Stock == StockStatus.SoldOut;

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
        LiquidColor = wine.LiquidColor,
        TintColor = wine.TintColor,
        AlcoholByVolume = wine.AlcoholByVolume,
        VolumeMl = wine.VolumeMl,
        HarvestMonth = wine.HarvestMonth,
        BatchSize = wine.BatchSize,
        Stock = wine.Stock,
        BottlesLeft = wine.BottlesLeft,
        IsHero = wine.IsHero,
    };
}
