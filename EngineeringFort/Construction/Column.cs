namespace EngineeringFort.Construction;

/// <summary>A column (IfcColumn), from its floor up to the next; its section is its <see cref="ColumnType"/>'s.</summary>
public record class Column : BuildingElement
{
    /// <summary>The centre of its section.</summary>
    public virtual Placement Position { get; set; }

    /// <summary>Its own x from the building's x, counter-clockwise.</summary>
    public virtual Angle Rotation { get; set; }
}
