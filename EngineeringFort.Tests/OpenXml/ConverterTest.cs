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
}
