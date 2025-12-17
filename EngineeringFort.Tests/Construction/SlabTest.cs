using EngineeringFort.Construction;
using System.Text.RegularExpressions;

namespace EngineeringFort.Tests.Construction;

public class SlabTest
{
    [Theory]
    [InlineData(false, "")]
    [InlineData(false, "1")]
    [InlineData(false, "S")]
    [InlineData(true, "S1")]
    [InlineData(false, "s1")]
    [InlineData(false, "C")]
    [InlineData(false, "C1")]
    [InlineData(false, "C1S")]
    [InlineData(true, "CS1")]
    [InlineData(true, "CS1a")]
    [InlineData(true, "S1a")]
    public void Slab_RegexPattern_ShouldBeCorrect(bool expected, string str)
    {
        Assert.Equal(expected, Regex.Match(str, Slab.RegexPattern).Success);
    }
}
