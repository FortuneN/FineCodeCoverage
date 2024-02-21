namespace FineCodeCoverage.Editor.Management
{
    internal class FontAndColorsItemNames
    {
        public FontAndColorsItemNames(MarkerTypeNames markerTypesName, MEFItemNames mefItemNames)
        {
            MarkerTypeNames = markerTypesName;
            MEFItemNames = mefItemNames;
        }
        public MarkerTypeNames MarkerTypeNames { get; }
        public MEFItemNames MEFItemNames { get; }
    }
}
