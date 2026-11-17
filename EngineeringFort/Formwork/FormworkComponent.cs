namespace EngineeringFort.Formwork;

/// <summary>
/// What one layer of formwork is made of: a type in IFC's sense (IfcTypeProduct), carrying what
/// every piece of that make shares, not a piece placed anywhere. A calculation that needs only the
/// make and a spacing uses it as it is; a model that places pieces names it from each one, as an
/// element of <see cref="Construction"/> names its <see cref="Construction.ElementType"/>
/// (IfcRelDefinesByType).
/// </summary>
/// <remarks>
/// It is not an <see cref="Construction.ElementType"/>: that is a mark read off a drawing, with
/// the drawing as its source, and this comes from a catalogue. The two share only that each is
/// known by one name, which is <see cref="Name"/> here and which <see cref="Label"/> reads and
/// writes.
/// </remarks>
public abstract record class FormworkComponent : ILabelable
{
    public virtual string Name { get; set; } = "";

    /// <summary><see cref="Name"/>, for what treats everything labelled alike.</summary>
    public virtual string Label { get => Name; set => Name = value; }
}
