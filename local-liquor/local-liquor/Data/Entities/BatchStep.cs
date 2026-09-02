using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>
/// One thing that has to happen to a batch on a particular day — take the fruit
/// off, rack it, filter it, bottle it.
///
/// The title is free text rather than an enum on purpose. Every batch ends up
/// with something the last one did not need, and a fixed list would either be
/// too long to read or too short to use. <see cref="StepTemplate"/> supplies the
/// usual ones so nobody types them out every time.
/// </summary>
public class BatchStep
{
    public int Id { get; set; }

    public int BatchId { get; set; }
    public Batch? Batch { get; set; }

    [Required, MaxLength(80)] public string Title { get; set; } = "";

    /// <summary>The day it should happen. This is what the dashboard sorts on.</summary>
    public DateOnly DueOn { get; set; }

    /// <summary>Null until it is done. The date, not a flag, so the log is a log.</summary>
    public DateOnly? DoneOn { get; set; }

    [MaxLength(400)] public string Notes { get; set; } = "";

    public int SortOrder { get; set; }

    public bool IsDone => DoneOn is not null;

    /// <summary>Days until it is due; negative once it is late.</summary>
    public int DaysUntil(DateOnly today) => DueOn.DayNumber - today.DayNumber;
}

/// <summary>
/// The steps a fruit wine normally goes through, as offsets in days from the day
/// the fruit went in. Applied when a batch is created, then edited freely — they
/// are a starting point, not a schedule anyone is held to.
/// </summary>
public readonly record struct StepTemplate(string Title, int DayOffset)
{
    public static readonly StepTemplate[] Default =
    [
        new("Rør om / pres frugten ned", 2),
        new("Fjern frugten", 7),
        new("Omstik fra bærmen", 21),
        new("Mål slutvægtfylde", 45),
        new("Omstik og klaring", 60),
        new("Filtrering", 90),
        new("Aftapning", 120),
    ];
}
