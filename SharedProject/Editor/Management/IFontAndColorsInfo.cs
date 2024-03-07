using System;

namespace FineCodeCoverage.Editor.Management
{
    internal interface IFontAndColorsInfo : IEquatable<IFontAndColorsInfo>
    {
        IItemCoverageColours ItemCoverageColours { get; }
        bool IsBold { get; }
    }
}
