using EngineeringFort.Construction;

namespace EngineeringFort.StructuralAnalysis;

/// <summary>
/// A straight member along its centreline between two connections (IfcStructuralCurveMember, joined to
/// them by IfcRelConnectsStructuralMember). A member crossing a connection is split there.
/// </summary>
public record class StructuralCurveMember
{
    public virtual string Label { get; init; } = string.Empty;

    /// <summary>The <see cref="StructuralPointConnection.Label"/> of the connection it starts at.</summary>
    public virtual string Start { get; init; } = string.Empty;

    /// <summary>The <see cref="StructuralPointConnection.Label"/> of the connection it ends at.</summary>
    public virtual string End { get; init; } = string.Empty;

    /// <summary>The name of its section (IfcProfileDef.ProfileName), as marked, e.g. H100X100; omitted when unknown.</summary>
    public virtual string? Profile { get; init; }

    /// <summary>Where it was read from, so it can be reviewed and read again.</summary>
    public virtual DocumentReference? Source { get; init; }
}
