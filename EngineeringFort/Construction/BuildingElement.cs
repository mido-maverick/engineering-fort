namespace EngineeringFort.Construction;

/// <summary>
/// What every element of a building has (IfcBuildingElement): its type, which carries its sizes, and
/// where it was read from. An element belongs to the <see cref="Storey"/> that lists it
/// (IfcRelContainedInSpatialStructure) and is placed in plan relative to that storey's elevation.
/// </summary>
public abstract record class BuildingElement
{
    /// <summary>
    /// The <see cref="ElementType.Label"/> of its type (IfcRelDefinesByType), e.g. C1, G3. The mark on
    /// the drawings is the type's, shared by many elements, so an element has no label of its own.
    /// </summary>
    public virtual string Type { get; set; } = string.Empty;

    /// <summary>Where it was read from, so it can be reviewed and read again.</summary>
    public virtual DocumentReference? Source { get; set; }
}
