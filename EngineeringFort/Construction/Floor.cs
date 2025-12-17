namespace EngineeringFort.Construction;

public record class Floor : IRegexPatternProvider<Floor>, ILabelable
{
    public static string RegexPattern { get; } = @"[BR]?\d{1,3}FL?|[RP]?RFL?";

    public virtual string Label { get; set; } = string.Empty;
}
