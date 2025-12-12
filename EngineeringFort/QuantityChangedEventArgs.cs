namespace EngineeringFort;

/// <summary>
/// Changed event arguments of UnitsNet <see cref="IQuantity"/>.
/// </summary>
public class QuantityChangedEventArgs<TQuantity>(TQuantity oldValue, TQuantity newValue) : EventArgs where TQuantity : IQuantity
{
    public TQuantity OldValue => oldValue;
    public TQuantity NewValue => newValue;
}
