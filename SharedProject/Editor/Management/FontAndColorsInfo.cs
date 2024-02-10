namespace FineCodeCoverage.Editor.Management
{
    internal class FontAndColorsInfo : IFontAndColorsInfo
    {
        public FontAndColorsInfo(IItemCoverageColours itemCoverageColours, bool isBold)
        {
            ItemCoverageColours = itemCoverageColours;
            IsBold = isBold;
        }

        public IItemCoverageColours ItemCoverageColours { get; }
        public bool IsBold { get; }

        public bool Equals(IFontAndColorsInfo other)
        {
            return IsBold == other.IsBold && ItemCoverageColours.Equals(other.ItemCoverageColours);
        }
    }
}
