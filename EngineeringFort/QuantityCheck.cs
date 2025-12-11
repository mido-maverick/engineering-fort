using System.Numerics;

namespace EngineeringFort;

public record class QuantityCheck<TQuantity> : Check
    where TQuantity : IQuantity, IComparisonOperators<TQuantity, TQuantity, bool>, new()
{
    public virtual TQuantity Value { get; set; } = new();
    public virtual TQuantity Limit { get; set; } = new();
    public override bool IsValid => Value < Limit;
    public override IEnumerable<ICheck> SubChecks => [];
}
