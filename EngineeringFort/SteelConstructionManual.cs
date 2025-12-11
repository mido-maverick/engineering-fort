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
                public static Force Vmax(ForcePerLength w, Length l) => (w * l) / 2;
                public static Torque Mmax(ForcePerLength w, Length l) => (w * (l * l)) / 8;
                public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I) // (5 * w * (l * l * l * l)) / (384 * E * I)
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
                public static Force Vmax(ForcePerLength w, Length l) => w * l;
                public static Torque Mmax(ForcePerLength w, Length l) => (w * (l * l)) / 2;
                public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I) // (w * (l * l * l * l)) / (8 * E * I)
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
                    public static Force Vmax(ForcePerLength w, Length l) => 0.6 * (w * l);
                    public static Torque Mmax(ForcePerLength w, Length l) => 0.1 * (w * (l * l));
                    public static Length Δmax(ForcePerLength w, Length l, Pressure E, AreaMomentOfInertia I) // (w * (l * l * l * l)) / (145 * E * I)
                    {
                        var eiNewtonSquareMeters = E.Pascals * I.MetersToTheFourth;
                        if (eiNewtonSquareMeters is 0) return Length.Zero;

                        return Length.FromMeters((w.NewtonsPerMeter * Math.Pow(l.Meters, 4)) / (145 * eiNewtonSquareMeters));
                    }
                }
            }
        }
    }
}
