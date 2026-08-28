using System.ComponentModel.DataAnnotations;

namespace local_liquor.Data.Entities;

/// <summary>A tasting note, shown as a chip on the product page.</summary>
public class WineNote
{
    public int Id { get; set; }

    public int WineId { get; set; }
    public Wine? Wine { get; set; }

    [Required, MaxLength(60)] public string TextDa { get; set; } = "";
    [Required, MaxLength(60)] public string TextEn { get; set; } = "";

    public int SortOrder { get; set; }
}
