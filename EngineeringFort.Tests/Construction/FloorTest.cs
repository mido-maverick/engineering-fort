using EngineeringFort.Construction;
using System.Text.RegularExpressions;

namespace EngineeringFort.Tests.Construction;

public class FloorTest
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "1")]
    [InlineData(true, "1F")]
    [InlineData(true, "1FL")]
    [InlineData(false, "B")]
    [InlineData(false, "B1")]
    [InlineData(true, "B1F")]
    [InlineData(true, "B1FL")]
    [InlineData(true, "RF")]
    [InlineData(true, "RFL")]
    [InlineData(true, "RRF")]
    [InlineData(true, "PRF")]
    [InlineData(true, "B1FL/1FL")]
    public void Floor_RegexPattern_ShouldBeCorrect(bool expected, string str)
    {
        Assert.Equal(expected, Regex.Match(str, Floor.RegexPattern).Success);
    }
}
