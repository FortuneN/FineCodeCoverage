using System;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    internal static class LuminanceContrastColourExtensions
    {
        public static double Contrast(this System.Drawing.Color color, System.Drawing.Color color2)
        {
            var l1 = color.Luminance();
            var l2 = color2.Luminance();
            return l1 > l2 ? (l1 + 0.05) / (l2 + 0.05) : (l2 + 0.05) / (l1 + 0.05);

        }

        public static double Luminance(this System.Drawing.Color color)
        {
            return Luminance(color.R, color.G, color.B);
        }

        private static double Luminance(int r, int g, int b)
        {
            var lr = LuminanceX(r);
            var lg = LuminanceX(g);
            var lb = LuminanceX(b);
            return 0.2126 * lr + 0.7152 * lg + 0.0722 * lb;

        }

        private static double LuminanceX(int x)
        {
            x /= 255;
            return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4);

        }
    }
}
