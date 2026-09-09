using UnitsNet;

namespace EngineeringFort.Tests;

public class QuantityFormatTest
{
    [Theory]
    // Precision only: the quantity keeps the unit it carries, and says so.
    [InlineData(1.5, null, "1.5 cm")]
    [InlineData(1.5, "0.000", "1.500 cm")]
    // "omit" drops the abbreviation, nothing else.
    [InlineData(1.5, "0.000 omit", "1.500")]
    // A named unit is converted to, whether or not the abbreviation follows.
    [InlineData(1.5, "G6 mm", "15 mm")]
    [InlineData(1.5, "G6 mm omit", "15")]
    [InlineData(1.5, "0.000 m omit", "0.015")]
    public void QuantityFormat_Format_ShouldReadTheConfiguredEntry(
        double centimeters, string? text, string expected)
    {
        // Arrange
        var length = Length.FromCentimeters(centimeters);

        // Act
        var actual = new QuantityFormat(text).Format(length);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void QuantityFormat_Format_ShouldLetTheCallerOverrideTheAbbreviation()
    {
        // Arrange — a caller that supplies the unit itself, such as a column heading.
        var length = Length.FromCentimeters(1.5);

        // Act
        var withheld = new QuantityFormat("G6 mm").Format(length, abbreviation: false);
        var added = new QuantityFormat("G6 mm omit").Format(length, abbreviation: true);

        // Assert
        Assert.Equal("15", withheld);
        Assert.Equal("15 mm", added);
    }
}
