namespace EngineeringFort.Construction;

public record class Building : ILabelable
{
    public virtual string Label { get; set; } = string.Empty;

    /// <summary>Storeys</summary>
    public virtual IReadOnlyList<Floor> Floors { get; set; } = [];
}
