namespace EngineeringFort.Formwork;

public record class FormworkSheathing : FormworkComponent
{
    public virtual Length Thickness { get; set; }

    public virtual IFormworkSheathingMaterial? Material { get; set; }

    public virtual Pressure? AllowableBendingStress => Material?.AllowableBendingStress(Thickness);

    public virtual Pressure? AllowableShearStress => Material?.AllowableShearStress();

    public virtual Pressure? ElasticModulus => Material?.ElasticModulus(Thickness);
}
