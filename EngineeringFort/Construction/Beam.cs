namespace EngineeringFort.Construction;

public record class Beam : IRegexPatternProvider<Beam>, ILabelable
{
    public static string RegexPattern { get; } = @"C?[GgBb]\d{1,3}[a-z]?";

    public virtual string Label { get; set; } = string.Empty;
}
