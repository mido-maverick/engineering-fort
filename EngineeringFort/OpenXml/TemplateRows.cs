using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EngineeringFort.OpenXml;

/// <summary>
/// The rows a sheet written by <see cref="SheetTemplate"/> takes after its headings, each styled as
/// one of the template's sample rows, and the merges and page breaks they bring.
/// </summary>
public sealed class TemplateRows
{
    #region Fields
    /// <summary>The last column a page break runs to, counted from zero: the sheet's whole width.</summary>
    private const uint BreakExtent = 16383;

    private readonly SheetData _data;
    private readonly IReadOnlyList<Row> _samples;
    private readonly RowHeightEstimator _estimator;
    private readonly List<string> _merges = [];
    private readonly List<uint> _breaks = [];
    #endregion

    #region Constructors
    internal TemplateRows(SheetData data, IReadOnlyList<Row> samples, uint firstRow, RowHeightEstimator estimator)
    {
        _data = data;
        _samples = samples;
        _estimator = estimator;
        Next = firstRow;
    }
    #endregion

    #region Properties
    /// <summary>The row the next <see cref="Add"/> writes.</summary>
    public uint Next { get; private set; }

    /// <summary>The last column of the first sample row, counted from zero.</summary>
    public int LastColumn => Styles(0).Length - 1;
    #endregion

    #region Methods
    /// <summary>
    /// A row styled as the template's <paramref name="sample"/>th row after the headings, a value
    /// to a column from the first; the columns left over are empty. It is as tall as that row, or,
    /// with <paramref name="fitText"/>, taller where its text needs more lines. Strings are written
    /// as text and integers as numbers. Answers the row's number.
    /// </summary>
    public uint Add(int sample, bool fitText, params object?[] values)
    {
        var styles = Styles(sample);
        if (values.Length > styles.Length)
            throw new InvalidDataException($"The template's sample row has {styles.Length} columns; the data has {values.Length}.");

        var row = new Row { RowIndex = Next };
        var floor = _samples[sample].Height?.Value;
        if (!fitText)
        {
            if (floor is { } height)
            {
                row.Height = height;
                row.CustomHeight = true;
            }
        }
        else if (Enumerable.Range(0, values.Length)
            .Select(column => _estimator.Height(column, styles[column], RowHeightEstimator.Text(values[column])))
            .Append(floor ?? 0)
            .Max() is > 0 and var fitted)
        {
            // Not marked custom, so Excel may fit it again and a row edited later grows with its text.
            row.Height = fitted;
        }

        for (var column = 0; column < styles.Length; column++)
            row.Append(NewCell($"{SpreadsheetNames.ColumnName(column)}{Next}", styles[column], column < values.Length ? values[column] : null));

        _data.Append(row);
        return Next++;
    }

    /// <summary>Merges the cells between the two corners, columns counted from zero; one cell is left as it is.</summary>
    public void Merge(int firstColumn, uint firstRow, int lastColumn, uint lastRow)
    {
        if (firstColumn == lastColumn && firstRow >= lastRow) return;
        _merges.Add($"{SpreadsheetNames.ColumnName(firstColumn)}{firstRow}:{SpreadsheetNames.ColumnName(lastColumn)}{lastRow}");
    }

    /// <summary>Starts a new page after <paramref name="row"/>.</summary>
    public void BreakAfter(uint row) => _breaks.Add(row);

    /// <summary>
    /// Leaves the template's own merges among the headings, drops those of its sample rows and its
    /// page breaks, and adds the merges and page breaks the rows brought — none after the last row,
    /// which ends the sheet anyway.
    /// </summary>
    internal void Finish(Worksheet worksheet, uint headerRows)
    {
        var merges = worksheet.GetFirstChild<MergeCells>();
        if (merges is null)
            worksheet.InsertAfter(merges = new MergeCells(), worksheet.Elements().Last(element => element is
                SheetData or SheetCalculationProperties or SheetProtection or ProtectedRanges or Scenarios
                or AutoFilter or SortState or DataConsolidate or CustomSheetViews));
        foreach (var merge in merges.Elements<MergeCell>().Where(merge => FirstRow(merge) > headerRows).ToList())
            merge.Remove();
        merges.Append(_merges.Select(reference => new MergeCell { Reference = reference }));
        merges.Count = (uint)merges.Elements<MergeCell>().Count();
        if (merges.Count.Value is 0) merges.Remove();

        worksheet.GetFirstChild<RowBreaks>()?.Remove();
        var breaks = _breaks.Where(row => row < Next - 1).ToList();
        if (breaks.Count is 0) return;

        OpenXmlElement after = worksheet.GetFirstChild<HeaderFooter>()
            ?? (OpenXmlElement?)worksheet.GetFirstChild<PageSetup>()
            ?? throw new InvalidDataException("The template sheet has no page set-up.");
        worksheet.InsertAfter(
            new RowBreaks(breaks.Select(row => new Break { Id = row, Max = BreakExtent, ManualPageBreak = true }))
            {
                Count = (uint)breaks.Count,
                ManualBreakCount = (uint)breaks.Count,
            },
            after);
    }

    private uint?[] Styles(int sample) => sample < _samples.Count
        ? [.. _samples[sample].Elements<Cell>().Select(cell => cell.StyleIndex?.Value)]
        : throw new InvalidDataException($"The template has no sample row {sample + 1} after its headings.");

    private static Cell NewCell(string reference, uint? style, object? value)
    {
        var cell = new Cell { CellReference = reference };
        if (style is { } index) cell.StyleIndex = index;

        switch (value)
        {
            case string { Length: > 0 } text:
                cell.DataType = CellValues.InlineString;
                cell.InlineString = new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
                break;
            case int number:
                cell.CellValue = new(number.ToString(CultureInfo.InvariantCulture));
                break;
        }
        return cell;
    }

    private static uint FirstRow(MergeCell merge) =>
        uint.Parse(new string([.. (merge.Reference?.Value ?? "").TakeWhile(c => c != ':').Where(char.IsDigit)]));
    #endregion
}
