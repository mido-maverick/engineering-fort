namespace EngineeringFort.Construction;

/// <summary>
/// A point in plan. Naming two axes, it is where they cross, shifted by <see cref="DX"/> and
/// <see cref="DY"/> (IfcGridPlacement at an IfcVirtualGridIntersection), the way the drawings set
/// elements out; naming none, it is that far from the building's origin (IfcLocalPlacement).
/// <see cref="Building.Locate"/> works out where it is.
/// </summary>
public readonly record struct Placement
{
    /// <summary>An axis from a grid's <see cref="Grid.UAxes"/>.</summary>
    public GridAxis? U { get; init; }

    /// <summary>An axis from the same grid's <see cref="Grid.VAxes"/>.</summary>
    public GridAxis? V { get; init; }

    /// <summary>Along the building's x, not the axis.</summary>
    public Length DX { get; init; }

    /// <summary>Along the building's y, not the axis.</summary>
    public Length DY { get; init; }
}
