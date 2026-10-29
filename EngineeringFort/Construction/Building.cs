namespace EngineeringFort.Construction;

/// <summary>
/// A building (IfcBuilding): its storeys, the grids its elements are set out from, and the types that
/// carry their sizes.
/// </summary>
public record class Building : ILabelable
{
    /// <summary>How far apart two points may be and still count as one.</summary>
    public static Length Tolerance { get; } = Length.FromMillimeters(1);

    /// <summary>Two axes whose directions differ by less than this sine count as parallel: they never cross.</summary>
    const double ParallelSine = 1e-9;

    public virtual string Label { get; set; } = string.Empty;

    /// <summary>Its storeys, bottom to top, so one whose elevation is unknown still has its place.</summary>
    public virtual IReadOnlyList<Storey> Floors { get; set; } = [];

    public virtual IReadOnlyList<Grid> Grids { get; set; } = [];

    public virtual IReadOnlyList<ColumnType> ColumnTypes { get; set; } = [];

    public virtual IReadOnlyList<WallType> WallTypes { get; set; } = [];

    public virtual IReadOnlyList<BeamType> BeamTypes { get; set; } = [];

    public virtual IReadOnlyList<SlabType> SlabTypes { get; set; } = [];

    public virtual IReadOnlyList<OpeningType> OpeningTypes { get; set; } = [];

    /// <summary>Where a placement is in the building's coordinates, or why it can't be worked out.</summary>
    public (CartesianPoint? Point, string? Problem) Locate(Placement placement)
    {
        if (placement.U is null && placement.V is null)
            return (new(placement.DX, placement.DY), null);
        if (placement.U is not { } u || placement.V is not { } v)
            return (null, "names one grid axis, not two");

        var grids = Grids
            .Select(grid => (U: grid.UAxes.Where(a => a.Axis == u).ToList(), V: grid.VAxes.Where(a => a.Axis == v).ToList()))
            .Where(grid => grid.U.Count > 0 && grid.V.Count > 0)
            .ToList();
        if (grids.Count is 0)
            return (null, $"no grid has U axis {u.Name} and V axis {v.Name}");
        if (grids is not [{ U: [var uAxis], V: [var vAxis] }])
            return (null, $"axes {u.Name} and {v.Name} are listed more than once");
        if (Intersection(uAxis, vAxis) is not { } at)
            return (null, $"axes {u.Name} and {v.Name} never cross");
        return (new(at.X + placement.DX, at.Y + placement.DY), null);
    }

    /// <summary>
    /// What makes the model inconsistent, one line each: repeated labels and elements, placements that
    /// can't be located, types that aren't defined, sizes that are not positive, elements with no
    /// length, openings outside their wall. Empty when sound. Elements are named by their place in their
    /// storey's list, from 1. Unknown sizes are not problems: see <see cref="Unknowns"/>.
    /// </summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();
        foreach (var label in Repeated(Floors.Select(f => f.Label)))
            problems.Add($"Storey {label} is listed more than once.");
        foreach (var grid in Grids)
        {
            var axes = grid.UAxes.Concat(grid.VAxes).ToList();
            foreach (var tag in Repeated(axes.Select(a => a.Axis.Name)))
                problems.Add($"Grid {grid.Label}: axis {tag} is listed more than once.");
            foreach (var axis in axes.Where(a => Short(a.Start, a.End)))
                problems.Add($"Grid {grid.Label}: axis {axis.Axis.Name} has no length.");
        }

        CheckTypes(problems, "column type", ColumnTypes, t => [t.XDim, t.YDim, t.Diameter]);
        CheckTypes(problems, "wall type", WallTypes, t => [t.Thickness]);
        CheckTypes(problems, "beam type", BeamTypes, t => [t.Width, t.Depth]);
        CheckTypes(problems, "slab type", SlabTypes, t => [t.Thickness]);
        CheckTypes(problems, "opening type", OpeningTypes, t => [t.Width, t.Height]);
        foreach (var type in ColumnTypes.Where(t => t.Diameter is not null && (t.XDim is not null || t.YDim is not null)))
            problems.Add($"The column type {type.Label}: has both a diameter and sides.");

        var catalogue = new Catalogue(ByLabel(ColumnTypes), ByLabel(WallTypes), ByLabel(BeamTypes), ByLabel(SlabTypes), ByLabel(OpeningTypes));
        Storey? below = null;
        for (var i = 0; i < Floors.Count; i++)
        {
            var storey = Floors[i];
            if (storey.Elevation is not null)
            {
                if (storey.Elevation - below?.Elevation <= Tolerance)
                    problems.Add($"Storey {storey.Label} is not above {below!.Label}.");
                below = storey;
            }
            Length? height = i + 1 < Floors.Count ? Floors[i + 1].Elevation - storey.Elevation : null;
            Check(storey, height, catalogue, problem => problems.Add($"{storey.Label} {problem}"));
        }
        return problems;
    }

    /// <summary>Sizes no drawing read so far has given, one line each.</summary>
    public IReadOnlyList<string> Unknowns()
    {
        var unknowns = new List<string>();
        foreach (var t in ColumnTypes.Where(t => t.Diameter is null && (t.XDim is null || t.YDim is null)))
            unknowns.Add($"Column type {t.Label}: section.");
        foreach (var t in WallTypes.Where(t => t.Thickness is null))
            unknowns.Add($"Wall type {t.Label}: thickness.");
        foreach (var t in BeamTypes.Where(t => t.Width is null || t.Depth is null))
            unknowns.Add($"Beam type {t.Label}: {Missing(("width", t.Width), ("depth", t.Depth))}.");
        foreach (var t in SlabTypes.Where(t => t.Thickness is null))
            unknowns.Add($"Slab type {t.Label}: thickness.");
        foreach (var t in OpeningTypes.Where(t => t.Width is null || t.Height is null))
            unknowns.Add($"Opening type {t.Label}: {Missing(("width", t.Width), ("height", t.Height))}.");
        foreach (var storey in Floors.Where(f => f.Elevation is null))
            unknowns.Add($"Storey {storey.Label}: elevation.");
        var sills = Floors.Sum(f => f.Walls.Sum(w => w.Openings.Count(o => o.SillHeight is null)));
        if (sills > 0)
            unknowns.Add($"{sills} wall openings: sill height.");
        // The roof is a floor too: list it, empty, and the floor below gets its height.
        if (Floors.LastOrDefault() is { } top &&
            (top.Columns.Count > 0 || top.Walls.Any(w => w.Height is null)))
            unknowns.Add($"{top.Label}: no floor above, so its columns' and walls' height.");
        return unknowns;
    }

    /// <summary>The types by label, looked up once for every storey.</summary>
    sealed record Catalogue(
        IReadOnlyDictionary<string, ColumnType> Columns,
        IReadOnlyDictionary<string, WallType> Walls,
        IReadOnlyDictionary<string, BeamType> Beams,
        IReadOnlyDictionary<string, SlabType> Slabs,
        IReadOnlyDictionary<string, OpeningType> Openings);

    /// <param name="height">Up to the next floor; <see langword="null"/> on the top one or where an elevation is unknown.</param>
    void Check(Storey storey, Length? height, Catalogue types, Action<string> report)
    {
        CartesianPoint? At(Placement placement, string name)
        {
            var (point, problem) = Locate(placement);
            if (problem is not null)
                report($"{name}: {problem}.");
            return point;
        }

        var columns = new List<CartesianPoint?>();
        for (var i = 0; i < storey.Columns.Count; i++)
        {
            var name = Name("column", storey.Columns[i], i);
            Typed(storey.Columns[i], types.Columns, name, report);
            columns.Add(At(storey.Columns[i].Position, name));
        }
        foreach (var (a, b) in Twins(columns, (p, q) => p is { } x && q is { } y && Short(x, y)))
            report($"{Name("column", storey.Columns[a], a)} and #{b + 1} stand at the same place.");

        var walls = new List<(CartesianPoint?, CartesianPoint?)>();
        for (var i = 0; i < storey.Walls.Count; i++)
        {
            var wall = storey.Walls[i];
            var name = Name("wall", wall, i);
            Typed(wall, types.Walls, name, report);
            var (start, end) = (At(wall.Start, name), At(wall.End, name));
            walls.Add((start, end));
            Length? length = start is { } s && end is { } e ? CartesianPoint.Distance(s, e) : null;
            if (wall.Height <= Length.Zero)
                report($"{name}: has no height.");
            // Every opening would run past the ends of a wall with none: one problem, said once.
            if (length <= Tolerance)
                report($"{name}: has no length.");
            else
                Check(wall, length, wall.Height ?? height, types.Openings, problem => report($"{name} {problem}"));
        }
        foreach (var (a, b) in Twins(walls, SameLine))
            report($"{Name("wall", storey.Walls[a], a)} and #{b + 1} run between the same points.");

        var beams = new List<(CartesianPoint?, CartesianPoint?)>();
        for (var i = 0; i < storey.Beams.Count; i++)
        {
            var name = Name("beam", storey.Beams[i], i);
            Typed(storey.Beams[i], types.Beams, name, report);
            var (start, end) = (At(storey.Beams[i].Start, name), At(storey.Beams[i].End, name));
            beams.Add((start, end));
            if (start is { } s && end is { } e && Short(s, e))
                report($"{name}: has no length.");
        }
        foreach (var (a, b) in Twins(beams, SameLine))
            report($"{Name("beam", storey.Beams[a], a)} and #{b + 1} run between the same points.");

        // Its corners when they all locate and enclose something; null otherwise.
        IReadOnlyList<CartesianPoint>? Enclosing(IReadOnlyList<Placement> boundary, string name)
        {
            var corners = boundary.Select(p => At(p, name)).ToList();
            List<CartesianPoint>? located = corners.All(p => p is not null) ? [.. corners.Select(p => p!.Value)] : null;
            if (corners.Count < 3 || located is not null && Abs(Area(located)) <= Pow(Tolerance.Meters, 2))
            {
                report($"{name}: its boundary encloses nothing.");
                return null;
            }
            if (located is not null && CrossesItself(located))
            {
                report($"{name}: its boundary crosses itself.");
                return null;
            }
            return located;
        }

        for (var i = 0; i < storey.Slabs.Count; i++)
        {
            var slab = storey.Slabs[i];
            var name = Name("slab", slab, i);
            Typed(slab, types.Slabs, name, report);
            var outline = Enclosing(slab.Boundary, name);
            var voids = new List<IReadOnlyList<CartesianPoint>?>();
            for (var j = 0; j < slab.Openings.Count; j++)
            {
                var opening = $"{name} {Name("opening", slab.Openings[j], j)}";
                // Its outline is its own: a type is optional.
                if (slab.Openings[j].Type.Length > 0)
                    Typed(slab.Openings[j], types.Openings, opening, report);
                var corners = Enclosing(slab.Openings[j].Boundary, opening);
                if (outline is not null && corners is not null && !Within(corners, outline))
                    report($"{opening}: runs outside the slab.");
                voids.Add(corners);
            }
            foreach (var (a, b) in Twins(voids, (p, q) => p is not null && q is not null && Overlap(p, q)))
                report($"{name} {Name("opening", slab.Openings[a], a)} and #{b + 1} overlap.");
        }
    }

    /// <param name="length">The wall's; <see langword="null"/> when its ends can't be located.</param>
    /// <param name="top">The wall's height; <see langword="null"/> when unknown.</param>
    static void Check(Wall wall, Length? length, Length? top, IReadOnlyDictionary<string, OpeningType> types, Action<string> report)
    {
        var spans = new List<(Length From, Length? To, Length? Bottom, Length? Top)>();
        for (var i = 0; i < wall.Openings.Count; i++)
        {
            var opening = wall.Openings[i];
            var name = Name("opening", opening, i);
            Typed(opening, types, name, report);
            var type = types.GetValueOrDefault(opening.Type);
            var right = opening.Offset + type?.Width;
            var head = opening.SillHeight + type?.Height;
            spans.Add((opening.Offset, right, opening.SillHeight, head));
            if (opening.Offset < -Tolerance || right > length + Tolerance)
                report($"{name}: runs past the wall's ends.");
            if (opening.SillHeight < -Tolerance)
                report($"{name}: starts below the floor.");
            if (head > top + Tolerance)
                report($"{name}: runs above the wall.");
        }
        // Unknown heights may overlap: only openings known to be one above the other are apart.
        foreach (var (a, b) in Twins(spans, (x, y) =>
            x.To > y.From + Tolerance && y.To > x.From + Tolerance &&
            !(x.Top <= y.Bottom + Tolerance) && !(y.Top <= x.Bottom + Tolerance)))
            report($"{Name("opening", wall.Openings[a], a)} and #{b + 1} overlap.");
    }

    static void CheckTypes<T>(List<string> problems, string kind, IReadOnlyList<T> types, Func<T, Length?[]> sizes) where T : ElementType
    {
        foreach (var label in Repeated(types.Select(t => t.Label)))
            problems.Add($"The {kind} {label} is defined more than once.");
        foreach (var type in types.Where(t => t.Label.Length is 0))
            problems.Add($"A {kind} has no label.");
        foreach (var type in types.Where(t => sizes(t).Any(size => size <= Length.Zero)))
            problems.Add($"The {kind} {type.Label}: a size is not positive.");
    }

    static void Typed<T>(BuildingElement element, IReadOnlyDictionary<string, T> types, string name, Action<string> report)
    {
        if (element.Type.Length is 0)
            report($"{name}: has no type.");
        else if (!types.ContainsKey(element.Type))
            report($"{name}: its type {element.Type} is not defined.");
    }

    /// <summary>The names of the sizes left out, e.g. "width and depth".</summary>
    static string Missing(params (string Name, Length? Size)[] sizes) =>
        string.Join(" and ", sizes.Where(s => s.Size is null).Select(s => s.Name));

    static Dictionary<string, T> ByLabel<T>(IReadOnlyList<T> types) where T : ElementType =>
        types.GroupBy(t => t.Label).ToDictionary(g => g.Key, g => g.First());

    static string Name(string kind, BuildingElement element, int index) =>
        element.Type.Length > 0 ? $"{kind} #{index + 1} ({element.Type})" : $"{kind} #{index + 1}";

    static IEnumerable<string> Repeated(IEnumerable<string> labels) =>
        labels.GroupBy(label => label).Where(g => g.Count() > 1).Select(g => g.Key);

    static IEnumerable<(int A, int B)> Twins<T>(IReadOnlyList<T> items, Func<T, T, bool> same)
    {
        for (var i = 0; i < items.Count; i++)
            for (var j = i + 1; j < items.Count; j++)
                if (same(items[i], items[j]))
                    yield return (i, j);
    }

    static bool Short(CartesianPoint a, CartesianPoint b) => CartesianPoint.Distance(a, b) <= Tolerance;

    static bool SameLine((CartesianPoint? Start, CartesianPoint? End) a, (CartesianPoint? Start, CartesianPoint? End) b) =>
        a is ({ } a1, { } a2) && b is ({ } b1, { } b2) &&
        (Short(a1, b1) && Short(a2, b2) || Short(a1, b2) && Short(a2, b1));

    /// <summary>Where the lines through two axes cross; <see langword="null"/> when they are parallel.</summary>
    static CartesianPoint? Intersection(GridAxisCurve a, GridAxisCurve b)
    {
        double ax = a.Start.X.Meters, ay = a.Start.Y.Meters, dx = a.End.X.Meters - ax, dy = a.End.Y.Meters - ay;
        double bx = b.Start.X.Meters, by = b.Start.Y.Meters, ex = b.End.X.Meters - bx, ey = b.End.Y.Meters - by;
        var cross = dx * ey - dy * ex;
        if (Abs(cross) <= ParallelSine * Sqrt(dx * dx + dy * dy) * Sqrt(ex * ex + ey * ey))
            return null;
        var t = ((bx - ax) * ey - (by - ay) * ex) / cross;
        return new(Length.FromMeters(ax + t * dx), Length.FromMeters(ay + t * dy));
    }

    /// <summary>
    /// Every corner of <paramref name="inner"/>, and a point of every stretch of its edges, in or on
    /// <paramref name="outer"/>, and no edge cutting through it.
    /// </summary>
    static bool Within(IReadOnlyList<CartesianPoint> inner, IReadOnlyList<CartesianPoint> outer) =>
        inner.Concat(Midpoints(inner, outer)).All(p => OnEdge(p, outer) || Inside(p, outer)) && !Crossing(inner, outer);

    /// <summary>Sharing more than an edge or a corner.</summary>
    static bool Overlap(IReadOnlyList<CartesianPoint> a, IReadOnlyList<CartesianPoint> b) =>
        Crossing(a, b) || Pierces(a, b) || Pierces(b, a);

    /// <summary>
    /// A corner of <paramref name="a"/> or a point of a stretch of its edges strictly inside
    /// <paramref name="b"/>, or its centroid when that lies inside both: the case of two identical outlines.
    /// </summary>
    static bool Pierces(IReadOnlyList<CartesianPoint> a, IReadOnlyList<CartesianPoint> b)
    {
        var centroid = Centroid(a);
        return a.Concat(Midpoints(a, b)).Any(p => StrictlyInside(p, b)) ||
            StrictlyInside(centroid, a) && StrictlyInside(centroid, b);
    }

    /// <summary>
    /// The middle of each stretch of <paramref name="polygon"/>'s edges between the corners of
    /// <paramref name="other"/> on them. An edge passing through a corner of the other, which no
    /// crossing catches, has a stretch on each side of it to test.
    /// </summary>
    static IEnumerable<CartesianPoint> Midpoints(IReadOnlyList<CartesianPoint> polygon, IReadOnlyList<CartesianPoint> other)
    {
        foreach (var (a, b) in Edges(polygon))
        {
            double dx = b.X.Meters - a.X.Meters, dy = b.Y.Meters - a.Y.Meters, squared = dx * dx + dy * dy;
            if (squared is 0)
                continue;
            var cuts = other
                .Where(p => Touches(p, (a, b)))
                .Select(p => ((p.X.Meters - a.X.Meters) * dx + (p.Y.Meters - a.Y.Meters) * dy) / squared)
                .Append(0).Append(1).Order().ToList();
            for (var i = 0; i + 1 < cuts.Count; i++)
            {
                var t = (cuts[i] + cuts[i + 1]) / 2;
                yield return new(Length.FromMeters(a.X.Meters + t * dx), Length.FromMeters(a.Y.Meters + t * dy));
            }
        }
    }

    /// <summary>An edge of one cuts through an edge of the other; touching or running along it doesn't count.</summary>
    static bool Crossing(IReadOnlyList<CartesianPoint> a, IReadOnlyList<CartesianPoint> b) =>
        Edges(a).Any(e => Edges(b).Any(f => Cuts(e, f)));

    static bool Cuts((CartesianPoint A, CartesianPoint B) e, (CartesianPoint A, CartesianPoint B) f) =>
        Side(e.A, e.B, f.A) * Side(e.A, e.B, f.B) < 0 && Side(f.A, f.B, e.A) * Side(f.A, f.B, e.B) < 0;

    /// <summary>Two edges that share no corner cut or touch each other.</summary>
    static bool CrossesItself(IReadOnlyList<CartesianPoint> corners)
    {
        var edges = Edges(corners).ToList();
        for (var i = 0; i < edges.Count; i++)
            for (var j = i + 2; j < edges.Count; j++)
            {
                if (i is 0 && j == edges.Count - 1)
                    continue;
                var (e, f) = (edges[i], edges[j]);
                if (Cuts(e, f) || Touches(e.A, f) || Touches(e.B, f) || Touches(f.A, e) || Touches(f.B, e))
                    return true;
            }
        return false;
    }

    static bool StrictlyInside(CartesianPoint p, IReadOnlyList<CartesianPoint> polygon) =>
        !OnEdge(p, polygon) && Inside(p, polygon);

    /// <summary>By ray casting; a point on an edge may go either way.</summary>
    static bool Inside(CartesianPoint p, IReadOnlyList<CartesianPoint> polygon)
    {
        var inside = false;
        foreach (var (a, b) in Edges(polygon))
            if (a.Y > p.Y != b.Y > p.Y &&
                p.X.Meters < a.X.Meters + (b.X.Meters - a.X.Meters) * (p.Y.Meters - a.Y.Meters) / (b.Y.Meters - a.Y.Meters))
                inside = !inside;
        return inside;
    }

    static bool OnEdge(CartesianPoint p, IReadOnlyList<CartesianPoint> polygon) =>
        Edges(polygon).Any(e => Touches(p, e));

    static bool Touches(CartesianPoint p, (CartesianPoint A, CartesianPoint B) edge) =>
        Distance(p, edge.A, edge.B) <= Tolerance.Meters;

    /// <summary>From <paramref name="p"/> to the segment a–b, in metres.</summary>
    static double Distance(CartesianPoint p, CartesianPoint a, CartesianPoint b)
    {
        double dx = b.X.Meters - a.X.Meters, dy = b.Y.Meters - a.Y.Meters, squared = dx * dx + dy * dy;
        var t = squared is 0 ? 0 : Clamp(((p.X.Meters - a.X.Meters) * dx + (p.Y.Meters - a.Y.Meters) * dy) / squared, 0, 1);
        return Sqrt(Pow(p.X.Meters - a.X.Meters - t * dx, 2) + Pow(p.Y.Meters - a.Y.Meters - t * dy, 2));
    }

    /// <summary>Which side of the line through a and b <paramref name="p"/> lies: 1 left, −1 right, 0 on it.</summary>
    static int Side(CartesianPoint a, CartesianPoint b, CartesianPoint p)
    {
        double dx = b.X.Meters - a.X.Meters, dy = b.Y.Meters - a.Y.Meters;
        var cross = dx * (p.Y.Meters - a.Y.Meters) - dy * (p.X.Meters - a.X.Meters);
        return Abs(cross) <= Tolerance.Meters * Sqrt(dx * dx + dy * dy) ? 0 : Sign(cross);
    }

    static IEnumerable<(CartesianPoint A, CartesianPoint B)> Edges(IReadOnlyList<CartesianPoint> polygon) =>
        polygon.Select((p, i) => (p, polygon[(i + 1) % polygon.Count]));

    /// <summary>Of the area a boundary encloses; the boundary must enclose some.</summary>
    static CartesianPoint Centroid(IReadOnlyList<CartesianPoint> corners)
    {
        double x = 0, y = 0;
        foreach (var (a, b) in Edges(corners))
        {
            var cross = a.X.Meters * b.Y.Meters - b.X.Meters * a.Y.Meters;
            x += (a.X.Meters + b.X.Meters) * cross;
            y += (a.Y.Meters + b.Y.Meters) * cross;
        }
        var area = Area(corners);
        return new(Length.FromMeters(x / (6 * area)), Length.FromMeters(y / (6 * area)));
    }

    /// <summary>Signed, in square metres: positive when the corners run counter-clockwise.</summary>
    static double Area(IReadOnlyList<CartesianPoint> corners) =>
        corners.Select((p, i) => (p, q: corners[(i + 1) % corners.Count]))
            .Sum(x => x.p.X.Meters * x.q.Y.Meters - x.q.X.Meters * x.p.Y.Meters) / 2;
}
