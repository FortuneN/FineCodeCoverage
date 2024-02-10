using System;

namespace FineCodeCoverage.Impl
{
    internal interface IFontAndColorsInfo : IEquatable<IFontAndColorsInfo>
    {
        IItemCoverageColours ItemCoverageColours { get; }
        bool IsBold { get; }
    }
}
