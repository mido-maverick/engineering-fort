namespace EngineeringFort.Construction;

/// <summary>
/// A structural grid (IfcGrid): two families of axes, usually numbers (U) and letters (V).
/// Which way each family runs differs between drawings.
/// </summary>
public record class Grid : ILabelable
{
    public virtual string Label { get; set; } = string.Empty;

    public virtual IReadOnlyList<GridAxisCurve> UAxes { get; set; } = [];

    public virtual IReadOnlyList<GridAxisCurve> VAxes { get; set; } = [];
}

/// <summary>A grid axis as drawn (IfcGridAxis): its tag and the line it runs along.</summary>
public readonly record struct GridAxisCurve(GridAxis Axis, CartesianPoint Start, CartesianPoint End)
{
    public DocumentReference? Source { get; init; }
}
