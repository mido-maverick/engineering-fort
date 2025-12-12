using static EngineeringFort.SteelConstructionManual;
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
    public override FormworkSheathing FormworkComponent => base.FormworkComponent;

    public virtual Length UnitStripWidth { get; set; }

    public virtual Length SupportSpacing { get; set; }

    public virtual Area UnitStripCrossSectionalArea =>
        RectangularCrossSection.CalculateCrossSectionalArea(UnitStripWidth, FormworkComponent.Thickness);

    public virtual Volume UnitStripSectionModulus =>
        RectangularCrossSection.CalculateSectionModulus(UnitStripWidth, FormworkComponent.Thickness);

    public virtual AreaMomentOfInertia UnitStripMomentOfInertia =>
        RectangularCrossSection.CalculateMomentOfInertia(UnitStripWidth, FormworkComponent.Thickness);

    public override Pressure Pressure { get => base.Pressure; set => base.Pressure = value; }

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
        Limit = FormworkComponent.AllowableBendingStress ?? Pressure.Zero
    };

    public virtual Force MaximumShearForce => SimpleBeam.UniformlyDistributedLoad.Vmax(UniformlyDistributedLoad, SupportSpacing);

    public virtual double ShearStressSafetyFactor { get; set; } = 1;

    public virtual Pressure MaximumShearStress
    {
        get
        {
            try
            {
                return ShearStressSafetyFactor * (MaximumShearForce / (UnitStripWidth * FormworkComponent.Thickness));
            }
            catch (ArgumentException)
            {
                return new();
            }
        }
    }

    public virtual QuantityCheck<Pressure> ShearStressCheck => new()
    {
        Value = MaximumShearStress,
        Limit = FormworkComponent.AllowableShearStress ?? Pressure.Zero
    };

    public virtual Length MaximumDeflection => FormworkComponent.ElasticModulus is Pressure elasticModulus ?
        SimpleBeam.UniformlyDistributedLoad.Δmax(
            UniformlyDistributedLoad,
            SupportSpacing,
            elasticModulus,
            UnitStripMomentOfInertia) : new();

    public virtual Length AllowableDeflection { get; set; }

    public virtual QuantityCheck<Length> DeflectionCheck => new()
    {
        Value = MaximumDeflection,
        Limit = AllowableDeflection
    };

    public override IEnumerable<ICheck> SubChecks
    {
        get
        {
            yield return BendingStressCheck;
            yield return ShearStressCheck;
            yield return DeflectionCheck;
        }
    }
}

public record class FormworkSupportLayerCheck : FormworkLayerCheck<FormworkSupport>
{
    public override FormworkSupport FormworkComponent => base.FormworkComponent;

    public Orientation Orientation { get; set; }

    public virtual Length TributaryWidth { get; set; }

    public virtual Length SupportSpacing { get; set; }

    public virtual Length CantileverLength { get; set; }

    public override Pressure Pressure { get => base.Pressure; set => base.Pressure = value; }

    public virtual ForcePerLength UniformlyDistributedLoad => ForcePerLength.FromKilogramsForcePerCentimeter(
        Pressure.KilogramsForcePerSquareCentimeter * TributaryWidth.Centimeters);

    public virtual BeamCheck? BottomCantileverBeamCheck => Orientation is Orientation.Vertical ? new()
    {
        UniformlyDistributedLoad = UniformlyDistributedLoad,
        BeamForm = BeamForm.Cantilever,
        Length = CantileverLength,
        CrossSection = FormworkComponent.CrossSection,
        ElasticModulus = FormworkComponent.ElasticModulus ?? new(),
        AllowableBendingStress = FormworkComponent.AllowableBendingStress ?? new(),
        AllowableShearStress = FormworkComponent.AllowableShearStress ?? new(),
        AllowableDeflection = FormworkComponent.AllowableDeflection
    } : null;

    public virtual BeamCheck ContinuousBeamCheck => new()
    {
        UniformlyDistributedLoad = UniformlyDistributedLoad,
        BeamForm = BeamForm.Continuous,
        Length = SupportSpacing,
        CrossSection = FormworkComponent.CrossSection,
        ElasticModulus = FormworkComponent.ElasticModulus ?? Pressure.Zero,
        ShearStressSafetyFactor = ShearStressSafetyFactor,
        AllowableBendingStress = FormworkComponent.AllowableBendingStress ?? Pressure.Zero,
        AllowableShearStress = FormworkComponent.AllowableShearStress ?? Pressure.Zero,
        AllowableDeflection = FormworkComponent.AllowableDeflection
    };

    public virtual double ShearStressSafetyFactor { get; set; } = 1;

    public override IEnumerable<ICheck> SubChecks
    {
        get
        {
            if (BottomCantileverBeamCheck is not null) yield return BottomCantileverBeamCheck;
            yield return ContinuousBeamCheck;
        }
    }
}

public record class FormworkTieRodLayerCheck : FormworkLayerCheck<FormworkTieRod>
{
    public override FormworkTieRod FormworkComponent => base.FormworkComponent;

    public virtual Length HorizontalSpacing { get; set; }

    public virtual Length VerticalSpacing { get; set; }

    public virtual Length TributaryWidth => HorizontalSpacing;

    public virtual Length TributaryHeight => VerticalSpacing;

    public virtual Area TributaryArea => TributaryWidth * TributaryHeight;

    public override Pressure Pressure { get => base.Pressure; set => base.Pressure = value; }

    /// <summary>
    /// Maximum force applied on each tie rod
    /// </summary>
    public virtual Force MaximumAppliedForce => Pressure * TributaryArea;

    public override IEnumerable<ICheck> SubChecks => [];
}
