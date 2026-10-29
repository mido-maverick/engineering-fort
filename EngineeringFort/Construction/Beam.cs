namespace EngineeringFort.Construction;

/// <summary>A straight beam (IfcBeam) along its centreline; its section is its <see cref="BeamType"/>'s.</summary>
public record class Beam : BuildingElement
{
    public virtual Placement Start { get; set; }

    public virtual Placement End { get; set; }

    /// <summary>Its top from the floor's elevation; negative when dropped.</summary>
    public virtual Length TopOffset { get; set; }
}
