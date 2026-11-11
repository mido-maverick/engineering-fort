using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EngineeringFort.OpenXml;

/// <summary>One sheet <see cref="SheetTemplate.Write"/> writes.</summary>
/// <param name="Template">The template sheet it is a copy of, by name.</param>
/// <param name="HeaderRows">The template sheet's title and heading rows; the rows after them are its sample rows.</param>
/// <param name="Name">The sheet's name, made one Excel takes by <see cref="SpreadsheetNames.SheetName"/>.</param>
/// <param name="Tokens">Text in the heading rows to replace, and what with.</param>
/// <param name="Fill">Writes the rows after the headings.</param>
public sealed record SheetFill(
    string Template, uint HeaderRows, string Name, IReadOnlyDictionary<string, string> Tokens, Action<TemplateRows> Fill);

/// <summary>
/// <para>
///     A workbook written from a template workbook: a copy of it whose sheets are copies of the
///     template's, filled. A template sheet has its title and heading rows, then a row of each kind
///     its table has. Those last rows are read for their styles and heights and left out; fonts,
///     borders, widths, colours, frozen headings and the page set-up are whatever the sheet has.
/// </para>
/// <para>
///     Every sheet prints at the least of the template sheets' scales, as Excel last worked them
///     out, and does not fit itself to the page: fitted, each would scale to its own content, and
///     the same text would print at different sizes from sheet to sheet. A template sheet no scale
///     was saved for keeps its own way of fitting. The heading rows repeat on every page; the
///     template's own sheets are left out.
/// </para>
/// </summary>
public static class SheetTemplate
{
    #region Fields
    private const string PrintTitlesName = "_xlnm.Print_Titles";
    #endregion

    #region Methods
    /// <summary>
    /// <paramref name="sheets"/>, in their order, written from <paramref name="template"/> into
    /// <paramref name="output"/>, which is read back as it is written: a <see cref="MemoryStream"/>,
    /// or a file opened for both.
    /// </summary>
    public static void Write(Stream template, Stream output, IEnumerable<SheetFill> sheets)
    {
        var fills = sheets.ToList();
        if (fills.Count is 0) throw new ArgumentException("There is no sheet to write.", nameof(sheets));

        template.CopyTo(output);
        using var document = SpreadsheetDocument.Open(output, isEditable: true);

        var workbookPart = document.WorkbookPart ?? throw new InvalidDataException("The template is not a workbook.");
        var workbook = workbookPart.Workbook;
        var styles = workbookPart.WorkbookStylesPart?.Stylesheet;
        var templateSheets = workbook.Sheets?.Elements<Sheet>().ToList() ?? [];
        var templates = fills.Select(fill => fill.Template).Distinct().ToDictionary(name => name, name =>
        {
            var part = templateSheets.FirstOrDefault(sheet => sheet.Name?.Value == name)?.Id?.Value is { } id
                ? (WorksheetPart)workbookPart.GetPartById(id)
                : throw new InvalidDataException($"The template has no sheet \"{name}\".");
            return (Part: part, Estimator: new RowHeightEstimator(part.Worksheet, styles));
        });
        var originals = workbookPart.WorksheetParts.ToList();
        var scale = templates.Values.Select(template => template.Part.Worksheet.GetFirstChild<PageSetup>()?.Scale?.Value).Min();
        string[] strings = [.. workbookPart.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>().Select(item => item.InnerText) ?? []];

        var sheetList = workbook.Sheets ??= new Sheets();
        sheetList.RemoveAllChildren();
        var printTitles = new DefinedNames();
        var names = new HashSet<string>();

        foreach (var (fill, position) in fills.Select((fill, index) => (fill, (uint)index)))
        {
            var (templatePart, estimator) = templates[fill.Template];
            var part = workbookPart.AddNewPart<WorksheetPart>();
            var worksheet = (Worksheet)templatePart.Worksheet.CloneNode(deep: true);
            part.Worksheet = worksheet;

            var data = worksheet.GetFirstChild<SheetData>() ?? throw new InvalidDataException($"The template sheet \"{fill.Template}\" has no cells.");
            var samples = data.Elements<Row>().Where(row => row.RowIndex?.Value > fill.HeaderRows).ToList();
            foreach (var sample in samples) sample.Remove();
            foreach (var cell in data.Elements<Row>().SelectMany(row => row.Elements<Cell>())) FillTokens(cell, strings, fill.Tokens);

            var rows = new TemplateRows(data, samples, fill.HeaderRows + 1, estimator);
            fill.Fill(rows);
            rows.Finish(worksheet, fill.HeaderRows);

            if (worksheet.GetFirstChild<PageSetup>() is { } pageSetup)
            {
                // The template's printer settings are its own part; a copy of the sheet cannot point at them.
                pageSetup.Id = null;
                if (scale is not null)
                {
                    pageSetup.Scale = scale;
                    pageSetup.FitToWidth = null;
                    pageSetup.FitToHeight = null;
                    if (worksheet.SheetProperties?.PageSetupProperties is { } fit) fit.FitToPage = null;
                }
            }
            worksheet.GetFirstChild<SheetDimension>()?.Remove();
            foreach (var view in worksheet.Descendants<SheetView>()) view.TabSelected = position is 0 ? true : null;

            var name = SpreadsheetNames.SheetName(fill.Name, names);
            sheetList.Append(new Sheet { Name = name, SheetId = position + 1, Id = workbookPart.GetIdOfPart(part) });
            printTitles.Append(new DefinedName($"'{name.Replace("'", "''")}'!$1:${fill.HeaderRows}")
            {
                Name = PrintTitlesName,
                LocalSheetId = position,
            });
        }

        foreach (var original in originals) workbookPart.DeletePart(original);
        // The template's chain lists its sample rows' formulas, which are gone.
        if (workbookPart.CalculationChainPart is { } chain) workbookPart.DeletePart(chain);
        workbook.DefinedNames = printTitles;
        // The template's active and first tabs count its own sheets; open on the first of these.
        foreach (var view in workbook.BookViews?.Elements<WorkbookView>() ?? [])
        {
            view.ActiveTab = null;
            view.FirstSheet = null;
        }
    }

    /// <summary>Puts <paramref name="tokens"/>' values into a heading cell that names any of them.</summary>
    private static void FillTokens(Cell cell, string[] strings, IReadOnlyDictionary<string, string> tokens)
    {
        if (cell.DataType?.Value != CellValues.SharedString ||
            !int.TryParse(cell.CellValue?.Text, out var index) || index < 0 || index >= strings.Length) return;

        var text = strings[index];
        if (!tokens.Keys.Any(text.Contains)) return;

        foreach (var (token, value) in tokens) text = text.Replace(token, value);
        cell.CellValue = null;
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }
    #endregion
}
