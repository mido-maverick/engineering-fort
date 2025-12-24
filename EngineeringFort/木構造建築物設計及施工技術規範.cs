namespace EngineeringFort;

/// <summary>
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <see href="https://www.nlma.gov.tw/ch/legislation/regsearch/960">
///                 木構造建築物設計及施工技術規範 | 內政部國土管理署全球資訊網
///             </see>
///         </item>
///         <item>
///             <see href="https://glrs.moi.gov.tw/LawContent.aspx?id=FL025530">
///                 內政部主管法規查詢系統-法規內容-木構造建築物設計及施工技術規範
///             </see>
///         </item>
///     </list>
/// </remarks>
public static class 木構造建築物設計及施工技術規範
{
    public enum 受力類型 { 壓, 拉, 彎, 剪 }

    /// <remarks>
    ///     <see href="https://www.nlma.gov.tw/uploads/files/c3069fb4ce95f330816455e1f10e5b11.pdf">
    ///         木構造建築物設計及施工技術規範 第四章 材料及容許應力
    ///     </see>
    /// </remarks>
    public static class 材料及容許應力
    {
        public const double 經常濕潤增減係數 = 0.7;

        public enum 樹種
        {
            // 針葉樹Ⅰ~Ⅳ類
            花旗松, 俄國落葉松,
            羅漢柏, 扁柏, 羅森檜, 硬木南方松,
            赤松, 黑松, 落葉松, 鐵杉, 北美鐵杉, 軟木南方松, 世界爺,
            冷杉, 蝦夷松, 椵松, 朝鮮松, 柳杉, 西部側柏, 雲杉, 杉木, 台灣杉, 放射松,

            // 闊葉樹Ⅰ~Ⅲ類
            樫木,
            栗木, 櫟木, 山毛櫸, 櫸木, 油脂木, 冰片樹, 硬槭木,
            柳桉
        }
        public enum 針闊葉樹別 { 針葉, 闊葉 }
        public enum 木材類別 { Ⅰ = 1, Ⅱ, Ⅲ, Ⅳ }
        public enum 木材等級 { 普通, 上等 }
        public enum 合板等級 { A, B, C }

        public abstract record class 結構用材料;

        /// <summary>
        /// 結構用木材 (Timber)
        /// </summary>
        public record class 木材 : 結構用材料, Formwork.IFormworkSupportMaterial, Formwork.IFormworkSheathingMaterial
        {
            public 樹種 樹種 { get; set; }

            public 針闊葉樹別 針闊葉樹別 => 樹種 switch
            {
                >= 樹種.花旗松 and <= 樹種.放射松 => 針闊葉樹別.針葉,
                >= 樹種.樫木 and <= 樹種.柳桉 => 針闊葉樹別.闊葉,
                _ => throw new ArgumentOutOfRangeException()
            };

            public 木材類別 木材類別 => 樹種 switch
            {
                樹種.花旗松 or 樹種.俄國落葉松 => 木材類別.Ⅰ,
                >= 樹種.羅漢柏 and <= 樹種.硬木南方松 => 木材類別.Ⅱ,
                >= 樹種.赤松 and <= 樹種.世界爺 => 木材類別.Ⅲ,
                >= 樹種.冷杉 and <= 樹種.放射松 => 木材類別.Ⅳ,
                樹種.樫木 => 木材類別.Ⅰ,
                >= 樹種.栗木 and <= 樹種.硬槭木 => 木材類別.Ⅱ,
                樹種.柳桉 => 木材類別.Ⅲ,
                _ => throw new ArgumentOutOfRangeException()
            };

            public 木材等級 木材等級 { get; set; }

            /// <summary>
            ///     木材纖維方向之彈性模數，取
            ///     <see href="https://www.nlma.gov.tw/uploads/files/c3069fb4ce95f330816455e1f10e5b11.pdf">
            ///         木構造建築物設計及施工技術規範 第四章 材料及容許應力
            ///     </see>
            ///     表 4.4-1 所示之值。
            /// </summary>
            public Pressure 纖維方向之彈性模數 => Pressure.FromKilogramsForcePerSquareCentimeter((針闊葉樹別, 木材類別, 木材等級) switch
            {
                (針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.普通) => 100_000,
                (針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.上等) => 110_000,
                (針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.普通) =>  90_000,
                (針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.上等) => 100_000,
                (針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.普通) =>  80_000,
                (針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.上等) =>  90_000,
                (針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.普通) =>  70_000,
                (針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.上等) =>  80_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅰ, 木材等級.普通) => 100_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅰ, 木材等級.上等) => 110_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅱ, 木材等級.普通) =>  80_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅱ, 木材等級.上等) =>  90_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅲ, 木材等級.普通) =>  70_000,
                (針闊葉樹別.闊葉, 木材類別.Ⅲ, 木材等級.上等) =>  80_000,
                _ => double.NaN
            });

            public static Pressure 纖維方向之長期容許應力(受力類型 受力類型, 針闊葉樹別 針闊葉樹別, 木材類別 木材類別, 木材等級 木材等級)
            {
                return Pressure.FromKilogramsForcePerSquareCentimeter((受力類型, 針闊葉樹別, 木材類別, 木材等級) switch
                {
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.普通) =>  95,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.普通) =>  90,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.普通) =>  85,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.普通) =>  75,
                    (受力類型.彎, 針闊葉樹別.闊葉, 木材類別.Ⅰ, 木材等級.普通) => 130,
                    (受力類型.彎, 針闊葉樹別.闊葉, 木材類別.Ⅱ, 木材等級.普通) => 100,
                    (受力類型.彎, 針闊葉樹別.闊葉, 木材類別.Ⅲ, 木材等級.普通) =>  90,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.普通) =>   8,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.普通) =>   7,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.普通) =>   7,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.普通) =>   6,
                    (受力類型.剪, 針闊葉樹別.闊葉, 木材類別.Ⅰ, 木材等級.普通) =>  14,
                    (受力類型.剪, 針闊葉樹別.闊葉, 木材類別.Ⅱ, 木材等級.普通) =>  10,
                    (受力類型.剪, 針闊葉樹別.闊葉, 木材類別.Ⅲ, 木材等級.普通) =>   6,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.上等) => 120,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.上等) => 110,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.上等) => 105,
                    (受力類型.彎, 針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.上等) =>  95,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅰ, 木材等級.上等) =>  10,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅱ, 木材等級.上等) =>   9,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅲ, 木材等級.上等) =>   9,
                    (受力類型.剪, 針闊葉樹別.針葉, 木材類別.Ⅳ, 木材等級.上等) =>   7,
                    (_, 針闊葉樹別.闊葉, _, 木材等級.上等) =>
                        1.25 * 纖維方向之長期容許應力(受力類型, 針闊葉樹別, 木材類別, 木材等級.普通).KilogramsForcePerSquareCentimeter,
                    _ => 0
                });
            }

            public static Pressure 纖維方向之短期容許應力(受力類型 受力類型, 針闊葉樹別 針闊葉樹別, 木材類別 木材類別, 木材等級 木材等級) =>
                2 * 纖維方向之長期容許應力(受力類型, 針闊葉樹別, 木材類別, 木材等級);

            public Pressure 長期容許彎應力 => 纖維方向之長期容許應力(受力類型.彎, 針闊葉樹別, 木材類別, 木材等級);

            public Pressure 長期容許剪應力 => 纖維方向之長期容許應力(受力類型.剪, 針闊葉樹別, 木材類別, 木材等級);

            public Pressure 短期容許彎應力 => 纖維方向之短期容許應力(受力類型.彎, 針闊葉樹別, 木材類別, 木材等級);

            public Pressure 短期容許剪應力 => 纖維方向之短期容許應力(受力類型.剪, 針闊葉樹別, 木材類別, 木材等級);

            Pressure Formwork.IFormworkSupportMaterial.AllowableBendingStress() => 短期容許彎應力;

            Pressure Formwork.IFormworkSupportMaterial.AllowableShearStress() => 短期容許剪應力;

            public Pressure ElasticModulus() => 纖維方向之彈性模數;

            Pressure Formwork.IFormworkSheathingMaterial.AllowableBendingStress(Length thickness) => 經常濕潤增減係數 * 短期容許彎應力;

            Pressure Formwork.IFormworkSheathingMaterial.AllowableShearStress() => 經常濕潤增減係數 * 短期容許剪應力;

            Pressure Formwork.IFormworkSheathingMaterial.ElasticModulus(Length thickness) => 纖維方向之彈性模數;
        }

        /// <summary>
        /// 結構用合板 (Plywood)
        /// </summary>
        public record class 合板 : 結構用材料, Formwork.IFormworkSheathingMaterial
        {
            public 合板等級 等級 { get; set; }

            /// <summary>
            /// 面板纖維垂直方向之長期容許拉應力 Lft⟂
            /// </summary>
            public static Pressure 垂直方向之長期容許拉應力(Length 厚度, 合板等級 等級) => Pressure.FromKilogramsForcePerSquareCentimeter((厚度.Millimeters, 等級) switch
            {
                (15, _) => 55,
                _ => 0
            });

            /// <summary>
            /// 面板纖維垂直方向之短期容許拉應力 Sft⟂
            /// </summary>
            public static Pressure 垂直方向之短期容許拉應力(Length 厚度, 合板等級 等級) => 2 * 垂直方向之長期容許拉應力(厚度, 等級);

            public static Pressure 長期容許應力(受力類型 受力類型, 合板等級 合板等級) =>
                Pressure.FromKilogramsForcePerSquareCentimeter((受力類型, 合板等級) switch
                {
                    (受力類型.剪, 合板等級.A) => 14,
                    (受力類型.剪, 合板等級.B) => 13,
                    (受力類型.剪, 合板等級.C) => 12,
                    _ => 0
                });

            public static Pressure 短期容許應力(受力類型 受力類型, 合板等級 合板等級) => 2 * 長期容許應力(受力類型, 合板等級);

            Pressure Formwork.IFormworkSheathingMaterial.AllowableBendingStress(Length thickness) => 垂直方向之短期容許拉應力(thickness, 等級);

            Pressure Formwork.IFormworkSheathingMaterial.AllowableShearStress() => 長期容許應力(受力類型.剪, 等級);

            public Pressure ElasticModulus(Length thickness) => Pressure.FromKilogramsForcePerSquareCentimeter(thickness.Millimeters switch
            {
                15 => 50_000,
                _ => 0
            });
        }
    }
}