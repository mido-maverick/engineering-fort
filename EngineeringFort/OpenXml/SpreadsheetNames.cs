namespace EngineeringFort.OpenXml;

/// <summary>Names a spreadsheet gives its columns and takes for its sheets.</summary>
public static class SpreadsheetNames
{
    #region Fields
    private const int Letters = 26;
    private const int SheetNameLimit = 31;
    private static readonly char[] SheetNameRefused = ['[', ']', ':', '*', '?', '/', '\\'];
    private const char SheetNameStandIn = '_';
    #endregion

    #region Methods
    /// <summary>A column's letters, counted from zero: A to Z, then AA on.</summary>
    public static string ColumnName(int index) =>
        index < 0 ? throw new ArgumentOutOfRangeException(nameof(index))
        : (index < Letters ? "" : ColumnName(index / Letters - 1)) + (char)('A' + index % Letters);

    /// <summary>
    /// <paramref name="wanted"/> as a sheet may be named: the characters Excel refuses stood in for,
    /// cut to its length, and added to <paramref name="taken"/>, which must not have it yet.
    /// </summary>
    public static string SheetName(string wanted, ISet<string> taken)
    {
        var name = string.Concat(wanted.Select(c => SheetNameRefused.Contains(c) ? SheetNameStandIn : c));
        if (name.Length > SheetNameLimit) name = name[..SheetNameLimit];
        return taken.Add(name) ? name : throw new InvalidDataException($"Sheet name \"{name}\" is taken; it comes of \"{wanted}\".");
    }
    #endregion
}
