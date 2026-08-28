using System.Globalization;
using local_liquor.Data.Entities;

namespace local_liquor.Models;

public sealed record MarketView
{
    public required string Title { get; init; }
    public required string Place { get; init; }
    public required string? Address { get; init; }
    public required DateOnly StartsOn { get; init; }
    public required DateOnly? EndsOn { get; init; }
    public required string? Hours { get; init; }
    public required string? Url { get; init; }

    /// <summary>"14. juni" or "14.–15. juni", in the reader's language.</summary>
    public string DateRange(CultureInfo culture)
    {
        var start = StartsOn.ToDateTime(TimeOnly.MinValue);
        if (EndsOn is null || EndsOn == StartsOn)
        {
            return start.ToString("d. MMMM", culture);
        }

        var end = EndsOn.Value.ToDateTime(TimeOnly.MinValue);
        return start.Month == end.Month
            ? $"{start.ToString("d.", culture)}–{end.ToString("d. MMMM", culture)}"
            : $"{start.ToString("d. MMM", culture)} – {end.ToString("d. MMM", culture)}";
    }

    public static MarketView From(MarketEvent market, bool danish) => new()
    {
        Title = danish ? market.TitleDa : market.TitleEn,
        Place = market.Place,
        Address = market.Address,
        StartsOn = market.StartsOn,
        EndsOn = market.EndsOn,
        Hours = market.Hours,
        Url = market.Url,
    };
}
