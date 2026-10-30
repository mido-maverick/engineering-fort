using EngineeringFort.Construction;

namespace EngineeringFort.Tests.Construction;

public class BuildingTest
{
    static CartesianPoint At(double x, double y) => new(Length.FromCentimeters(x), Length.FromCentimeters(y));

    static Length Cm(double value) => Length.FromCentimeters(value);

    static Placement Grid(string u, string v, double dx = 0, double dy = 0) =>
        new() { U = new(u), V = new(v), DX = Cm(dx), DY = Cm(dy) };

    /// <summary>Axes 1 and 2 run north 600 apart, A and B run east 500 apart.</summary>
    static readonly Grid Axes = new()
    {
        UAxes = [new(new("1"), At(0, -100), At(0, 600)), new(new("2"), At(600, -100), At(600, 600))],
        VAxes = [new(new("A"), At(-100, 0), At(700, 0)), new(new("B"), At(-100, 500), At(700, 500))],
    };

    static Wall Wall(params WallOpening[] openings) => new()
    {
        Type = "RC20", Start = Grid("1", "A"), End = Grid("2", "A"), Openings = openings,
    };

    static Building Building(params Wall[] walls) => new()
    {
        Grids = [Axes],
        WallTypes = [new WallType { Label = "RC20", Thickness = Cm(20) }],
        OpeningTypes =
        [
            new OpeningType { Label = "W1", Width = Cm(150), Height = Cm(120), Filling = OpeningFilling.Window },
            new OpeningType { Label = "D1", Width = Cm(120), Height = Cm(210), Filling = OpeningFilling.Door },
            new OpeningType { Label = "W2", Width = Cm(100) },
        ],
        Floors =
        [
            new Storey { Label = "1F", Elevation = Cm(0), Walls = walls },
            new Storey { Label = "2F", Elevation = Cm(320) },
        ],
    };

    [Fact]
    public void Locate_GridIntersection_IsWhereTheAxesCrossPlusTheOffset()
    {
        var (point, problem) = Building().Locate(Grid("2", "B", -10, 25));

        Assert.Null(problem);
        Assert.Equal(590, point!.Value.X.Centimeters, 6);
        Assert.Equal(525, point.Value.Y.Centimeters, 6);
    }

    [Fact]
    public void Locate_AxesNoGridHas_SaysWhich()
    {
        var (point, problem) = Building().Locate(Grid("A", "1"));

        Assert.Null(point);
        Assert.Equal("no grid has U axis A and V axis 1", problem);
    }

    [Fact]
    public void Validate_SoundModel_FindsNothing()
    {
        var window = new WallOpening { Type = "W1", Offset = Cm(100), SillHeight = Cm(90) };

        Assert.Empty(Building(Wall(window)).Validate());
    }

    [Fact]
    public void Validate_OpeningPastTheWallsEnd_IsFound()
    {
        var door = new WallOpening { Type = "D1", Offset = Cm(500), SillHeight = Cm(0) };

        Assert.Equal(["1F wall #1 (RC20) opening #1 (D1): runs past the wall's ends."], Building(Wall(door)).Validate());
    }

    [Fact]
    public void Validate_OpeningAboveTheNextFloor_IsFound()
    {
        var window = new WallOpening { Type = "D1", Offset = Cm(100), SillHeight = Cm(200) };

        Assert.Equal(["1F wall #1 (RC20) opening #1 (D1): runs above the wall."], Building(Wall(window)).Validate());
    }

    [Fact]
    public void Validate_OpeningsOfUnknownHeight_MayOverlap()
    {
        var a = new WallOpening { Type = "W2", Offset = Cm(100), SillHeight = Cm(0) };
        var b = new WallOpening { Type = "W1", Offset = Cm(150), SillHeight = Cm(200) };

        Assert.Equal(["1F wall #1 (RC20) opening #1 (W2) and #2 overlap."], Building(Wall(a, b)).Validate());
    }

    [Fact]
    public void Validate_UndefinedType_IsFound()
    {
        var wall = Wall() with { Type = "RC15" };

        Assert.Equal(["1F wall #1 (RC15): its type RC15 is not defined."], Building(wall).Validate());
    }

    [Fact]
    public void Validate_WallDrawnTwiceInEitherDirection_IsFound()
    {
        var wall = Wall();
        var reversed = wall with { Start = wall.End, End = wall.Start };

        Assert.Equal(["1F wall #1 (RC20) and #2 run between the same points."], Building(wall, reversed).Validate());
    }

    [Fact]
    public void Validate_WallWithNoLength_IsReportedOnce()
    {
        var wall = Wall(new WallOpening { Type = "W1", Offset = Cm(100), SillHeight = Cm(90) }) with { End = Grid("1", "A") };

        Assert.Equal(["1F wall #1 (RC20): has no length."], Building(wall).Validate());
    }

    [Fact]
    public void Validate_StoreyNotAboveTheOneBelow_IsFound()
    {
        // 2F's elevation is unknown, so 1FL is compared with 1F below it.
        var building = new Building
        {
            Floors = [new Storey { Label = "1F", Elevation = Cm(0) }, new Storey { Label = "2F" }, new Storey { Label = "1FL", Elevation = Cm(0) }],
        };

        Assert.Equal(["Storey 1FL is not above 1F."], building.Validate());
    }

    [Fact]
    public void Validate_ColumnTypeRoundAndRectangular_IsFound()
    {
        var building = Building() with { ColumnTypes = [new ColumnType { Label = "C1", XDim = Cm(60), YDim = Cm(60), Diameter = Cm(60) }] };

        Assert.Equal(["The column type C1: has both a diameter and sides."], building.Validate());
    }

    [Fact]
    public void Validate_BeamEndingOffEverySupport_IsFound()
    {
        // 2F's beams rest on 1F's columns: #1 between their faces, #2 along axis B on nothing.
        var building = Building() with
        {
            ColumnTypes = [new ColumnType { Label = "C60", XDim = Cm(60), YDim = Cm(60) }],
            BeamTypes = [new BeamType { Label = "B40", Width = Cm(40) }],
            Floors =
            [
                new Storey { Label = "1F", Elevation = Cm(0), Columns = [new Column { Type = "C60", Position = Grid("1", "A") }, new Column { Type = "C60", Position = Grid("2", "A") }] },
                new Storey
                {
                    Label = "2F",
                    Elevation = Cm(320),
                    Beams =
                    [
                        new Beam { Type = "B40", Start = Grid("1", "A", 30, 0), End = Grid("2", "A", -30, 0) },
                        new Beam { Type = "B40", Start = Grid("1", "B"), End = Grid("2", "B") },
                    ],
                },
            ],
        };

        Assert.Equal(
        [
            "2F beam #2 (B40): its start rests on no column, wall or beam.",
            "2F beam #2 (B40): its end rests on no column, wall or beam.",
        ], building.Validate());
    }

    /// <summary>A 600 × 500 rectangle between axes 1–2 and A–B.</summary>
    static readonly Placement[] Rectangle = [Grid("1", "A"), Grid("2", "A"), Grid("2", "B"), Grid("1", "B")];

    /// <summary>The rectangle without its corner north of A + 300 and west of 1 + 300.</summary>
    static readonly Placement[] LShape =
    [
        Grid("1", "A"), Grid("2", "A"), Grid("2", "B"), Grid("1", "A", 300, 500), Grid("1", "A", 300, 300), Grid("1", "A", 0, 300),
    ];

    /// <summary>A slab with voids on 1F.</summary>
    static Building SlabBuilding(Placement[] boundary, params SlabOpening[] openings) => Building() with
    {
        SlabTypes = [new SlabType { Label = "S1", Thickness = Cm(15) }],
        Floors =
        [
            new Storey { Label = "1F", Elevation = Cm(0), Slabs = [new Slab { Type = "S1", Boundary = boundary, Openings = openings }] },
            new Storey { Label = "2F", Elevation = Cm(320) },
        ],
    };

    static SlabOpening Square(double x, double y, double size) => new()
    {
        Boundary = [Grid("1", "A", x, y), Grid("1", "A", x + size, y), Grid("1", "A", x + size, y + size), Grid("1", "A", x, y + size)],
    };

    [Fact]
    public void Validate_SlabOpenings_NeedNoTypeButAnOutline()
    {
        var triangle = new SlabOpening { Boundary = [Grid("1", "A", 100, 100), Grid("1", "A", 300, 100), Grid("1", "A", 300, 350)] };
        var line = new SlabOpening { Boundary = [Grid("1", "A", 100, 100), Grid("1", "A", 300, 100)] };

        Assert.Equal(["1F slab #1 (S1) opening #2: its boundary encloses nothing."], SlabBuilding(Rectangle, triangle, line).Validate());
    }

    [Fact]
    public void Validate_BoundaryCrossingItself_IsFound()
    {
        var bowTie = new SlabOpening { Boundary = [Grid("1", "A", 100, 100), Grid("1", "A", 300, 400), Grid("1", "A", 300, 100), Grid("1", "A", 100, 200)] };

        Assert.Equal(["1F slab #1 (S1) opening #1: its boundary crosses itself."], SlabBuilding(Rectangle, bowTie).Validate());
    }

    [Fact]
    public void Validate_SlabOpeningFillingTheNotch_RunsOutside()
    {
        // Every corner is on the slab's edge and no edge cuts through one: only its diagonal leaves the slab.
        var notch = new SlabOpening { Boundary = [Grid("1", "A", 0, 300), Grid("1", "A", 300, 300), Grid("1", "A", 300, 500)] };

        Assert.Equal(["1F slab #1 (S1) opening #1: runs outside the slab."], SlabBuilding(LShape, notch).Validate());
    }

    [Fact]
    public void Validate_SlabOpeningsOutsideTheSlabOrOverlapping_AreFound()
    {
        // #1 and #3 share an edge; #2 overlaps both; #4 runs past axis 2.
        var building = SlabBuilding(Rectangle, Square(100, 100, 100), Square(150, 150, 100), Square(200, 100, 100), Square(550, 400, 100));

        Assert.Equal(
        [
            "1F slab #1 (S1) opening #4: runs outside the slab.",
            "1F slab #1 (S1) opening #1 and #2 overlap.",
            "1F slab #1 (S1) opening #2 and #3 overlap.",
        ], building.Validate());
    }

    [Fact]
    public void Unknowns_SizesLeftOut_AreListed()
    {
        var building = Building(Wall(new WallOpening { Type = "W2", Offset = Cm(100) })) with
        {
            BeamTypes = [new BeamType { Label = "G3", Width = Cm(40) }],
        };

        Assert.Equal(["Beam type G3: depth.", "Opening type W2: height.", "1 wall openings: sill height."], building.Unknowns());
    }

    [Fact]
    public void Unknowns_WallsOnTheTopFloor_HaveNoHeight()
    {
        // Not Building(): its W2's unknown height would be listed too.
        var building = new Building { Floors = [new Storey { Label = "RF", Elevation = Cm(0), Walls = [Wall()] }] };

        Assert.Equal(["RF: no floor above, so its columns' and walls' height."], building.Unknowns());
    }

    [Fact]
    public void Unknowns_StoreyWithoutElevation_IsListed()
    {
        var building = new Building
        {
            Floors = [new Storey { Label = "1F", Elevation = Cm(0) }, new Storey { Label = "2F" }, new Storey { Label = "RF", Elevation = Cm(640) }],
        };

        Assert.Equal(["Storey 2F: elevation."], building.Unknowns());
    }
}
