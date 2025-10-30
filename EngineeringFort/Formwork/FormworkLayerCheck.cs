using static EngineeringFort.SteelConstructionManual.BeamFormulas;

namespace EngineeringFort.Formwork;

public abstract record class FormworkLayerCheck : Check
{
    public virtual Pressure Pressure { get; set; }
}

public abstract record class FormworkLayerCheck<T> : FormworkLayerCheck where T : FormworkComponent, new()
{
    public virtual T FormworkComponent { get; } = new();
}

public record class FormworkSheathingLayerCheck : FormworkLayerCheck<FormworkSheathing>
{
    public virtual Length UnitStripWidth { get; set; }

    public virtual Length SupportSpacing { get; set; }

    public virtual Volume UnitStripSectionModulus =>
        RectangularCrossSection.CalculateSectionModulus(UnitStripWidth, FormworkComponent.Thickness);

    public virtual AreaMomentOfInertia UnitStripMomentOfInertia =>
        RectangularCrossSection.CalculateMomentOfInertia(UnitStripWidth, FormworkComponent.Thickness);

    public virtual ForcePerLength UniformlyDistributedLoad => ForcePerLength.FromKilogramsForcePerCentimeter(
        Pressure.KilogramsForcePerSquareCentimeter * UnitStripWidth.Centimeters);

    public virtual Torque MaximumBendingMoment => SimpleBeam.UniformlyDistributedLoad.Mmax(UniformlyDistributedLoad, SupportSpacing);

    public virtual Pressure MaximumBendingStress
    {
        get
        {
            try
            {
                return Pressure.FromKilogramsForcePerSquareCentimeter(
                    MaximumBendingMoment.KilogramForceCentimeters /
                    UnitStripSectionModulus.CubicCentimeters);
            }
            catch (ArgumentException)
            {
                return new();
            }
        }
    }

    public virtual QuantityCheck<Pressure> BendingStressCheck => new()
    {
        Value = MaximumBendingStress,
        Limit = FormworkComponent.AllowableBendingStress ?? new()
    };

    public virtual Length MaximumDeflection => FormworkComponent.ElasticModulus is Pressure elasticModulus ?
        SimpleBeam.UniformlyDistributedLoad.Δmax(
            UniformlyDistributedLoad,
            SupportSpacing,
            elasticModulus,
            UnitStripMomentOfInertia) : new();
}

public record class FormworkSupportLayerCheck : FormworkLayerCheck<FormworkSupport>
{
    public virtual Length TributaryWidth { get; set; }

    public virtual Length SupportSpacing { get; set; }

    public virtual ForcePerLength UniformlyDistributedLoad => ForcePerLength.FromKilogramsForcePerCentimeter(
        Pressure.KilogramsForcePerSquareCentimeter * TributaryWidth.Centimeters);

    public virtual Torque MaximumBendingMoment => ContinuousBeam.ThreeEqualSpans.AllSpansLoaded.Mmax(UniformlyDistributedLoad, SupportSpacing);

    public virtual Pressure MaximumBendingStress
    {
        get
        {
            try
            {
                return Pressure.FromKilogramsForcePerSquareCentimeter(
                    MaximumBendingMoment.KilogramForceCentimeters /
                    FormworkComponent.CrossSection.SectionModulus.CubicCentimeters);
            }
            catch (ArgumentException)
            {
                return new();
            }
        }
    }

    public virtual QuantityCheck<Pressure> BendingStressCheck => new()
    {
        Value = MaximumBendingStress,
        Limit = FormworkComponent.AllowableBendingStress ?? new()
    };
}

public record class FormworkTieRodLayerCheck : FormworkLayerCheck<FormworkTieRod>
{
}
