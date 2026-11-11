using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EngineeringFort.OpenXml;

namespace EngineeringFort.Tests.OpenXml;

public class SheetTemplateTest
{
    private const string First = "First";
    private const string Second = "Second";
    private const string Token = "{name}";
    private const uint HeaderRows = 2;
    private const int ColumnCount = 3;
    private const double SampleHeight = 13.5;
    private const double ColumnWidth = 10;
    private const double DataFontSize = 8;
    private const uint FirstScale = 98;
    private const uint SecondScale = 97;
    private const int FixedSample = 0;
    private const int FittedSample = 1;

    private static readonly string Long = new('危', 60);

    /// <summary>
    /// Two template sheets, <see cref="Second"/> first: a title with a token, a heading row, two
    /// sample rows, a page break of their own, and each its own fitted scale.
    /// </summary>
    private static MemoryStream Template()
    {
        var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            workbookPart.AddNewPart<WorkbookStylesPart>().Stylesheet = new Stylesheet(
                new Fonts(new Font(new FontSize { Val = 11 }), new Font(new FontSize { Val = DataFontSize })),
                new Fills(new Fill(new PatternFill { PatternType = PatternValues.None })),
                new Borders(new Border()),
                new CellStyleFormats(new CellFormat { FontId = 0 }),
                new CellFormats(new CellFormat { FontId = 0 }, new CellFormat { FontId = 1, ApplyFont = true }));

            workbookPart.AddNewPart<SharedStringTablePart>().SharedStringTable =
                new SharedStringTable(new SharedStringItem(new Text($"Title {Token}")));

            var second = TemplateSheet(workbookPart, SecondScale);
            var first = TemplateSheet(workbookPart, FirstScale);
            workbookPart.Workbook.AppendChild(new Sheets(
                new Sheet { Name = Second, SheetId = 1, Id = workbookPart.GetIdOfPart(second) },
                new Sheet { Name = First, SheetId = 2, Id = workbookPart.GetIdOfPart(first) }));
        }
        stream.Position = 0;
        return stream;
    }

    private static WorksheetPart TemplateSheet(WorkbookPart workbookPart, uint scale)
    {
        var data = new SheetData(new Row(
            new Cell { CellReference = "A1", DataType = CellValues.SharedString, CellValue = new CellValue("0") })
        { RowIndex = 1 });
        for (var row = 2u; row <= HeaderRows; row++) data.Append(new Row { RowIndex = row });
        for (var row = HeaderRows + 1; row <= HeaderRows + 2; row++)
            data.Append(new Row(Enumerable.Range(0, ColumnCount).Select(column =>
                new Cell { CellReference = $"{SpreadsheetNames.ColumnName(column)}{row}", StyleIndex = 1 }))
            { RowIndex = row, Height = SampleHeight, CustomHeight = true });

        var part = workbookPart.AddNewPart<WorksheetPart>();
        part.Worksheet = new Worksheet(
            new Columns(new Column { Min = 1, Max = ColumnCount, Width = ColumnWidth, CustomWidth = true }),
            data,
            new PageSetup { Scale = scale, FitToHeight = 0 },
            new RowBreaks(new Break { Id = HeaderRows + 1, ManualPageBreak = true }) { Count = 1, ManualBreakCount = 1 });
        part.Worksheet.SheetProperties = new SheetProperties(new PageSetupProperties { FitToPage = true });
        return part;
    }

    /// <summary>
    /// A sheet of each template, each a fixed row, a short fitted row and a long one, a page
    /// asked to break after the first and after the last.
    /// </summary>
    private static SheetFill[] Fills { get; } =
    [
        Fill(First, "One"),
        Fill(Second, "Two"),
    ];

    private static SheetFill Fill(string template, string name) =>
        new(template, HeaderRows, name, new Dictionary<string, string> { [Token] = name }, rows =>
        {
            rows.BreakAfter(rows.Add(FixedSample, fitText: false, "fixed", 1));
            rows.Add(FittedSample, fitText: true, "short");
            rows.BreakAfter(rows.Add(FittedSample, fitText: true, "long", Long));
        });

    private static SpreadsheetDocument Written(IEnumerable<SheetFill> fills)
    {
        using var template = Template();
        var output = new MemoryStream();
        SheetTemplate.Write(template, output, fills);
        output.Position = 0;
        return SpreadsheetDocument.Open(output, isEditable: false);
    }

    private static Worksheet[] Worksheets(SpreadsheetDocument document) =>
    [
        .. document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>()
            .Select(sheet => ((WorksheetPart)document.WorkbookPart.GetPartById(sheet.Id!.Value!)).Worksheet),
    ];

    private static Row[] Rows(Worksheet sheet) =>
        [.. sheet.GetFirstChild<SheetData>()!.Elements<Row>().Where(row => row.RowIndex!.Value > HeaderRows)];

    [Fact]
    public void Write_ShouldWriteTheFillsInOrder_EachWithItsPrintTitles()
    {
        // Act
        using var document = Written(Fills);

        // Assert
        Assert.Equal(new[] { "One", "Two" }, document.WorkbookPart!.Workbook.Sheets!.Elements<Sheet>().Select(sheet => sheet.Name!.Value!));
        Assert.Equal(2, document.WorkbookPart.WorksheetParts.Count());
        Assert.Equal(
            new[] { $"'One'!$1:${HeaderRows}", $"'Two'!$1:${HeaderRows}" },
            document.WorkbookPart.Workbook.DefinedNames!.Elements<DefinedName>().Select(name => name.Text));
    }

    [Fact]
    public void Write_ShouldFillTokensAndValues()
    {
        // Act
        using var document = Written(Fills);
        var sheet = Worksheets(document)[0];

        // Assert
        Assert.Equal("Title One", sheet.Descendants<Cell>().First().InnerText);
        var fixedRow = Rows(sheet)[0].Elements<Cell>().ToArray();
        Assert.Equal("fixed", fixedRow[0].InnerText);
        Assert.Equal("1", fixedRow[1].CellValue!.Text);
        Assert.Null(fixedRow[1].DataType);
    }

    [Fact]
    public void Write_ShouldGrowAFittedRowWithItsText_AndKeepTheSampleHeightOtherwise()
    {
        // Act
        using var document = Written(Fills);
        var rows = Rows(Worksheets(document)[0]);

        // Assert
        Assert.Equal(SampleHeight, rows[0].Height!.Value);
        Assert.True(rows[0].CustomHeight!.Value);
        Assert.Equal(SampleHeight, rows[1].Height!.Value);
        Assert.True(rows[2].Height!.Value > 3 * DataFontSize, $"{Long.Length} characters take only {rows[2].Height.Value} pt.");
        Assert.Null(rows[2].CustomHeight);
    }

    [Fact]
    public void Write_ShouldReplaceTheTemplatesBreaks_AndBreakAfterAllButTheLastRow()
    {
        // Act
        using var document = Written(Fills);

        // Assert
        var sheet = Worksheets(document)[0];
        Assert.Single(sheet.Elements<RowBreaks>());
        Assert.Equal(HeaderRows + 1, Assert.Single(sheet.Descendants<Break>()).Id!.Value);
    }

    [Fact]
    public void Write_ShouldPrintEverySheetAtTheLeastScale_Unfitted()
    {
        // Act
        using var document = Written(Fills);

        // Assert
        foreach (var sheet in Worksheets(document))
        {
            var pageSetup = sheet.GetFirstChild<PageSetup>()!;
            Assert.Equal(Math.Min(FirstScale, SecondScale), pageSetup.Scale!.Value);
            Assert.Null(pageSetup.FitToHeight);
            Assert.Null(sheet.SheetProperties!.PageSetupProperties!.FitToPage);
        }
    }

    [Fact]
    public void Write_ShouldRefuseATemplateSheetItDoesNotHave()
    {
        Assert.Throws<InvalidDataException>(() => Written([Fill("Missing", "One")]));
    }

    [Theory]
    [InlineData(0, "A")]
    [InlineData(25, "Z")]
    [InlineData(26, "AA")]
    [InlineData(27, "AB")]
    [InlineData(701, "ZZ")]
    [InlineData(702, "AAA")]
    public void ColumnName_ShouldCountInLetters(int index, string expected)
    {
        Assert.Equal(expected, SpreadsheetNames.ColumnName(index));
    }

    [Fact]
    public void SheetName_ShouldStandInForRefusedCharacters_CutToLength_AndRefuseATakenName()
    {
        // Arrange
        var taken = new HashSet<string>();

        // Act
        var name = SpreadsheetNames.SheetName("A/B:" + new string('x', 40), taken);

        // Assert
        Assert.Equal("A_B_" + new string('x', 27), name);
        Assert.Throws<InvalidDataException>(() => SpreadsheetNames.SheetName(name, taken));
    }
}
