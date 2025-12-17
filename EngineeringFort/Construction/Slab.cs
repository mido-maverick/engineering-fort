namespace EngineeringFort.Construction;

public record class Slab : IRegexPatternProvider<Slab>, ILabelable
{
    public static string RegexPattern { get; } = @"C?S\d{1,3}[a-z]?";

    public virtual string Label { get; set; } = string.Empty;
}
