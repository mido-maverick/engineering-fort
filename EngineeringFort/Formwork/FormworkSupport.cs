namespace EngineeringFort.Formwork;

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
