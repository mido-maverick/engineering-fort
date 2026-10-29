namespace EngineeringFort.Construction;

/// <summary>
/// The document something was read from (IfcDocumentReference, related by IfcRelAssociatesDocument):
/// which drawing, which entities in it, and what was judged rather than read.
/// </summary>
public record class DocumentReference
{
    /// <summary>The document, e.g. a drawing's file name.</summary>
    public virtual string Location { get; set; } = string.Empty;

    /// <summary>What it was read from within the document, e.g. CAD entity handles.</summary>
    public virtual IReadOnlyList<string> Identifications { get; set; } = [];

    /// <summary>What was judged or taken from elsewhere, e.g. "depth from the beam schedule".</summary>
    public virtual string Description { get; set; } = string.Empty;
}
