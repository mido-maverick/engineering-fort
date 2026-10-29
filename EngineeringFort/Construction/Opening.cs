namespace EngineeringFort.Construction;

/// <summary>
/// A void through the element that lists it (IfcOpeningElement, related by IfcRelVoidsElement). Each
/// host sets out its openings its own way: <see cref="WallOpening"/>, <see cref="SlabOpening"/>.
/// </summary>
public abstract record class Opening : BuildingElement;
