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

    [Theory]
    [InlineData("H150×75×5×7", 15 * 0.5)]                       // H·tw, not the 17.85 gross area
    [InlineData("C100×50×5×7.5", 10 * 0.5)]                     // H·tw, not the 11.92 gross area
    [InlineData("□100×100×3.2", 2 * (10 - 2 * 0.32) * 0.32)]    // both webs, clear height
    public void BeamCheck_MaximumShearStress_ShouldAverageOverTheWebOfASteelShape(string name, double shearArea)
    {
        // Arrange — a simple span, so V = wL/2 = 10 × 300 / 2 = 1500 kgf
        var beamCheck = new BeamCheck()
        {
            UniformlyDistributedLoad = ForcePerLength.FromKilogramsForcePerCentimeter(10),
            BeamForm = SteelConstructionManual.BeamForm.Simple,
            Length = Length.FromCentimeters(300),
            CrossSection = SteelConstructionManual.SteelSpecs.Presets[name].CrossSection,
        };

        // Act
        var result = beamCheck.MaximumShearStress;

        // Assert
        Assert.Equal(1500 / shearArea, result.KilogramsForcePerSquareCentimeter, 5);
    }

    [Fact]
    public void BeamCheck_MaximumShearStress_ShouldStillAverageOverTheWholeOfASolidSection()
    {
        // Arrange — a 4.5 × 9 timber, where the shear-stress factor carries the 1.5 for a rectangle
        var beamCheck = new BeamCheck()
        {
            UniformlyDistributedLoad = ForcePerLength.FromKilogramsForcePerCentimeter(10),
            BeamForm = SteelConstructionManual.BeamForm.Simple,
            Length = Length.FromCentimeters(300),
            CrossSection = new RectangularCrossSection
            {
                Width = Length.FromCentimeters(4.5),
                Height = Length.FromCentimeters(9),
            },
        };

        // Act
        var result = beamCheck.MaximumShearStress;

        // Assert
        Assert.Equal(1500 / (4.5 * 9), result.KilogramsForcePerSquareCentimeter, 6);
    }
}
