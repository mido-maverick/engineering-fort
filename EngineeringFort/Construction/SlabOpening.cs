namespace EngineeringFort.Construction;

/// <summary>
/// An opening through the <see cref="Slab"/> that lists it, e.g. a stair or lift void. Its outline is
/// its own, so it needs no type; one given must be an <see cref="OpeningType"/>, e.g. for a hatch.
/// </summary>
public record class SlabOpening : Opening
{
    /// <summary>Its corners in order, not repeating the first.</summary>
    public virtual IReadOnlyList<Placement> Boundary { get; set; } = [];
}
