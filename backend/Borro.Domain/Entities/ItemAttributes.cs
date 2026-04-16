namespace Borro.Domain.Entities;

/// <summary>
/// Flat owned type stored as JSONB via EF Core OwnsOne(...).ToJson().
/// All properties are nullable so that only the relevant fields for a given
/// Category are populated; absent fields serialize as JSON null.
///
/// Category → fields:
///   Vehicle      : Mileage, Transmission
///   Electronics  : Megapixels, Brand, Condition
///   RealEstate   : Bedrooms
///   Tools        : Brand, Condition
///   Sports       : Brand, Condition
///   Other        : Brand, Condition
/// </summary>
public sealed class ItemAttributes
{
    // Vehicle
    public int? Mileage { get; set; }
    public string? Transmission { get; set; }

    // RealEstate
    public int? Bedrooms { get; set; }

    // Electronics / Tools / Sports / Other
    public int? Megapixels { get; set; }
    public string? Brand { get; set; }
    public string? Condition { get; set; }
}
