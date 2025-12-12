namespace EngineeringFort.Formwork;

public record class FormworkTieRod : FormworkComponent
{
    public override string Name { get => base.Name; set => base.Name = value; }

    public virtual Force AllowableTensileForce { get; set; }
}
