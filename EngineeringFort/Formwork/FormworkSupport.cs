namespace EngineeringFort.Formwork;

/// <summary>
/// A linear member under or behind the sheathing — a stud, a waler, a joist, a stringer — as a
/// type (IfcBeamType or IfcMemberType): one material on one section, which is what IFC calls a
/// material profile (IfcMaterialProfile, in an IfcMaterialProfileSet, the
/// <see cref="CrossSection"/> being its IfcProfileDef).
/// </summary>
public record class FormworkSupport : FormworkComponent
{
    public override string Name { get => base.Name; set => base.Name = value; }

    public virtual IFormworkSupportMaterial? Material { get; set; }

    public virtual ICrossSection CrossSection { get; init; } = new RectangularCrossSection();

    public virtual Pressure? AllowableBendingStress => Material?.AllowableBendingStress();

    public virtual Pressure? AllowableShearStress => Material?.AllowableShearStress();

    public virtual Length AllowableDeflection { get; set; }

    public virtual Pressure? ElasticModulus => Material?.ElasticModulus();
}
