using EngineeringFort.Construction;

namespace EngineeringFort.StructuralAnalysis;

/// <summary>
/// A structure as analysis sees it (IfcStructuralAnalysisModel): a skeleton of connections and the
/// members between them, apart from any physical model. Immutable, so an edit is a new model.
/// </summary>
public record class StructuralAnalysisModel
{
    /// <summary>A member shorter than this is about its own depth: more likely a misreading than a member.</summary>
    public static Length ShortMember { get; } = Length.FromCentimeters(5);

    public virtual string Label { get; init; } = string.Empty;

    public virtual IReadOnlyList<StructuralPointConnection> Connections { get; init; } = [];

    public virtual IReadOnlyList<StructuralCurveMember> Members { get; init; } = [];

    /// <summary>
    /// What makes the model inconsistent, one line each: repeated labels, connections at one point or
    /// with no member, members with no length, ending at no connection or joining the same two, and
    /// parts standing on no support. Empty when sound. Unknowns are not problems: see <see cref="Unknowns"/>.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        foreach (var label in Repeated(Connections.Select(c => c.Label)))
            problems.Add($"Connection {label} is listed more than once.");
        foreach (var label in Repeated(Members.Select(m => m.Label)))
            problems.Add($"Member {label} is listed more than once.");

        var located = Connections.Where(c => At(c) is not null).ToList();
        for (var a = 0; a < located.Count; a++)
            for (var b = a + 1; b < located.Count; b++)
                if (Distance(At(located[a])!.Value, At(located[b])!.Value) <= Building.Tolerance.Meters)
                    problems.Add($"Connections {located[a].Label} and {located[b].Label} are at one point.");

        var connections = ByLabel();
        var joined = new List<StructuralCurveMember>();
        foreach (var member in Members)
        {
            var missing = new[] { member.Start, member.End }.Where(label => !connections.ContainsKey(label)).ToList();
            if (missing.Count > 0)
            {
                problems.Add($"Member {member.Label} ends at {string.Join(" and ", missing)}, not a connection.");
                continue;
            }
            joined.Add(member);
            if (Span(member, connections) is <= 0 || member.Start == member.End)
                problems.Add($"Member {member.Label} has no length.");
        }
        foreach (var repeated in joined.Where(m => m.Start != m.End).GroupBy(Ends).Where(group => group.Count() > 1))
            problems.Add($"Members {string.Join(", ", repeated.Select(m => m.Label))} join the same two connections.");

        var used = joined.SelectMany(m => new[] { m.Start, m.End }).ToHashSet();
        foreach (var label in connections.Keys.Where(label => !used.Contains(label)))
            problems.Add($"Connection {label} has no member.");

        var supports = Connections.Where(c => c.IsSupport).Select(c => c.Label).ToHashSet();
        foreach (var part in Parts(joined).Where(part => !part.Overlaps(supports)))
            problems.Add($"The part with connection {part.First()} stands on no support.");
        return problems;

        static (string, string) Ends(StructuralCurveMember member) =>
            string.CompareOrdinal(member.Start, member.End) < 0 ? (member.Start, member.End) : (member.End, member.Start);
    }

    /// <summary>
    /// What is sound but worth a look, one line each: members that touch with no connection between
    /// them may connect in the structure or pass by each other, and a very short member may be real.
    /// </summary>
    public IReadOnlyList<string> Warnings()
    {
        var warnings = new List<string>();
        var connections = ByLabel();
        var drawn = Members
            .Select(m => (Member: m, From: connections.GetValueOrDefault(m.Start) is { } s ? At(s) : null, To: connections.GetValueOrDefault(m.End) is { } e ? At(e) : null))
            .Where(m => m.From is not null && m.To is not null)
            .Select(m => (m.Member, From: m.From!.Value, To: m.To!.Value))
            .ToList();

        foreach (var (member, from, to) in drawn)
            if (Distance(from, to) is > 0 and var length && length < ShortMember.Meters && length > Building.Tolerance.Meters)
                warnings.Add($"Member {member.Label} is shorter than {ShortMember}.");

        for (var a = 0; a < drawn.Count; a++)
            for (var b = a + 1; b < drawn.Count; b++)
            {
                var (first, second) = (drawn[a], drawn[b]);
                if (new[] { first.Member.Start, first.Member.End }.Intersect([second.Member.Start, second.Member.End]).Any()) continue;
                if (Distance(first.From, first.To, second.From, second.To) <= Building.Tolerance.Meters)
                    warnings.Add($"Members {first.Member.Label} and {second.Member.Label} touch with no connection between them.");
            }
        return warnings;
    }

    /// <summary>What no drawing read so far has given, one line each.</summary>
    public IReadOnlyList<string> Unknowns()
    {
        var unknowns = new List<string>();
        foreach (var connection in Connections)
        {
            var missing = new[] { ("x", connection.X), ("y", connection.Y), ("z", connection.Z) }
                .Where(axis => axis.Item2 is null).Select(axis => axis.Item1).ToList();
            if (missing.Count > 0)
                unknowns.Add($"Connection {connection.Label}: {string.Join(", ", missing)}.");
        }
        if (Members.Count(m => m.Profile is null) is > 0 and var profiles)
            unknowns.Add($"{profiles} members: profile.");
        return unknowns;
    }

    /// <summary>How many separate structures the members make: more than one says nothing ties them together.</summary>
    public int PartCount() => Parts(Members).Count;

    Dictionary<string, StructuralPointConnection> ByLabel() =>
        Connections.GroupBy(c => c.Label).ToDictionary(group => group.Key, group => group.First());

    /// <summary>In metres; <see langword="null"/> when an end's place is unknown.</summary>
    static double? Span(StructuralCurveMember member, Dictionary<string, StructuralPointConnection> connections)
    {
        if (At(connections[member.Start]) is not { } from || At(connections[member.End]) is not { } to) return null;
        var distance = Distance(from, to);
        return distance <= Building.Tolerance.Meters ? 0 : distance;
    }

    static IEnumerable<string> Repeated(IEnumerable<string> labels) =>
        labels.GroupBy(label => label).Where(group => group.Count() > 1).Select(group => group.Key);

    /// <summary>The connections of each connected part of the structure.</summary>
    static List<HashSet<string>> Parts(IEnumerable<StructuralCurveMember> members)
    {
        var neighbours = new Dictionary<string, List<string>>();
        foreach (var member in members)
        {
            Neighbours(member.Start).Add(member.End);
            Neighbours(member.End).Add(member.Start);
        }

        var parts = new List<HashSet<string>>();
        var seen = new HashSet<string>();
        foreach (var start in neighbours.Keys.Where(seen.Add))
        {
            var part = new HashSet<string> { start };
            var waiting = new Stack<string>([start]);
            while (waiting.TryPop(out var label))
                foreach (var next in neighbours[label].Where(part.Add))
                {
                    seen.Add(next);
                    waiting.Push(next);
                }
            parts.Add(part);
        }
        return parts;

        List<string> Neighbours(string label)
        {
            if (!neighbours.TryGetValue(label, out var list))
                neighbours[label] = list = [];
            return list;
        }
    }

    /// <summary>A place in metres.</summary>
    readonly record struct Vector(double X, double Y, double Z)
    {
        public double Dot(Vector other) => X * other.X + Y * other.Y + Z * other.Z;

        public static Vector operator -(Vector a, Vector b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector operator +(Vector a, Vector b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector operator *(double scale, Vector a) => new(scale * a.X, scale * a.Y, scale * a.Z);
    }

    static Vector? At(StructuralPointConnection connection) =>
        connection is { X: { } x, Y: { } y, Z: { } z } ? new Vector(x.Meters, y.Meters, z.Meters) : null;

    static double Distance(Vector a, Vector b) => Sqrt((a - b).Dot(a - b));

    /// <summary>The shortest distance between segments a0–a1 and b0–b1, neither of them of no length.</summary>
    static double Distance(Vector a0, Vector a1, Vector b0, Vector b1)
    {
        var (u, v, w) = (a1 - a0, b1 - b0, a0 - b0);
        var (uu, vv, uv, uw, vw) = (u.Dot(u), v.Dot(v), u.Dot(v), u.Dot(w), v.Dot(w));
        if (uu is 0 || vv is 0) return Distance(a0, b0);
        var denominator = uu * vv - uv * uv;

        // Parallel segments are as near anywhere along them: start from a0's end.
        var s = denominator > 0 ? Clamp((uv * vw - vv * uw) / denominator, 0, 1) : 0;
        var t = (uv * s + vw) / vv;
        if (t is < 0 or > 1)
        {
            t = Clamp(t, 0, 1);
            s = Clamp((uv * t - uw) / uu, 0, 1);
        }
        return Distance(a0 + s * u, b0 + t * v);
    }
}
