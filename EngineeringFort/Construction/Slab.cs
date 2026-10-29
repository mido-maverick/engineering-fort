namespace EngineeringFort.Construction;

/// <summary>A flat slab (IfcSlab) over a closed boundary; its thickness is its <see cref="SlabType"/>'s.</summary>
public record class Slab : BuildingElement
{
    /// <summary>Its corners in order, not repeating the first (IfcArbitraryClosedProfileDef).</summary>
    public virtual IReadOnlyList<Placement> Boundary { get; set; } = [];

    /// <summary>Its top from the floor's elevation; negative when dropped.</summary>
    public virtual Length TopOffset { get; set; }

    public virtual IReadOnlyList<SlabOpening> Openings { get; set; } = [];
}
