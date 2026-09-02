using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>
/// Where a batch is in its life. The order matters: the admin sorts by it and
/// treats everything before <see cref="Bottled"/> as work in progress.
/// </summary>
public enum BatchStage
{
    /// <summary>Fruit picked or bought, nothing started yet.</summary>
    Planned = 0,

    /// <summary>On the fruit, fermenting hard.</summary>
    Primary = 1,

    /// <summary>Fruit out, off the gross lees, fermenting slowly.</summary>
    Secondary = 2,

    /// <summary>Fermentation done, sitting and clearing.</summary>
    Ageing = 3,

    /// <summary>In bottles.</summary>
    Bottled = 4,

    /// <summary>Drunk, sold or written off. Kept for the record.</summary>
    Archived = 5,
}

/// <summary>
/// One vessel of wine, from fruit to bottle.
///
/// A batch is deliberately not the same thing as a <see cref="Wine"/>: a wine is
/// what the site sells, a batch is what is actually standing in the room. Several
/// batches of jordbær can exist at once, and a batch can be an experiment that
/// never becomes a listed wine at all — which is why <see cref="WineId"/> is
/// optional.
/// </summary>
public class Batch
{
    public int Id { get; set; }

    /// <summary>The batch number as written on the vessel and the back label.</summary>
    [Required, MaxLength(12)] public string Code { get; set; } = "";

    /// <summary>What it is, when it is not one of the listed wines yet.</summary>
    [Required, MaxLength(80)] public string Title { get; set; } = "";

    /// <summary>The wine this will be bottled as, if it is one of them.</summary>
    public int? WineId { get; set; }
    public Wine? Wine { get; set; }

    public BatchStage Stage { get; set; } = BatchStage.Planned;

    /// <summary>The day the fruit went in. Everything else is counted from here.</summary>
    public DateOnly StartedOn { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    public DateOnly? BottledOn { get; set; }

    [Range(0, 10000)] public decimal Litres { get; set; }

    [Range(0, 10000)] public decimal FruitKg { get; set; }

    [Range(0, 10000)] public decimal SugarKg { get; set; }

    /// <summary>
    /// Specific gravity at the start and at the end. Stored as typed rather than
    /// computed because the hydrometer is the thing that was actually read.
    /// </summary>
    [Range(0.9, 1.3)] public decimal? StartGravity { get; set; }
    [Range(0.9, 1.3)] public decimal? EndGravity { get; set; }

    /// <summary>Bottles that came out of it.</summary>
    [Range(0, 100000)] public int BottleCount { get; set; }

    [MaxLength(4000)] public string Notes { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<BatchStep> Steps { get; set; } = [];

    /// <summary>
    /// The rule of thumb every home winemaker uses: the drop in gravity times
    /// 131.25. Null until both readings are in.
    /// </summary>
    public decimal? EstimatedAbv =>
        StartGravity is { } start && EndGravity is { } end && start > end
            ? Math.Round((start - end) * 131.25m, 1)
            : null;

    public bool IsFinished => Stage >= BatchStage.Bottled;
}
