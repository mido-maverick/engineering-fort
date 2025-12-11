namespace EngineeringFort.Tests;

public class BeamCheckTest
{
    [Fact]
    public void BeamCheck_ShouldBeCorrect()
    {
        // Arrange
        var beamCheck = new BeamCheck()
        {

        };

        // Act
        var result = beamCheck.IsValid;

        // Assert
        Assert.False(result);
    }
}
