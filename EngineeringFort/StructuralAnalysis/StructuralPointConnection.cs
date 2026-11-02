using EngineeringFort.Construction;

namespace EngineeringFort.StructuralAnalysis;

/// <summary>A point where structural members meet (IfcStructuralPointConnection).</summary>
public record class StructuralPointConnection
{
    public virtual string Label { get; init; } = string.Empty;

    /// <summary>Along the model's x; omitted when unknown.</summary>
    public virtual Length? X { get; init; }

    /// <summary>Along the model's y; omitted when unknown.</summary>
    public virtual Length? Y { get; init; }

    /// <summary>Upward; omitted when unknown.</summary>
    public virtual Length? Z { get; init; }

    /// <summary>
    /// Whether the structure stands on it (an IfcBoundaryNodeCondition applied to it). Which ways it is
    /// restrained is left to the analysis program.
    /// </summary>
    public virtual bool IsSupport { get; init; }

    /// <summary>Where it was read from, so it can be reviewed and read again.</summary>
    public virtual DocumentReference? Source { get; init; }
}
