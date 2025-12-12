namespace EngineeringFort.Formwork;

public interface IFormworkSheathingMaterial : IMaterial
{
    Pressure AllowableBendingStress(Length thickness);

    Pressure AllowableShearStress();

    Pressure ElasticModulus(Length thickness);
}
