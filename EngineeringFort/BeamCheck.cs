using static EngineeringFort.SteelConstructionManual;
using static EngineeringFort.SteelConstructionManual.BeamFormulas;

namespace EngineeringFort;

public record class BeamCheck : Check
{
    public virtual ForcePerLength? UniformlyDistributedLoad { get; set; }

    public virtual BeamForm BeamForm { get; set; }

    public virtual Length Length { get; set; }

    public virtual ICrossSection? CrossSection { get; set; }

    public virtual Pressure ElasticModulus { get; set; }

    public virtual Pressure AllowableBendingStress { get; set; }

    public virtual Pressure AllowableShearStress { get; set; }

    public virtual Length AllowableDeflection { get; set; }


    public virtual Torque MaximumBendingMoment => (UniformlyDistributedLoad, BeamForm) switch
    {
        (ForcePerLength udl, BeamForm.Simple) => SimpleBeam.UniformlyDistributedLoad.Mmax(udl, Length),
        (ForcePerLength udl, BeamForm.Cantilever) => CantileverBeam.UniformlyDistributedLoad.Mmax(udl, Length),
        (ForcePerLength udl, BeamForm.Continuous) => ContinuousBeam.ThreeEqualSpans.AllSpansLoaded.Mmax(udl, Length),
        _ => new(),
    };

    public virtual Pressure MaximumBendingStress
    {
        get
        {
            if (CrossSection is null) return new();
            try
            {
                return Pressure.FromKilogramsForcePerSquareCentimeter(
                    MaximumBendingMoment.KilogramForceCentimeters /
                    CrossSection.SectionModulus.CubicCentimeters);
            }
            catch (ArgumentException)
            {
                return new();
            }
        }
    }

    public virtual QuantityCheck<Pressure>? BendingStressCheck => new()
    {
        Value = MaximumBendingStress,
        Limit = AllowableBendingStress
    };

    public virtual Force MaximumShearForce => (UniformlyDistributedLoad, BeamForm) switch
    {
        (ForcePerLength udl, BeamForm.Simple) => SimpleBeam.UniformlyDistributedLoad.Vmax(udl, Length),
        (ForcePerLength udl, BeamForm.Cantilever) => CantileverBeam.UniformlyDistributedLoad.Vmax(udl, Length),
        (ForcePerLength udl, BeamForm.Continuous) => ContinuousBeam.ThreeEqualSpans.AllSpansLoaded.Vmax(udl, Length),
        _ => new(),
    };

    public virtual double ShearStressSafetyFactor { get; set; } = 1;

    public virtual Pressure MaximumShearStress
    {
        get
        {
            if (CrossSection is null) return new();
            try
            {
                return ShearStressSafetyFactor * MaximumShearForce / CrossSection.CrossSectionalArea;
            }
            catch (ArgumentException)
            {
                return new();
            }
        }
    }

    public virtual QuantityCheck<Pressure>? ShearStressCheck => new()
    {
        Value = MaximumShearStress,
        Limit = AllowableShearStress
    };

    public virtual Length MaximumDeflection => (UniformlyDistributedLoad, BeamForm, CrossSection) switch
    {
        (ForcePerLength udl, BeamForm.Simple, ICrossSection cs) =>
            SimpleBeam.UniformlyDistributedLoad.Δmax(udl, Length, ElasticModulus, cs.MomentOfInertia),
        (ForcePerLength udl, BeamForm.Cantilever, ICrossSection cs) =>
            CantileverBeam.UniformlyDistributedLoad.Δmax(udl, Length, ElasticModulus, cs.MomentOfInertia),
        (ForcePerLength udl, BeamForm.Continuous, ICrossSection cs) =>
            ContinuousBeam.ThreeEqualSpans.AllSpansLoaded.Δmax(udl, Length, ElasticModulus, cs.MomentOfInertia),
        _ => new(),
    };

    public virtual QuantityCheck<Length>? DeflectionCheck => new()
    {
        Value = MaximumDeflection,
        Limit = AllowableDeflection
    };
}
