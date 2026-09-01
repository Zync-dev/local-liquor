using local_liquor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Data;

/// <summary>
/// The range as the 2026 brand manual defines it, taken from the label artwork
/// rather than retyped: two wines, one accent hue each, copy lifted from the
/// back labels.
///
/// This runs on an empty database and also brings an existing one across from
/// the old three-wine range. Both paths are keyed on Skovbær being absent, so
/// re-running it does nothing.
/// </summary>
public static class Seed
{
    /// <summary>Slugs retired in the redesign. Deleted, at the owner's instruction.</summary>
    private static readonly string[] Discontinued = ["solbaer", "blaabaer"];

    public static async Task EnsureSeededAsync(LocalLiquorContext db, CancellationToken ct = default)
    {
        if (await db.Wines.AnyAsync(w => w.Slug == "skovbaer", ct)) return;

        // The old range is gone, not hidden.
        var retired = await db.Wines.Where(w => Discontinued.Contains(w.Slug)).ToListAsync(ct);
        if (retired.Count > 0) db.Wines.RemoveRange(retired);

        var jordbaer = await db.Wines.Include(w => w.Notes)
            .FirstOrDefaultAsync(w => w.Slug == "jordbaer", ct);

        if (jordbaer is null)
        {
            jordbaer = new Wine { Slug = "jordbaer" };
            db.Wines.Add(jordbaer);
        }

        ApplyJordbaer(jordbaer);
        db.Wines.Add(BuildSkovbaer());

        await db.SaveChangesAsync(ct);
    }

    private static void ApplyJordbaer(Wine wine)
    {
        wine.LabelName = "JORDBÆR";
        wine.NameDa = "Jordbær";
        wine.NameEn = "Strawberry";
        wine.SubtitleEn = "STRAWBERRY WINE";
        wine.TaglineDa = "Dansk jordbær, presset og gæret i små hold.";
        wine.TaglineEn = "Danish strawberries, pressed and fermented in small batches.";
        wine.BodyDa = "Presset på danske jordbær fra sæsonens høst og gæret langsomt i "
                      + "små hold. Uklar og tør. Serveres kold, 8–10 °C.";
        wine.BodyEn = "Pressed from Danish strawberries, slowly fermented in small "
                      + "batches. Dry, unfiltered. Serve chilled.";
        wine.ServingDa = "Serveres kold, 8–10 °C.";
        wine.ServingEn = "Serve chilled, 8–10 °C.";
        wine.IngredientsDa = "Jordbær, vand, sukker, gær. Indeholder sulfitter.";
        wine.IngredientsEn = "Strawberries, water, sugar, yeast. Contains sulphites.";
        // Accent ladder, hue 22.
        wine.AccentColor = "#c0453c";
        wine.LiquidColor = "#d07c33";
        wine.AlcoholByVolume = 13m;
        wine.VolumeMl = 750;
        wine.Batch = "04";
        wine.HarvestMonth = 7;
        wine.BatchSize = 240;
        wine.IsPublished = true;
        wine.IsHero = true;
        wine.SortOrder = 0;
        wine.Notes.Clear();
        wine.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static Wine BuildSkovbaer() => new()
    {
        Slug = "skovbaer",
        LabelName = "SKOVBÆR",
        NameDa = "Skovbær",
        NameEn = "Forest fruit",
        SubtitleEn = "FOREST FRUIT WINE",
        TaglineDa = "Plukkede skovbær, lagret til smagen falder til ro.",
        TaglineEn = "Hand-picked forest fruit, aged until the flavour settles.",
        BodyDa = "Gæret på plukkede skovbær og lagret til smagen falder til ro. "
                 + "Mørk og bærret med lav sødme. Serveres kold, 8–10 °C.",
        BodyEn = "Fermented on hand-picked forest fruit, aged until the flavour "
                 + "settles. Dark, berried, barely sweet.",
        ServingDa = "Serveres kold, 8–10 °C.",
        ServingEn = "Serve chilled, 8–10 °C.",
        IngredientsDa = "Skovbær, vand, sukker, gær. Indeholder sulfitter.",
        IngredientsEn = "Forest fruit, water, sugar, yeast. Contains sulphites.",
        // Accent ladder, hue 320 — 62 degrees clear of Jordbær, as the rules require.
        AccentColor = "#a34e93",
        LiquidColor = "#6d2f5c",
        AlcoholByVolume = 13m,
        VolumeMl = 750,
        Batch = "02",
        HarvestMonth = 9,
        BatchSize = 180,
        IsPublished = true,
        IsHero = false,
        SortOrder = 1,
    };
}
