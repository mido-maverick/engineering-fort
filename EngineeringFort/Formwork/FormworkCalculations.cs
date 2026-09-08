namespace EngineeringFort.Formwork;

public interface ISideFormworkCalculation
{
    Pressure MaximumSidePressure { get; set; }
}

public interface IBottomFormworkCalculation
{
}

public record class SideFormworkCalculation : FormworkCalculation, ISideFormworkCalculation
{
    public virtual string Name { get; set; } = DisplayStrings.SideFormworkCalculation;

    public virtual Length MaximumHeight { get; set; }

    public virtual Pressure MaximumSidePressure
    {
        get;
        set
        {
            if (value.Equals(field, tolerance: Pressure.Zero)) return;

            var eventArgs = new QuantityChangedEventArgs<Pressure>(field, value);
            field = value;
            foreach (var formworkLayerCheck in FormworkLayerChecks) formworkLayerCheck?.Pressure = value;
            MaximumSidePressureChanged?.Invoke(this, eventArgs);
        }
    }

    public event EventHandler<QuantityChangedEventArgs<Pressure>>? MaximumSidePressureChanged;

    [Display(Name = nameof(FormworkLayerCheck), ResourceType = typeof(DisplayStrings))]
    public FormworkLayerCheck?[] FormworkLayerChecks { get; init; } = new FormworkLayerCheck?[5]; // TODO: limit length

    public override IEnumerable<ICheck> SubChecks => FormworkLayerChecks.OfType<ICheck>();
}
