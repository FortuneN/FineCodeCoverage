namespace FineCodeCoverage.Editor.Management
{
    internal class FontAndColorsInfo : IFontAndColorsInfo
    {
        public FontAndColorsInfo(IItemCoverageColours itemCoverageColours, bool isBold)
        {
            this.ItemCoverageColours = itemCoverageColours;
            this.IsBold = isBold;
        }

        public IItemCoverageColours ItemCoverageColours { get; }
        public bool IsBold { get; }

        public bool Equals(IFontAndColorsInfo other) => this.IsBold == other.IsBold && this.ItemCoverageColours.Equals(other.ItemCoverageColours);
    }
}
