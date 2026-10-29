namespace EngineeringFort.Construction;

/// <summary>A straight wall (IfcWall) along its centreline, from its floor up; its thickness is its <see cref="WallType"/>'s.</summary>
public record class Wall : BuildingElement
{
    public virtual Placement Start { get; set; }

    public virtual Placement End { get; set; }

    /// <summary>Omitted reaches the next floor.</summary>
    public virtual Length? Height { get; set; }

    /// <summary>Faces the outside (Pset_WallCommon.IsExternal): where scaffolds go.</summary>
    public virtual bool IsExternal { get; set; }

    /// <summary>Structural rather than a partition (Pset_WallCommon.LoadBearing).</summary>
    public virtual bool LoadBearing { get; set; }

    public virtual IReadOnlyList<WallOpening> Openings { get; set; } = [];
}
