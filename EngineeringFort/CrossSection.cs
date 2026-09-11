namespace EngineeringFort;

public interface ICrossSection
{
    Area CrossSectionalArea { get; }
    Volume SectionModulus { get; }
    AreaMomentOfInertia MomentOfInertia { get; }

    /// <summary>
    /// The area the shear force is averaged over. A solid section takes all of it — which is what
    /// the timber checks and their shear-stress factor assume — so that is the default. A steel
    /// shape carries its shear in the web, and overrides this with the web alone.
    /// </summary>
    Area ShearArea => CrossSectionalArea;
}

public record class RectangularCrossSection : ICrossSection
{
    public static Area CalculateCrossSectionalArea(Length w, Length h) => w * h;
    public static Volume CalculateSectionModulus(Length w, Length h) => w * h * h / 6;
    public static AreaMomentOfInertia CalculateMomentOfInertia(Length w, Length h) => // w * h * h * h / 12
        AreaMomentOfInertia.FromMetersToTheFourth(w.Meters * Pow(h.Meters, 3) / 12);

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

    public required Length Height { get; init; }

    public required Length WebThickness { get; init; }

    /// <summary>The area steel's allowable shear stress is set against.</summary>
    public Area ShearArea => Height * WebThickness;
}

public record class CSection : ICrossSection
{
    public Area CrossSectionalArea { get; init; }
    public Volume SectionModulus { get; init; }
    public AreaMomentOfInertia MomentOfInertia { get; init; }

    public required Length Height { get; init; }

    public required Length WebThickness { get; init; }

    /// <inheritdoc cref="HSection.ShearArea"/>
    public Area ShearArea => Height * WebThickness;
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

    public required Length Height { get; init; }

    public required Length Thickness { get; init; }

    /// <summary>
    /// Both webs over their clear height between the flanges, 2·(H − 2t)·t. Clear rather than
    /// overall height, since the corners are shared with the flanges; the smaller area is the safe side.
    /// </summary>
    public Area ShearArea => 2 * (Height - 2 * Thickness) * Thickness;
}
