namespace EngineeringFort;

/// <summary>
///     How one quantity is written: which unit to convert to, how many figures to keep, and whether
///     the abbreviation is shown.
/// </summary>
/// <remarks>
///     <para>
///         The text comes from <see cref="QuantityOptions.Formats"/>, whose entries read
///         <c>"&lt;precision&gt; [&lt;unit&gt;] [omit]"</c> — <c>"G6 cm omit"</c> for six significant
///         figures in centimetres with no abbreviation, <c>"G3 kgf/cm²"</c> for three with one.
///         The precision is any numeric format string; the unit is an abbreviation
///         <see cref="UnitParser"/> understands, and is also what gets printed, so a document and a
///         page reading the same entry cannot disagree about the glyph.
///     </para>
///     <para>
///         A default instance keeps the unit the quantity carries and prints it to
///         <see cref="DefaultPrecision"/>, which is what a member with no entry gets.
///     </para>
/// </remarks>
/// <param name="Text">The configured entry, or <see langword="null"/> for the default.</param>
public readonly record struct QuantityFormat(string? Text)
{
    /// <summary>Applied when a member has no configured format.</summary>
    public const string DefaultPrecision = "0.0##";

    /// <summary>The trailing word that suppresses the abbreviation.</summary>
    public const string OmitUnit = "omit";

    /// <summary>
    ///     The unit the entry names, or <see langword="null"/> when it keeps the quantity's own —
    ///     for callers that need the unit rather than the rendered text, such as a unit picker
    ///     choosing which entry to select.
    /// </summary>
    public string? Unit => Parse().Unit;

    /// <summary>Writes <paramref name="quantity"/> as configured.</summary>
    /// <param name="abbreviation">
    ///     Overrides the entry's own answer, for callers that supply the unit themselves — a column
    ///     heading that already carries it, or a spreadsheet cell that must stay numeric.
    /// </param>
    public string Format(IQuantity quantity, bool? abbreviation = null)
    {
        var (precision, unit, omit) = Parse();
        var target = unit is null
            ? quantity.Unit
            : UnitParser.Default.Parse(unit, quantity.Unit.GetType());
        var text = quantity.As(target).ToString(precision);

        return abbreviation ?? !omit
            ? $"{text} {unit ?? UnitAbbreviationsCache.Default.GetDefaultAbbreviation(target)}"
            : text;
    }

    /// <remarks>
    ///     An entry naming a unit is converted and labelled whether or not it says <c>omit</c>;
    ///     the two spellings differ only in the label. They used to differ in more than that — the
    ///     one without <c>omit</c> was handed to UnitsNet whole, where <c>"G6 cm"</c> is not a
    ///     format string it can read.
    /// </remarks>
    (string Precision, string? Unit, bool Omit) Parse() =>
        (string.IsNullOrWhiteSpace(Text) ? null : Text.Split(' ')) switch
        {
            [var precision, var unit, OmitUnit] => (precision, unit, true),
            [var precision, var unit, _] => (precision, unit, false),
            [var precision, OmitUnit] => (precision, null, true),
            [var precision, var unit] => (precision, unit, false),
            [var precision] => (precision, null, false),
            _ => (DefaultPrecision, null, false),
        };
}
