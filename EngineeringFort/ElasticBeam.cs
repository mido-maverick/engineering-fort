namespace EngineeringFort;

/// <summary>
/// A straight beam of one section on vertical supports, with the loads it carries.
/// <see cref="Analyze"/> turns it into reactions, shear, moment, rotation and deflection.
/// </summary>
/// <remarks>
/// <para>
/// Linear-elastic Euler–Bernoulli bending. The stiffness method finds the displacements at the
/// nodes, which it gets exactly; between nodes the load is integrated into polynomials, so every
/// result is exact too and nothing is sampled until something is drawn. The analysis runs in SI.
/// </para>
/// <para>
/// Positions run from the left end. Loads and deflections are positive downward, reactions positive
/// upward, and reaction moments positive counter-clockwise. Shear is the net upward force left of a
/// section and moment is positive in sagging, so that dM/dx = V.
/// </para>
/// </remarks>
public sealed record ElasticBeam(
    Length Length,
    Pressure ElasticModulus,
    AreaMomentOfInertia MomentOfInertia,
    IReadOnlyList<ElasticBeam.Support> Supports,
    IReadOnlyList<ElasticBeam.PointLoad> PointLoads,
    IReadOnlyList<ElasticBeam.DistributedLoad> DistributedLoads)
{
    /// <summary>
    /// How near two positions may be, as a fraction of <see cref="Length"/>, and still be one node, so
    /// a load typed a hair off a support does not make an element of vanishing length, and supports
    /// typed at one place act as one.
    /// </summary>
    const double CoincidenceTolerance = 1e-9;

    /// <summary>
    /// The smallest pivot, as a fraction of the largest stiffness term, that elimination accepts. Only
    /// a beam that <see cref="Problem"/> should have caught comes near it.
    /// </summary>
    const double SingularityTolerance = 1e-12;

    /// <summary>Every load added up, point and distributed alike; the reactions must add up to the same.</summary>
    public Force TotalLoad => Force.FromNewtons(
        PointLoads.Sum(load => load.Force.Newtons) + DistributedLoads.Sum(load => load.Resultant.Newtons));

    /// <summary>What stops the beam being analysed, or <see langword="null"/> when nothing does.</summary>
    public ProblemKind? Problem
    {
        get
        {
            if (!(Span > 0 && ElasticModulus.Pascals > 0 && MomentOfInertia.MetersToTheFourth > 0) ||
                !Supports.All(support => OnBeam(support.Position)) ||
                !PointLoads.All(load => OnBeam(load.Position)) ||
                !DistributedLoads.All(load => OnBeam(load.Start) && OnBeam(load.End)))
                return ProblemKind.InvalidInput;

            if (Merge(Supports.Select(support => support.Position.Meters)).Count < 2 &&
                !Supports.Any(support => support.Kind is SupportKind.Fixed))
                return ProblemKind.Unstable;

            return null;
        }
    }

    double Span => Length.Meters;

    double Rigidity => ElasticModulus.Pascals * MomentOfInertia.MetersToTheFourth;

    double Tolerance => Span * CoincidenceTolerance;

    bool OnBeam(Length position) => position.Meters >= -Tolerance && position.Meters <= Span + Tolerance;

    /// <exception cref="InvalidOperationException">When there is a <see cref="Problem"/>.</exception>
    public Response Analyze()
    {
        if (Problem is { } problem)
            throw new InvalidOperationException($"The beam cannot be analysed: {problem}.");

        var lines = DistributedLoads
            .Select(load => load.LeftToRight)
            .Where(load => load.End.Meters - load.Start.Meters > Tolerance)
            .ToArray();
        var nodes = Nodes(lines);
        var lineLoads = new Polynomial[nodes.Length - 1];
        var loads = new double[2 * nodes.Length];
        var stiffness = new double[loads.Length, loads.Length];

        for (var element = 0; element < lineLoads.Length; element++)
        {
            lineLoads[element] = LineLoad(lines, nodes[element], nodes[element + 1]);
            Assemble(stiffness, loads, 2 * element, nodes[element + 1] - nodes[element], lineLoads[element], Rigidity);
        }

        var pointForces = new double[nodes.Length];
        foreach (var load in PointLoads)
            pointForces[NodeAt(nodes, load.Position.Meters)] += load.Force.Newtons;
        for (var node = 0; node < nodes.Length; node++)
            loads[2 * node] += pointForces[node];

        var restrained = new bool[loads.Length];
        foreach (var support in Supports)
        {
            var node = NodeAt(nodes, support.Position.Meters);
            restrained[2 * node] = true;
            restrained[2 * node + 1] |= support.Kind is SupportKind.Fixed;
        }

        var displacements = Solve(stiffness, loads, restrained);

        // What each support exerts on the beam along its degree of freedom: downward, and clockwise
        // for a rotation, since the rotation is dδ/dx with δ downward.
        var supportForces = new double[loads.Length];
        for (var row = 0; row < loads.Length; row++)
        {
            if (!restrained[row]) continue;

            var internalForce = 0d;
            for (var column = 0; column < loads.Length; column++)
                internalForce += stiffness[row, column] * displacements[column];
            supportForces[row] = internalForce - loads[row];
        }

        var segments = new Segment[lineLoads.Length];
        var (shear, moment) = (0d, 0d);
        for (var element = 0; element < segments.Length; element++)
        {
            // Crossing a node, its reaction pushes the shear up and its point load down, and a
            // counter-clockwise reaction moment takes as much off the sagging moment.
            shear -= supportForces[2 * element] + pointForces[element];
            moment += supportForces[2 * element + 1];

            var (start, end) = (nodes[element], nodes[element + 1]);
            var shearLine = shear - lineLoads[element].Integral();
            var momentLine = moment + shearLine.Integral();
            var rotation = displacements[2 * element + 1] - momentLine.Integral() / Rigidity;
            var deflection = displacements[2 * element] + rotation.Integral();
            segments[element] = new(Length.FromMeters(start), Length.FromMeters(end), shearLine, momentLine, rotation, deflection);

            shear = shearLine.Evaluate(end - start);
            moment = momentLine.Evaluate(end - start);
        }

        return new(segments, [.. Enumerable.Range(0, nodes.Length)
            .Where(node => restrained[2 * node])
            .Select(node => new Reaction(
                Length.FromMeters(nodes[node]),
                Force.FromNewtons(-supportForces[2 * node]),
                Torque.FromNewtonMeters(restrained[2 * node + 1] ? -supportForces[2 * node + 1] : 0)))]);
    }

    /// <summary>
    /// Both ends and every position anything stands at, in metres and in order, with near-coincident
    /// ones merged — so each element carries no point load inside it and is either wholly under a
    /// distributed load or wholly clear of it.
    /// </summary>
    double[] Nodes(DistributedLoad[] lines)
    {
        var nodes = Merge(
        [
            0,
            Span,
            .. Supports.Select(support => support.Position.Meters),
            .. PointLoads.Select(load => load.Position.Meters),
            .. lines.SelectMany(load => new[] { load.Start.Meters, load.End.Meters }),
        ]);

        nodes[0] = 0;
        nodes[^1] = Span;
        return [.. nodes];
    }

    /// <summary><paramref name="positions"/> in order, dropping each within <see cref="Tolerance"/> of the one kept before it.</summary>
    List<double> Merge(IEnumerable<double> positions)
    {
        var merged = new List<double>();
        foreach (var position in positions.Order())
            if (merged.Count is 0 || position - merged[^1] > Tolerance)
                merged.Add(position);
        return merged;
    }

    /// <summary>The node nearest <paramref name="position"/>; <see cref="Nodes"/> put one within tolerance of it.</summary>
    static int NodeAt(double[] nodes, double position)
    {
        var index = Array.BinarySearch(nodes, position);
        if (index >= 0) return index;

        index = ~index;
        return index == nodes.Length || index > 0 && position - nodes[index - 1] < nodes[index] - position
            ? index - 1
            : index;
    }

    /// <summary>The distributed load on one element, in N/m, as a polynomial in the distance from its start.</summary>
    Polynomial LineLoad(DistributedLoad[] lines, double start, double end) =>
        lines
            .Where(load => load.Start.Meters - Tolerance <= start && end <= load.End.Meters + Tolerance)
            .Aggregate(Polynomial.Zero, (sum, load) =>
            {
                var gradient = (load.EndIntensity.NewtonsPerMeter - load.StartIntensity.NewtonsPerMeter) /
                               (load.End.Meters - load.Start.Meters);
                var intensity = load.StartIntensity.NewtonsPerMeter + gradient * (start - load.Start.Meters);
                return sum + new Polynomial(intensity, gradient);
            });

    /// <summary>
    /// Adds one element's stiffness, and its line load as the nodal forces doing the same work through
    /// the cubic shape functions — for a beam, exactly its fixed-end reactions reversed.
    /// </summary>
    static void Assemble(
        double[,] stiffness, double[] loads, int first, double length, Polynomial lineLoad, double rigidity)
    {
        var l = length;
        var k = rigidity / (l * l * l);
        double[,] element =
        {
            { 12 * k, 6 * l * k, -12 * k, 6 * l * k },
            { 6 * l * k, 4 * l * l * k, -6 * l * k, 2 * l * l * k },
            { -12 * k, -6 * l * k, 12 * k, -6 * l * k },
            { 6 * l * k, 2 * l * l * k, -6 * l * k, 4 * l * l * k },
        };
        Polynomial[] shapes =
        [
            new(1, 0, -3 / (l * l), 2 / (l * l * l)),
            new(0, 1, -2 / l, 1 / (l * l)),
            new(0, 0, 3 / (l * l), -2 / (l * l * l)),
            new(0, 0, -1 / l, 1 / (l * l)),
        ];

        for (var row = 0; row < 4; row++)
        {
            loads[first + row] += (lineLoad * shapes[row]).Integral().Evaluate(l);
            for (var column = 0; column < 4; column++)
                stiffness[first + row, first + column] += element[row, column];
        }
    }

    /// <summary>The displacements, with the restrained ones held at zero.</summary>
    static double[] Solve(double[,] stiffness, double[] loads, bool[] restrained)
    {
        var free = Enumerable.Range(0, loads.Length).Where(degree => !restrained[degree]).ToArray();
        var matrix = new double[free.Length, free.Length];
        var right = new double[free.Length];
        for (var row = 0; row < free.Length; row++)
        {
            right[row] = loads[free[row]];
            for (var column = 0; column < free.Length; column++)
                matrix[row, column] = stiffness[free[row], free[column]];
        }

        var solution = GaussianElimination(matrix, right);
        var displacements = new double[loads.Length];
        for (var i = 0; i < free.Length; i++)
            displacements[free[i]] = solution[i];
        return displacements;
    }

    static double[] GaussianElimination(double[,] matrix, double[] right)
    {
        var count = right.Length;
        var largest = 0d;
        for (var i = 0; i < count; i++)
            largest = Max(largest, Abs(matrix[i, i]));

        for (var pivot = 0; pivot < count; pivot++)
        {
            var best = pivot;
            for (var row = pivot + 1; row < count; row++)
                if (Abs(matrix[row, pivot]) > Abs(matrix[best, pivot]))
                    best = row;

            if (Abs(matrix[best, pivot]) <= largest * SingularityTolerance)
                throw new InvalidOperationException($"The beam cannot be analysed: {ProblemKind.Unstable}.");

            if (best != pivot)
            {
                for (var column = 0; column < count; column++)
                    (matrix[pivot, column], matrix[best, column]) = (matrix[best, column], matrix[pivot, column]);
                (right[pivot], right[best]) = (right[best], right[pivot]);
            }

            for (var row = pivot + 1; row < count; row++)
            {
                var factor = matrix[row, pivot] / matrix[pivot, pivot];
                if (factor == 0) continue;

                for (var column = pivot; column < count; column++)
                    matrix[row, column] -= factor * matrix[pivot, column];
                right[row] -= factor * right[pivot];
            }
        }

        var solution = new double[count];
        for (var row = count - 1; row >= 0; row--)
        {
            var sum = right[row];
            for (var column = row + 1; column < count; column++)
                sum -= matrix[row, column] * solution[column];
            solution[row] = sum / matrix[row, row];
        }
        return solution;
    }

    /// <summary>What stops an <see cref="ElasticBeam"/> being analysed.</summary>
    public enum ProblemKind
    {
        /// <summary>A length, elastic modulus or moment of inertia not above zero, or a support or load off the beam.</summary>
        InvalidInput,

        /// <summary>Fewer than two supported positions and no fixed support, so the beam is free to move.</summary>
        Unstable,
    }

    public enum SupportKind
    {
        /// <summary>Holds the beam up and lets it turn; a pin and a roller alike, since only vertical load is carried.</summary>
        [Display(Name = nameof(DisplayStrings.SimpleSupport), ResourceType = typeof(DisplayStrings))]
        Simple,

        /// <summary>Holds both its deflection and its rotation.</summary>
        [Display(Name = nameof(DisplayStrings.FixedSupport), ResourceType = typeof(DisplayStrings))]
        Fixed,
    }

    public sealed record Support(Length Position, SupportKind Kind = SupportKind.Simple);

    /// <param name="Force">Positive downward.</param>
    public sealed record PointLoad(Length Position, Force Force);

    /// <summary>
    /// A load per length, varying linearly from <see cref="StartIntensity"/> at <see cref="Start"/> to
    /// <see cref="EndIntensity"/> at <see cref="End"/>; positive downward. Either end may be the left one.
    /// </summary>
    public sealed record DistributedLoad(Length Start, Length End, ForcePerLength StartIntensity, ForcePerLength EndIntensity)
    {
        /// <summary>A uniform load.</summary>
        public DistributedLoad(Length start, Length end, ForcePerLength intensity)
            : this(start, end, intensity, intensity) { }

        /// <summary>The same load, given from its left end.</summary>
        public DistributedLoad LeftToRight =>
            Start.Meters <= End.Meters ? this : new(End, Start, EndIntensity, StartIntensity);

        public Force Resultant => Force.FromNewtons(
            (StartIntensity.NewtonsPerMeter + EndIntensity.NewtonsPerMeter) / 2 * Abs(End.Meters - Start.Meters));

        /// <summary>The intensity at <paramref name="position"/>, on the straight line through both ends.</summary>
        public ForcePerLength IntensityAt(Length position)
        {
            var width = End.Meters - Start.Meters;
            if (width == 0) return StartIntensity;

            var fraction = (position.Meters - Start.Meters) / width;
            return ForcePerLength.FromNewtonsPerMeter(
                StartIntensity.NewtonsPerMeter + (EndIntensity.NewtonsPerMeter - StartIntensity.NewtonsPerMeter) * fraction);
        }
    }

    /// <summary>What one supported position exerts on the beam; supports typed at one place count once.</summary>
    /// <param name="Force">Positive upward.</param>
    /// <param name="Moment">Positive counter-clockwise; zero unless the support is fixed.</param>
    public sealed record Reaction(Length Position, Force Force, Torque Moment);

    /// <summary>A value a response takes, and where along the beam.</summary>
    /// <remarks>
    /// A struct, unlike its neighbours, so that <c>MaxBy</c> and <c>MinBy</c> hand it back non-nullable
    /// and throw on no segments rather than return <see langword="null"/>.
    /// </remarks>
    public readonly record struct Extremum<TValue>(Length Position, TValue Value)
    {
        public Extremum<TResult> Select<TResult>(Func<TValue, TResult> convert) => new(Position, convert(Value));
    }

    /// <summary>
    /// The response between two adjacent nodes. Each polynomial takes the distance from
    /// <see cref="Start"/> in metres, not from the left end of the beam, and gives SI values: N for
    /// <see cref="Shear"/>, N·m for <see cref="Moment"/>, radians for <see cref="Rotation"/> and metres
    /// for <see cref="Deflection"/>.
    /// </summary>
    /// <param name="Rotation">
    /// dδ/dx under the small-rotation assumption the analysis rests on; positive clockwise, where the beam
    /// falls to the right.
    /// </param>
    public sealed record Segment(
        Length Start, Length End, Polynomial Shear, Polynomial Moment, Polynomial Rotation, Polynomial Deflection)
    {
        public Length Length => End - Start;
    }

    /// <summary>What <see cref="ElasticBeam.Analyze"/> found.</summary>
    /// <param name="Segments">One per element, left to right.</param>
    /// <param name="Reactions">One per supported position, left to right.</param>
    public sealed record Response(IReadOnlyList<Segment> Segments, IReadOnlyList<Reaction> Reactions)
    {
        /// <summary>The shear of greatest magnitude, sign kept.</summary>
        public Extremum<Force> PeakShear => Peak(segment => segment.Shear).Select(value => Force.FromNewtons(value));

        /// <summary>The greatest sagging moment; not positive where the beam only hogs.</summary>
        public Extremum<Torque> MaxMoment => Maximum(segment => segment.Moment).Select(value => Torque.FromNewtonMeters(value));

        /// <summary>The greatest hogging moment, as a negative value; not negative where the beam only sags.</summary>
        public Extremum<Torque> MinMoment => Minimum(segment => segment.Moment).Select(value => Torque.FromNewtonMeters(value));

        /// <summary>The moment of greatest magnitude, sign kept — what the section modulus has to carry.</summary>
        public Extremum<Torque> PeakMoment => Peak(segment => segment.Moment).Select(value => Torque.FromNewtonMeters(value));

        /// <summary>The rotation of greatest magnitude, sign kept.</summary>
        public Extremum<Angle> PeakRotation => Peak(segment => segment.Rotation).Select(value => Angle.FromRadians(value));

        /// <summary>The deflection of greatest magnitude, sign kept.</summary>
        public Extremum<Length> PeakDeflection => Peak(segment => segment.Deflection).Select(value => Length.FromMeters(value));

        public Force TotalReaction => Force.FromNewtons(Reactions.Sum(reaction => reaction.Force.Newtons));

        /// <summary>The greatest value <paramref name="response"/> takes anywhere along the beam, in SI.</summary>
        public Extremum<double> Maximum(Func<Segment, Polynomial> response) =>
            Segments.Select(segment => Along(segment, response(segment).Maximum(0, segment.Length.Meters)))
                .MaxBy(extremum => extremum.Value);

        /// <summary>The least value <paramref name="response"/> takes anywhere along the beam, in SI.</summary>
        public Extremum<double> Minimum(Func<Segment, Polynomial> response) =>
            Segments.Select(segment => Along(segment, response(segment).Minimum(0, segment.Length.Meters)))
                .MinBy(extremum => extremum.Value);

        /// <summary>The value of greatest magnitude <paramref name="response"/> takes, sign kept, in SI.</summary>
        public Extremum<double> Peak(Func<Segment, Polynomial> response)
        {
            var (maximum, minimum) = (Maximum(response), Minimum(response));
            return -minimum.Value > maximum.Value ? minimum : maximum;
        }

        static Extremum<double> Along(Segment segment, (double At, double Value) local) =>
            new(Length.FromMeters(segment.Start.Meters + local.At), local.Value);
    }
}
