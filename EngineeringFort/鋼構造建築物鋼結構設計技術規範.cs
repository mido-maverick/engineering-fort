namespace EngineeringFort;

/// <summary>
///     Specification for Structural Steel Buildings
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <see href="https://www.nlma.gov.tw/ch/legislation/regsearch/7176">
///                 鋼構造建築物鋼結構設計技術規範 | 內政部國土管理署全球資訊網
///             </see>
///         </item>
///         <item>
///             <see href="https://glrs.moi.gov.tw/LawContent.aspx?id=GL000143">
///                 內政部主管法規查詢系統-法規內容-鋼構造建築物鋼結構設計技術規範
///             </see>
///         </item>
///     </list>
/// </remarks>
public static class 鋼構造建築物鋼結構設計技術規範
{
    /// <summary>
    ///     (一) 鋼結構容許應力設計法規範及解說
    /// </summary>
    public static class 鋼結構容許應力設計法
    {
        /// <remarks>
        ///     <see href="https://www.nlma.gov.tw/uploads/files/e139e8d278188c817090a2510017202e.pdf">
        ///         鋼結構容許應力設計法 第六章 受壓構材
        ///     </see>
        /// </remarks>
        public static class 受壓構材
        {
            /// <summary>Slenderness ratio λ = KL/r</summary>
            public static double 細長比(double K, Length L, Length r) => K * L / r;

            /// <summary>Critical slenderness ratio Cc = √(2π²E / Fy)</summary>
            public static double 臨界細長比(Pressure E, Pressure Fy) =>
                Sqrt(2 * PI * PI * (E / Fy));

            /// <summary>Allowable compressive stress Fa on the gross section</summary>
            /// <remarks>
            ///     Takes E and Fy rather than a precomputed Cc, so the branch and the formula always
            ///     use the same Cc.
            /// </remarks>
            public static Pressure 容許壓應力(double 細長比, Pressure E, Pressure Fy)
            {
                var Cc = 臨界細長比(E, Fy);

                if (細長比 >= Cc) return 12 * PI * PI * E / (23 * 細長比 * 細長比);

                var ratio = 細長比 / Cc;
                var factor = 5.0 / 3 + 3 * ratio / 8 - ratio * ratio * ratio / 8;
                return (1 - ratio * ratio / 2) * Fy / factor;
            }
        }
    }
}
