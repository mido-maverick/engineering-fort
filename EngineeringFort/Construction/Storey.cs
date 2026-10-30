namespace EngineeringFort.Construction;

/// <summary>A storey (IfcBuildingStorey) of a <see cref="Building"/> and the elements on it.</summary>
public record class Storey : IRegexPatternProvider<Storey>, ILabelable
{
    public static string RegexPattern { get; } = @"[BR]?\d{1,3}FL?|[RP]?RFL?";

    public virtual string Label { get; set; } = string.Empty;

    /// <summary>Omitted when unknown.</summary>
    public virtual Length? Elevation { get; set; }

    /// <summary>Where its elevation came from.</summary>
    public virtual DocumentReference? Source { get; set; }

    /// <summary>
    /// The label of the storey whose elements this one repeats, as one typical plan stands for several
    /// storeys; its own lists stay empty. See <see cref="Building.Resolve"/>.
    /// </summary>
    public virtual string? SameAs { get; set; }

    public virtual IReadOnlyList<Column> Columns { get; set; } = [];

    public virtual IReadOnlyList<Wall> Walls { get; set; } = [];

    public virtual IReadOnlyList<Beam> Beams { get; set; } = [];

    public virtual IReadOnlyList<Slab> Slabs { get; set; } = [];
}
