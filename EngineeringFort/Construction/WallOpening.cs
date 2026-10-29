namespace EngineeringFort.Construction;

/// <summary>
/// An opening through the <see cref="Wall"/> that lists it; its width, height and filling are its
/// <see cref="OpeningType"/>'s.
/// </summary>
public record class WallOpening : Opening
{
    /// <summary>From the wall's start to the opening's nearer edge, along the wall.</summary>
    public virtual Length Offset { get; set; }

    /// <summary>From the floor's elevation to the opening's bottom; 0 for a door, omitted when unknown.</summary>
    public virtual Length? SillHeight { get; set; }
}
