namespace EngineeringFort;

/// <summary>
/// A polynomial with real coefficients, lowest power first: enough algebra to carry a beam's load
/// through shear, moment, slope and deflection exactly, rather than by sampling.
/// </summary>
/// <param name="coefficients">The constant term first, then those of x, x², and so on.</param>
public sealed class Polynomial(params double[] coefficients)
{
    /// <summary>
    /// How many equal steps <see cref="Maximum"/> and <see cref="Minimum"/> bracket stationary points
    /// with. The ends of every step are candidates too, so two stationary points closer than one step
    /// can cost no more than the difference between two nearly equal values.
    /// </summary>
    const int SearchSteps = 32;

    /// <summary>Halvings of a bracket; enough to reach round-off from any bracket a beam produces.</summary>
    const int BisectionSteps = 64;

    readonly double[] _coefficients = [.. coefficients];

    public static Polynomial Zero { get; } = new();

    public IReadOnlyList<double> Coefficients => _coefficients;

    /// <summary>The highest power with a nonzero coefficient; zero for a constant, zero itself included.</summary>
    public int Degree => Max(0, Array.FindLastIndex(_coefficients, coefficient => coefficient != 0));

    public double Evaluate(double x)
    {
        var value = 0d;
        for (var power = _coefficients.Length - 1; power >= 0; power--)
            value = value * x + _coefficients[power];
        return value;
    }

    public Polynomial Derivative() =>
        new([.. _coefficients.Skip(1).Select((coefficient, power) => coefficient * (power + 1))]);

    /// <summary>The antiderivative that is zero at x = 0.</summary>
    public Polynomial Integral() =>
        _coefficients.Length is 0 ? Zero : new([0, .. _coefficients.Select((coefficient, power) => coefficient / (power + 1))]);

    /// <summary>The greatest value on [<paramref name="start"/>, <paramref name="end"/>], and where it is.</summary>
    public (double At, double Value) Maximum(double start, double end) =>
        Candidates(start, end).Select(x => (x, Evaluate(x))).MaxBy(candidate => candidate.Item2);

    /// <summary>The least value on [<paramref name="start"/>, <paramref name="end"/>], and where it is.</summary>
    public (double At, double Value) Minimum(double start, double end) =>
        Candidates(start, end).Select(x => (x, Evaluate(x))).MinBy(candidate => candidate.Item2);

    /// <summary>The ends, the step ends, and every stationary point a step brackets.</summary>
    IEnumerable<double> Candidates(double start, double end)
    {
        yield return start;
        yield return end;
        if (Degree < 2) yield break;

        var slope = Derivative();
        var (left, leftSlope) = (start, slope.Evaluate(start));
        for (var step = 1; step <= SearchSteps; step++)
        {
            var right = step == SearchSteps ? end : start + (end - start) * step / SearchSteps;
            var rightSlope = slope.Evaluate(right);
            yield return right;

            if (Sign(leftSlope) * Sign(rightSlope) < 0)
                yield return Root(slope, left, right, leftSlope);

            (left, leftSlope) = (right, rightSlope);
        }
    }

    /// <summary>Bisects a bracket whose ends <paramref name="function"/> gives opposite signs.</summary>
    static double Root(Polynomial function, double left, double right, double leftValue)
    {
        for (var step = 0; step < BisectionSteps; step++)
        {
            var middle = (left + right) / 2;
            if (middle <= left || middle >= right) break;

            var value = function.Evaluate(middle);
            if (Sign(value) == Sign(leftValue))
                (left, leftValue) = (middle, value);
            else
                right = middle;
        }
        return (left + right) / 2;
    }

    public static implicit operator Polynomial(double constant) => new(constant);

    public static Polynomial operator +(Polynomial left, Polynomial right)
    {
        var sum = new double[Max(left._coefficients.Length, right._coefficients.Length)];
        for (var power = 0; power < sum.Length; power++)
            sum[power] = left._coefficients.ElementAtOrDefault(power) + right._coefficients.ElementAtOrDefault(power);
        return new(sum);
    }

    public static Polynomial operator -(Polynomial polynomial) =>
        new([.. polynomial._coefficients.Select(coefficient => -coefficient)]);

    public static Polynomial operator -(Polynomial left, Polynomial right) => left + -right;

    public static Polynomial operator *(Polynomial left, Polynomial right)
    {
        if (left._coefficients.Length is 0 || right._coefficients.Length is 0) return Zero;

        var product = new double[left._coefficients.Length + right._coefficients.Length - 1];
        for (var i = 0; i < left._coefficients.Length; i++)
            for (var j = 0; j < right._coefficients.Length; j++)
                product[i + j] += left._coefficients[i] * right._coefficients[j];
        return new(product);
    }

    public static Polynomial operator /(Polynomial polynomial, double divisor) =>
        new([.. polynomial._coefficients.Select(coefficient => coefficient / divisor)]);
}
