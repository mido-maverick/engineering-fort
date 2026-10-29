namespace EngineeringFort.Construction;

/// <summary>
/// What the elements of one mark share (IfcTypeProduct, related by IfcRelDefinesByType): the sizes a
/// schedule lists once per mark. A size left out is unknown — not on any drawing read so far — rather
/// than zero.
/// </summary>
public abstract record class ElementType : ILabelable
{
    /// <summary>
    /// The mark (Pset_*Common.Reference), e.g. C1, G3; elements name their type by it. Unmarked
    /// elements get one made up, e.g. RC15.
    /// </summary>
    public virtual string Label { get; set; } = string.Empty;

    /// <summary>Where the sizes came from.</summary>
    public virtual DocumentReference? Source { get; set; }
}

/// <summary>IfcColumnType.</summary>
public record class ColumnType : ElementType
{
    /// <summary>Section size along the column's own x (IfcRectangleProfileDef.XDim).</summary>
    public virtual Length? XDim { get; set; }

    /// <summary>Section size along the column's own y (IfcRectangleProfileDef.YDim).</summary>
    public virtual Length? YDim { get; set; }

    /// <summary>Set for a round column (IfcCircleProfileDef), whose section is this across; then XDim and YDim are ignored.</summary>
    public virtual Length? Diameter { get; set; }
}

/// <summary>IfcBeamType.</summary>
public record class BeamType : ElementType, IRegexPatternProvider<BeamType>
{
    public static string RegexPattern { get; } = @"C?[GgBb]\d{1,3}[a-z]?";

    public virtual Length? Width { get; set; }

    public virtual Length? Depth { get; set; }
}

/// <summary>IfcWallType.</summary>
public record class WallType : ElementType
{
    public virtual Length? Thickness { get; set; }
}

/// <summary>IfcSlabType.</summary>
public record class SlabType : ElementType, IRegexPatternProvider<SlabType>
{
    public static string RegexPattern { get; } = @"C?S\d{1,3}[a-z]?";

    public virtual Length? Thickness { get; set; }
}

/// <summary>A door or window type (IfcDoorType, IfcWindowType), or a bare opening's.</summary>
public record class OpeningType : ElementType
{
    public virtual Length? Width { get; set; }

    public virtual Length? Height { get; set; }

    /// <summary>What fills its openings (IfcRelFillsElement).</summary>
    public virtual OpeningFilling Filling { get; set; }
}

public enum OpeningFilling
{
    None,
    Door,
    Window
}
