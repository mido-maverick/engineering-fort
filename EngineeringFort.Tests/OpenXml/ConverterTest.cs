using DocumentFormat.OpenXml.Wordprocessing;
using EngineeringFort.OpenXml;

namespace EngineeringFort.Tests.OpenXml;

public class ConverterTest
{
    [Theory]
    [InlineData(123.0, "0.0", "123.0 kgf/cm²")]
    [InlineData(123.456789, null, "123.457 kgf/cm²")]
    [InlineData(123.456789, "G6", "123.457 kgf/cm²")]
    [InlineData(123.456789, "F6", "123.456789 kgf/cm²")]
    [InlineData(123.456789, "0.0 omit", "123.5")]
    [InlineData(123.456789, "0.0# omit", "123.46")]
    [InlineData(123.456789, "0.0## omit", "123.457")]
    [InlineData(123.456789, "G6 omit", "123.457")]
    [InlineData(123.456789, "F6 omit", "123.456789")]
    [InlineData(1.23456789, "G6 omit", "1.23457")]
    [InlineData(1.23456789, "F6 omit", "1.234568")]
    public void Converter_Format_ShouldBeCorrect(double quantityValue, string? format, string expectedResult)
    {
        // Arrange
        var stress = UnitsNet.Pressure.FromKilogramsForcePerSquareCentimeter(quantityValue);

        // Act
        var formatMethod = typeof(Converter).GetMethod("Format", BindingFlags.NonPublic | BindingFlags.Static)!;
        var actualResult = formatMethod.Invoke(obj: null, parameters: [stress, format]);

        // Assert
        Assert.Equal(expectedResult, actualResult);
    }

    public static TheoryData<byte[]> Images400By300 => new()
    {
        // PNG: signature, IHDR length and type, width 400, height 300.
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D, 0x49, 0x48, 0x44, 0x52, 0, 0, 0x01, 0x90, 0, 0, 0x01, 0x2C] },
        // JPEG: SOI, an APP0 to skip, SOF0 with precision, height 300, width 400, then the rest of the frame.
        { [0xFF, 0xD8, 0xFF, 0xE0, 0, 0x06, 0, 0, 0, 0, 0xFF, 0xC0, 0, 0x11, 0x08, 0x01, 0x2C, 0x01, 0x90, 0x03, 0, 0, 0, 0, 0] },
    };

    [Theory]
    [MemberData(nameof(Images400By300))]
    public void Converter_TryGetImageAspect_ShouldReadHeaderFromCurrentPosition(byte[] header)
    {
        // Arrange
        byte[] before = [1, 2, 3];
        using var stream = new MemoryStream([.. before, .. header]);
        stream.Position = before.Length;
        var method = typeof(Converter).GetMethod("TryGetImageAspect", BindingFlags.NonPublic | BindingFlags.Static)!;
        object?[] arguments = [stream, null];

        // Act
        var read = (bool)method.Invoke(obj: null, parameters: arguments)!;

        // Assert
        Assert.True(read);
        Assert.Equal(0.75, (double)arguments[1]!, precision: 6);
        Assert.Equal(before.Length, stream.Position);
    }

    [Fact]
    public void Converter_MergeRows_ShouldMergeRunsOfAGroupAndUnwrapTheRows()
    {
        // Arrange: three rows, the first two of one group; column B merges by nothing they share.
        int[] groups = [1, 1, 2];
        SdtRow[] rows = [.. groups.Select((group, i) => Row(("A", $"a{group}"), ("B", $"b{i}")))];
        var table = new Table(rows);
        (string, Func<int, object?>)[] merges = [("A", group => group), ("B", _ => new object()), ("C", group => group)];
        var method = typeof(Converter).GetMethod("MergeRows", BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(typeof(int));

        // Act
        method.Invoke(obj: null, parameters: [rows, groups, merges]);

        // Assert
        Assert.Empty(table.Descendants<SdtElement>());
        var cells = table.Elements<TableRow>().Select(row => row.Elements<TableCell>().ToArray()).ToArray();
        Assert.Equal(3, cells.Length);
        Assert.Equal(MergedCellValues.Restart, cells[0][0].TableCellProperties?.VerticalMerge?.Val?.Value);
        Assert.Equal("a1", cells[0][0].InnerText);
        Assert.NotNull(cells[1][0].TableCellProperties?.VerticalMerge);
        Assert.Null(cells[1][0].TableCellProperties?.VerticalMerge?.Val);
        Assert.Equal("", cells[1][0].InnerText);
        Assert.Null(cells[2][0].TableCellProperties?.VerticalMerge);
        Assert.All(cells, row => Assert.Null(row[1].TableCellProperties?.VerticalMerge));
    }

    [Fact]
    public void Converter_Set_ShouldRepeatARowForEachOfAnArray()
    {
        // Arrange
        var template = Row(("A", ""));
        var table = new Table(template);
        object[] objects = [new { A = "x" }, new { A = "y" }];
        var method = typeof(Converter).GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(SdtElement), typeof(object), typeof(string)])!;

        // Act
        method.Invoke(new Converter(), [template, objects, null]);

        // Assert
        Assert.Equal(["x", "y"], table.Elements<SdtRow>().Select(row => row.InnerText));
    }

    [Fact]
    public void Converter_Set_ShouldRepeatABlockWithoutItemsForEachOfAnArray()
    {
        // Arrange: a page, its table's cells tagged but not its rows
        var template = new SdtBlock(
            new SdtProperties(new Tag { Val = "Pages" }),
            new SdtContentBlock(new Table(new TableRow(new SdtCell(
                new SdtProperties(new Tag { Val = "A" }),
                new SdtContentCell(new TableCell(new Paragraph(new Run(new Text("")))))))));
        var body = new Body(template);
        object[] objects = [new { A = "x" }, new { A = "y" }];
        var method = typeof(Converter).GetMethod("Set", BindingFlags.NonPublic | BindingFlags.Instance, [typeof(SdtElement), typeof(object), typeof(string)])!;

        // Act
        method.Invoke(new Converter(), [template, objects, null]);

        // Assert
        Assert.Equal(["x", "y"], body.Elements<SdtBlock>().Select(block => block.InnerText));
    }

    private static SdtRow Row(params (string Tag, string Text)[] cells) => new(
        new SdtContentRow(new TableRow(cells.Select(cell => new SdtCell(
            new SdtProperties(new Tag { Val = cell.Tag }),
            new SdtContentCell(new TableCell(new Paragraph(new Run(new Text(cell.Text))))))))));
}
