using EngineeringFort.Construction;
using System.Text.RegularExpressions;

namespace EngineeringFort.Tests.Construction;

public class BeamTest
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "'")]
    [InlineData(false, "1")]
    [InlineData(false, "G")]
    [InlineData(true, "G1")]
    [InlineData(true, "g1")]
    [InlineData(true, "B1")]
    [InlineData(true, "b1")]
    [InlineData(false, "C")]
    [InlineData(false, "C1")]
    [InlineData(false, "C1G")]
    [InlineData(true, "CG1")]
    [InlineData(true, "CG1a")]
    [InlineData(true, "G1a")]
    [InlineData(true, "G1'")]
    [InlineData(true, "G1'-1")]
    [InlineData(true, "G1-1")]
    public void Beam_RegexPattern_ShouldBeCorrect(bool expected, string str)
    {
        Assert.Equal(expected, Regex.Match(str, Beam.RegexPattern).Success);
    }
}
