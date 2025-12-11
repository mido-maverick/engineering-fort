namespace EngineeringFort;

public interface ICheck
{
    IEnumerable<ICheck> SubChecks { get; }
    bool IsValid { get; }
    string CheckStatus { get; }
}

public abstract record class Check : ICheck
{
    public abstract IEnumerable<ICheck> SubChecks { get; }
    public virtual bool IsValid => SubChecks.All(sc => sc.IsValid);
    public virtual string CheckStatus => IsValid ? "<<OK>>" : "--NG--";
}
