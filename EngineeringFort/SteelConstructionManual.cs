namespace EngineeringFort;

public static class SteelConstructionManual
{
    /// <summary>
    /// The structural formation (configuration) of a beam, determining its support conditions.
    /// </summary>
    public enum BeamForm
    {
        /// <summary>
        /// Simply Supported
        /// </summary>
        [Display(Name = nameof(BeamFormulas.SimpleBeam), ResourceType = typeof(DisplayStrings))]
        Simple,

        [Display(Name = nameof(BeamFormulas.CantileverBeam), ResourceType = typeof(DisplayStrings))]
        Cantilever,

        [Display(Name = nameof(BeamFormulas.ContinuousBeam), ResourceType = typeof(DisplayStrings))]
        Continuous
    }

    public enum LoadingCondition
    {
        UniformlyDistributed,
        IncreasingUniformlyToOneEnd,
        IncreasingUniformlyToCenter
    }

    public static class BeamFormulas
    {
        /// <summary>
        /// Simply Supported Beam
        /// </summary>
        public static class SimpleBeam
        {
            public static class UniformlyDistributedLoad
            {
                /// <summary>
                /// (w * l) / 2
                /// </summary>
                /// <returns>The maximum shear force</returns>
                public static Force Vmax(ForcePerLength w, Length l) => (w * l) / 2;

                /// <summary>
                /// (w * (l * l)) / 8
                /// </summary>
                /// <returns>The maximum bending moment</returns>
                public static Torque Mmax(ForcePerLength w, Length l) => (w * (l * l)) / 8;

                /// <summary>
                /// (5 * w * (l * l * l * l)) / (384 * E * I)
                /// </summary>
                /// <returns>The maximum deflection</returns>
                public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I)
                {
                    var eiNewtonSquareMeters = E.Pascals * I.MetersToTheFourth;
                    if (eiNewtonSquareMeters is 0) return Length.Zero;

                    return Length.FromMeters((5 * w.NewtonsPerMeter * Math.Pow(l.Meters, 4)) / (384 * eiNewtonSquareMeters));
                }
            }
        }

        public static class CantileverBeam
        {
            public static class UniformlyDistributedLoad
            {
                /// <summary>
                /// w * l
                /// </summary>
                /// <returns>The maximum shear force</returns>
                public static Force Vmax(ForcePerLength w, Length l) => w * l;

                /// <summary>
                /// (w * (l * l)) / 2
                /// </summary>
                /// <returns>The maximum bending moment</returns>
                public static Torque Mmax(ForcePerLength w, Length l) => (w * (l * l)) / 2;

                /// <summary>
                /// (w * (l * l * l * l)) / (8 * E * I)
                /// </summary>
                /// <returns>The maximum deflection</returns>
                public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I)
                { 
                    var eiNewtonSquareMeters = E.Pascals * I.MetersToTheFourth;
                    if (eiNewtonSquareMeters is 0) return Length.Zero;

                    return Length.FromMeters((w.NewtonsPerMeter * Math.Pow(l.Meters, 4)) / (8 * eiNewtonSquareMeters));
                }
            }
        }

        public static class ContinuousBeam
        {
            public static class ThreeEqualSpans
            {
                public static class AllSpansLoaded
                {
                    /// <summary>
                    /// 0.6 * (w * l)
                    /// </summary>
                    /// <returns>The maximum shear force</returns>
                    public static Force Vmax(ForcePerLength w, Length l) => 0.6 * (w * l);

                    /// <summary>
                    /// 0.1 * (w * (l * l))
                    /// </summary>
                    /// <returns>The maximum bending moment</returns>
                    public static Torque Mmax(ForcePerLength w, Length l) => 0.1 * (w * (l * l));

                    /// <summary>
                    /// (w * (l * l * l * l)) / (145 * E * I)
                    /// </summary>
                    /// <returns>The maximum deflection</returns>
                    public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I)
                    {
                        var eiNewtonSquareMeters = E.Pascals * I.MetersToTheFourth;
                        if (eiNewtonSquareMeters is 0) return Length.Zero;

                        return Length.FromMeters((w.NewtonsPerMeter * Math.Pow(l.Meters, 4)) / (145 * eiNewtonSquareMeters));
                    }
                }
            }
        }
    }

    public static class SteelSpecs
    {
        public record Steel : IMaterial;

        public record SteelBeam(ICrossSection CrossSection, ForcePerLength WeightPerLength);

        public static readonly Dictionary<string, SteelBeam> Presets = new()
        {
            {
                "H150×75×5×7",
                new SteelBeam(
                    new HSection()
                    {
                        CrossSectionalArea = Area.FromSquareCentimeters(17.85),
                        SectionModulus = Volume.FromCubicCentimeters(88.8),
                        MomentOfInertia = AreaMomentOfInertia.FromCentimetersToTheFourth(666),
                    },
                    ForcePerLength.FromKilogramsForcePerMeter(14)
                )
            },
            {
                "H200×100×5.5×8",
                new SteelBeam(
                    new HSection()
                    {
                        CrossSectionalArea = Area.FromSquareCentimeters(26.67),
                        SectionModulus = Volume.FromCubicCentimeters(181),
                        MomentOfInertia = AreaMomentOfInertia.FromCentimetersToTheFourth(1810),
                    },
                    ForcePerLength.FromKilogramsForcePerMeter(20.9)
                )
            },
            {
                "C100×50×5×7.5",
                new SteelBeam(
                    new CSection()
                    {
                        CrossSectionalArea = Area.FromSquareCentimeters(11.92),
                        SectionModulus = Volume.FromCubicCentimeters(37.6),
                        MomentOfInertia = AreaMomentOfInertia.FromCentimetersToTheFourth(188),
                    },
                    ForcePerLength.FromKilogramsForcePerMeter(9.36)
                )
            },
        };
    }
}
