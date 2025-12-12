namespace EngineeringFort.Formwork;

public interface ISideFormworkDesign
{
    Pressure MaximumSidePressure { get; set; }
}

public interface IBottomFormworkDesign
{
}

public record class SideFormworkDesign : FormworkDesign, ISideFormworkDesign
{
    public virtual string Name { get; set; } = DisplayStrings.SideFormworkDesign;

    public virtual Length MaximumHeight { get; set; }

    public virtual Pressure MaximumSidePressure
    {
        get;
        set
        {
            field = value;
            foreach (var formworkLayerCheck in FormworkLayerChecks) formworkLayerCheck?.Pressure = value;
        }
    }

    [Display(Name = nameof(FormworkLayerCheck), ResourceType = typeof(DisplayStrings))]
    public FormworkLayerCheck?[] FormworkLayerChecks { get; init; } = new FormworkLayerCheck?[5]; // TODO: limit length

    public override IEnumerable<ICheck> SubChecks => FormworkLayerChecks.OfType<ICheck>();
}
