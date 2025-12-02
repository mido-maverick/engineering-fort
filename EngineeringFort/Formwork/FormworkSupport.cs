namespace EngineeringFort.Formwork;

public record class FormworkSupport : FormworkComponent
{
    public virtual ICrossSection CrossSection { get; } = new RectangularCrossSection();

    public virtual IFormworkSupportMaterial? Material { get; set; }

    public virtual Pressure? AllowableBendingStress => Material?.AllowableBendingStress();

    public virtual Pressure? AllowableShearStress => Material?.AllowableShearStress();

    public virtual Pressure? ElasticModulus => Material?.ElasticModulus();
}
