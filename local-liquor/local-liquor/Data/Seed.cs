using local_liquor.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace local_liquor.Data;

/// <summary>
/// First-run content. This is the range as it stood when the site moved from
/// compiled constants to the database, so an empty database comes up looking
/// exactly like the site did before. It only ever runs when Wines is empty.
/// </summary>
public static class Seed
{
    public static async Task EnsureSeededAsync(LocalLiquorContext db, CancellationToken ct = default)
    {
        if (await db.Wines.AnyAsync(ct)) return;

        db.Wines.AddRange(
            new Wine
            {
                Slug = "jordbaer",
                LabelName = "JORDBÆR",
                NameDa = "Jordbær",
                NameEn = "Strawberry",
                TaglineDa = "Højsommer, holdt fast.",
                TaglineEn = "High summer, held still.",
                BodyDa = "Lavet på jordbær, der er for modne til at sælge og præcis modne nok til at gære. Dyb rosa, rund i midten, med en syrlighed til sidst der forhindrer den i at blive sød.",
                BodyEn = "Made from strawberries too ripe to sell and exactly ripe enough to ferment. Deep pink, round in the middle, with a tartness at the end that stops it turning sweet.",
                ServingDa = "Køleskabskold, 10–12 °C. Til desserter med fløde, eller til en fredag.",
                ServingEn = "Fridge cold, 10–12 °C. With cream desserts, or with a Friday.",
                LiquidColor = "#D07C33",
                TintColor = "#F8E7D4",
                AlcoholByVolume = 8m,
                VolumeMl = 750,
                HarvestMonth = 7,
                BatchSize = 240,
                IsHero = true,
                SortOrder = 0,
                Notes =
                [
                    new WineNote { TextDa = "Moden jordbær", TextEn = "Ripe strawberry", SortOrder = 0 },
                    new WineNote { TextDa = "Rabarber", TextEn = "Rhubarb", SortOrder = 1 },
                    new WineNote { TextDa = "Let krydderi", TextEn = "Light spice", SortOrder = 2 },
                ],
            },
            new Wine
            {
                Slug = "solbaer",
                LabelName = "SOLBÆR",
                NameDa = "Solbær",
                NameEn = "Blackcurrant",
                TaglineDa = "Den mørke i familien.",
                TaglineEn = "The dark one in the family.",
                BodyDa = "Solbær giver farve nok til at male med og tannin nok til at holde i årevis. Den her er kraftig, næsten sortrød, og bliver kun bedre af at få lov at stå.",
                BodyEn = "Blackcurrants give enough colour to paint with and enough tannin to last for years. This one is powerful, almost black-red, and only improves if you leave it alone.",
                ServingDa = "Let afkølet, 14–16 °C. Til vildt, mørk chokolade, eller en lang aften.",
                ServingEn = "Lightly cooled, 14–16 °C. With game, dark chocolate, or a long evening.",
                LiquidColor = "#71184A",
                TintColor = "#EEDCE8",
                AlcoholByVolume = 8m,
                VolumeMl = 750,
                HarvestMonth = 8,
                BatchSize = 150,
                IsHero = false,
                SortOrder = 1,
                Notes =
                [
                    new WineNote { TextDa = "Solbær", TextEn = "Blackcurrant", SortOrder = 0 },
                    new WineNote { TextDa = "Lakrids", TextEn = "Liquorice", SortOrder = 1 },
                    new WineNote { TextDa = "Skovbund", TextEn = "Forest floor", SortOrder = 2 },
                ],
            },
            new Wine
            {
                Slug = "blaabaer",
                LabelName = "BLÅBÆR",
                NameDa = "Blåbær",
                NameEn = "Blueberry",
                TaglineDa = "Sensommerens sidste plukning.",
                TaglineEn = "The last picking of late summer.",
                BodyDa = "Blåbær giver en vin, der er næsten sort i glasset og alligevel let at drikke. Rund, en anelse sødmefuld, med en tør bund der holder den fra at blive sirupsagtig.",
                BodyEn = "Blueberries make a wine that is almost black in the glass and still easy to drink. Round, faintly sweet, with a dry base that stops it turning syrupy.",
                ServingDa = "Let afkølet, 12–14 °C. Til ost, mørke bær, eller en sen aften.",
                ServingEn = "Lightly cooled, 12–14 °C. With cheese, dark berries, or a late evening.",
                LiquidColor = "#48317D",
                TintColor = "#E2DDEF",
                AlcoholByVolume = 8m,
                VolumeMl = 750,
                HarvestMonth = 9,
                BatchSize = 180,
                IsHero = false,
                SortOrder = 2,
                Notes =
                [
                    new WineNote { TextDa = "Blåbær", TextEn = "Blueberry", SortOrder = 0 },
                    new WineNote { TextDa = "Violet", TextEn = "Violet", SortOrder = 1 },
                    new WineNote { TextDa = "Tør bund", TextEn = "Dry base", SortOrder = 2 },
                ],
            }
        );

        await db.SaveChangesAsync(ct);
    }
}
