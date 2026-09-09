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
}
