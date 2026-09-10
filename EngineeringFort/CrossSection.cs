namespace EngineeringFort;

public interface ICrossSection
{
    Area CrossSectionalArea { get; }
    Volume SectionModulus { get; }
    AreaMomentOfInertia MomentOfInertia { get; }
}

public record class RectangularCrossSection : ICrossSection
{
    public static Area CalculateCrossSectionalArea(Length w, Length h) => w * h;
    public static Volume CalculateSectionModulus(Length w, Length h) => w * h * h / 6;
    public static AreaMomentOfInertia CalculateMomentOfInertia(Length w, Length h) => // w * h * h * h / 12
        AreaMomentOfInertia.FromMetersToTheFourth(w.Meters * Math.Pow(h.Meters, 3) / 12);

    public Length Width { get; set; }
    public Length Height { get; set; }
    public Area CrossSectionalArea => CalculateCrossSectionalArea(Width, Height);
    public Volume SectionModulus => CalculateSectionModulus(Width, Height);
    public AreaMomentOfInertia MomentOfInertia => CalculateMomentOfInertia(Width, Height);
}

public record class HSection : ICrossSection
{
    public Area CrossSectionalArea { get; init; }
    public Volume SectionModulus { get; init; }
    public AreaMomentOfInertia MomentOfInertia { get; init; }
}

public record class CSection : ICrossSection
{
    public Area CrossSectionalArea { get; init; }
    public Volume SectionModulus { get; init; }
    public AreaMomentOfInertia MomentOfInertia { get; init; }
}

/// <summary>
/// Square or rectangular hollow section.
/// A square tube is the B = H case.
/// </summary>
public record class BoxSection : ICrossSection
{
    public Area CrossSectionalArea { get; init; }
    public Volume SectionModulus { get; init; }
    public AreaMomentOfInertia MomentOfInertia { get; init; }
}
