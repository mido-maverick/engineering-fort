namespace EngineeringFort.Formwork;

public record class FormworkSheathing : FormworkComponent
{
    public override string Name { get => base.Name; set => base.Name = value; }

    public virtual Length Thickness { get; set; }

    public virtual IFormworkSheathingMaterial? Material { get; set; }

    public virtual Pressure? AllowableBendingStress => Material?.AllowableBendingStress(Thickness);

    public virtual Pressure? AllowableShearStress => Material?.AllowableShearStress();

    public virtual Pressure? ElasticModulus => Material?.ElasticModulus(Thickness);
}
