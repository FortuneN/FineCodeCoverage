namespace FineCodeCoverage.Editor.Management
{
    internal interface ICoverageFontAndColorsCategoryItemNames
    {
        FontAndColorsCategoryItemName Covered { get; }
        FontAndColorsCategoryItemName Dirty { get; }
        FontAndColorsCategoryItemName NewLines { get; }
        FontAndColorsCategoryItemName NotCovered { get; }
        FontAndColorsCategoryItemName PartiallyCovered { get; }
        FontAndColorsCategoryItemName NotIncluded { get; }
    }
}
