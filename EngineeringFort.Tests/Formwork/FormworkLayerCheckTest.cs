using EngineeringFort.Formwork;
using UnitsNet;

namespace EngineeringFort.Tests.Formwork;

public class FormworkLayerCheckTest
{
    public static IEnumerable<object[]> MaximumDeflectionTestData =>
    [
        [
            Length.FromCentimeters(0.15517),
            Pressure.FromKilogramsForcePerSquareCentimeter(0.345),
            Length.FromCentimeters(1.0),
            Length.FromCentimeters(26.4),
            Length.FromCentimeters(1.5),
            new 木構造建築物設計及施工技術規範.材料及容許應力.合板()
        ],
        [
            Length.FromCentimeters(0.13507),
            Pressure.FromKilogramsForcePerSquareCentimeter(0.483),
            Length.FromCentimeters(1.0),
            Length.FromCentimeters(25.5),
            Length.FromCentimeters(1.5),
            new 木構造建築物設計及施工技術規範.材料及容許應力.木材() { 樹種 = 木構造建築物設計及施工技術規範.材料及容許應力.樹種.柳桉 }
        ],
        [
            Length.FromCentimeters(0.18994),
            Pressure.FromKilogramsForcePerSquareCentimeter(0.35455),
            Length.FromCentimeters(1.0),
            Length.FromCentimeters(30.0),
            Length.FromCentimeters(1.5),
            new 木構造建築物設計及施工技術規範.材料及容許應力.木材() { 樹種 = 木構造建築物設計及施工技術規範.材料及容許應力.樹種.柳桉 }
        ]
    ];

    [Theory]
    [MemberData(nameof(MaximumDeflectionTestData))]
    public void FormworkSheathingLayerCheck_MaximumDeflection_ShouldBeCorrect(
        Length expextedMaxDeflection, Pressure pressure, Length unitStripWidth,
        Length supportSpacing, Length thickness, IFormworkSheathingMaterial formworkSheathingMaterial)
    {
        // Arrange
        var check = new FormworkSheathingLayerCheck()
        {
            Pressure = pressure,
            UnitStripWidth = unitStripWidth,
            SupportSpacing = supportSpacing,
        };
        check.FormworkComponent.Thickness = thickness;
        check.FormworkComponent.Material = formworkSheathingMaterial;

        // Act
        var maxDeflection = check.MaximumDeflection;

        // Assert
        Assert.Equal(expextedMaxDeflection.Centimeters, maxDeflection.Centimeters, 5);
    }
}
