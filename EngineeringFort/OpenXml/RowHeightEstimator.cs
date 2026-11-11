using System.Globalization;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EngineeringFort.OpenXml;

/// <summary>
/// How tall Excel makes a row of wrapped text, estimated from a sheet's column widths and fonts, so
/// that a viewer showing the stored height — a preview, a phone, LibreOffice — shows every line.
/// CJK fonts such as 標楷體 set a CJK character an em wide and the rest about half.
/// </summary>
internal sealed class RowHeightEstimator
{
    #region Fields
    private const double PointsPerPixel = 0.75;

    /// <summary>A column's width unit is its default font's digit, about half an em: Calibri 11's is 7 px.</summary>
    private const double DigitPerEm = 0.5;

    /// <summary>The margin a column keeps beside its text, in pixels (ECMA-376 §18.3.1.13).</summary>
    private const double ColumnPaddingPixels = 5;

    /// <summary>
    /// What one level of a cell's indent takes from its text, in pixels: about a character of the
    /// default font, counted a little wide so that an estimate errs towards a row too tall.
    /// </summary>
    private const double IndentPixels = 10;

    private const double DefaultFontSize = 11;

    /// <summary>Excel's width when a sheet sets none.</summary>
    private const double DefaultColumnWidth = 8.43;

    /// <summary>A half-width character's share of an em.</summary>
    private const double HalfWidth = 0.5;

    /// <summary>A line of a CJK font in Excel, in ems: three lines of 8 pt 標楷體 take 33.75 pt.</summary>
    private const double LineHeight = 1.4;

    /// <summary>Row heights go by whole pixels.</summary>
    private const double HeightStep = 0.75;

    /// <summary>The tallest row Excel allows.</summary>
    private const double MaxHeight = 409.5;

    /// <summary>The first character set an em wide: CJK, its punctuation, and the symbols CJK fonts draw full width.</summary>
    private const char FirstFullWidth = ' ';

    private const char FirstHalfWidthForm = '｡';
    private const char LastHalfWidthForm = 'ￜ';

    private readonly double _pixelsPerUnit;
    private readonly double _defaultWidth;
    private readonly Dictionary<int, double> _widths = [];
    private readonly double[] _fontSizes;
    private readonly uint[] _styleFonts;
    private readonly uint[] _styleIndents;
    private readonly double _defaultFontSize;
    #endregion

    #region Constructors
    public RowHeightEstimator(Worksheet sheet, Stylesheet? styles)
    {
        _fontSizes = [.. styles?.Fonts?.Elements<Font>().Select(font => font.FontSize?.Val?.Value ?? DefaultFontSize) ?? []];
        CellFormat[] formats = [.. styles?.CellFormats?.Elements<CellFormat>() ?? []];
        _styleFonts = [.. formats.Select(format => format.FontId?.Value ?? 0)];
        _styleIndents = [.. formats.Select(format => format.Alignment?.Indent?.Value ?? 0)];

        var baseFont = styles?.CellStyleFormats?.Elements<CellFormat>().FirstOrDefault()?.FontId?.Value ?? 0;
        _defaultFontSize = baseFont < _fontSizes.Length ? _fontSizes[baseFont] : DefaultFontSize;
        _pixelsPerUnit = Math.Round(_defaultFontSize / PointsPerPixel * DigitPerEm);

        _defaultWidth = sheet.GetFirstChild<SheetFormatProperties>()?.DefaultColumnWidth?.Value ?? DefaultColumnWidth;
        foreach (var column in sheet.GetFirstChild<Columns>()?.Elements<Column>() ?? [])
        {
            if (column is not { Min.Value: var min, Max.Value: var max, Width.Value: var width }) continue;
            for (var index = (int)min - 1; index < max; index++) _widths[index] = width;
        }
    }
    #endregion

    #region Methods
    /// <summary>The height <paramref name="text"/> needs in the column at <paramref name="column"/>, styled <paramref name="style"/>.</summary>
    public double Height(int column, uint? style, string text)
    {
        if (text.Length is 0) return 0;

        var (size, indent) = style is { } index && index < _styleFonts.Length
            ? (_styleFonts[index] < _fontSizes.Length ? _fontSizes[_styleFonts[index]] : _defaultFontSize, _styleIndents[index] * IndentPixels)
            : (_defaultFontSize, 0.0);
        var width = (_widths.GetValueOrDefault(column, _defaultWidth) * _pixelsPerUnit - ColumnPaddingPixels - indent) * PointsPerPixel;

        var lines = text.ReplaceLineEndings("\n").Split('\n').Sum(paragraph => Lines(paragraph, width / size));
        var height = Math.Ceiling(lines * size * LineHeight / HeightStep) * HeightStep;
        return Math.Min(height, MaxHeight);
    }

    /// <summary>A value as a cell shows it, for <see cref="Height"/>.</summary>
    public static string Text(object? value) => value switch
    {
        null => "",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? "",
    };

    /// <summary>
    /// The lines <paramref name="paragraph"/> wraps to in <paramref name="ems"/> ems: CJK breaks
    /// after any character, other text between words, and a word too long for a line by character.
    /// </summary>
    private static int Lines(string paragraph, double ems)
    {
        if (ems <= 0) return 1;

        var lines = 1;
        var used = 0.0;
        foreach (var unit in Units(paragraph))
        {
            var width = unit.Sum(Width);
            if (used + width <= ems) { used += width; continue; }
            if (width <= ems) { lines++; used = width; continue; }

            foreach (var c in unit)
            {
                if (used + Width(c) > ems && used > 0) { lines++; used = 0; }
                used += Width(c);
            }
        }
        return lines;
    }

    /// <summary>What a line may not break inside: a full-width character alone, or a word with the space after it.</summary>
    private static IEnumerable<string> Units(string paragraph)
    {
        var start = 0;
        for (var i = 0; i < paragraph.Length; i++)
        {
            if (IsFullWidth(paragraph[i]))
            {
                if (i > start) yield return paragraph[start..i];
                yield return paragraph[i].ToString();
                start = i + 1;
            }
            else if (paragraph[i] == ' ')
            {
                yield return paragraph[start..(i + 1)];
                start = i + 1;
            }
        }
        if (start < paragraph.Length) yield return paragraph[start..];
    }

    private static double Width(char c) => IsFullWidth(c) ? 1 : HalfWidth;

    private static bool IsFullWidth(char c) =>
        c >= FirstFullWidth && c is not (>= FirstHalfWidthForm and <= LastHalfWidthForm);
    #endregion
}
