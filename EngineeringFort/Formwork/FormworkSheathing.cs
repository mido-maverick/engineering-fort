namespace EngineeringFort.Formwork;

/// <summary>
/// The sheet the concrete is cast against, as a type (IfcPlateType): one material of one
/// <see cref="Thickness"/>, which is what IFC calls a material layer (IfcMaterialLayer, in an
/// IfcMaterialLayerSet).
/// </summary>
public record class FormworkSheathing : FormworkComponent
{
    public override string Name { get => base.Name; set => base.Name = value; }

    public virtual IFormworkSheathingMaterial? Material { get; set; }

    public virtual Length Thickness { get; set; }

    public virtual Pressure? AllowableBendingStress => Material?.AllowableBendingStress(Thickness);

    public virtual Pressure? AllowableShearStress => Material?.AllowableShearStress();

    public virtual Pressure? ElasticModulus => Material?.ElasticModulus(Thickness);
}
