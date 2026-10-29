namespace EngineeringFort.Construction;

/// <summary>A point on plan (IfcCartesianPoint), in the building's coordinates.</summary>
public readonly record struct CartesianPoint(Length X, Length Y)
{
    public static Length Distance(CartesianPoint a, CartesianPoint b) =>
        Length.FromMeters(Sqrt(Pow(b.X.Meters - a.X.Meters, 2) + Pow(b.Y.Meters - a.Y.Meters, 2)));
}
