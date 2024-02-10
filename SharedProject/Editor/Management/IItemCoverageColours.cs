using System;
using System.Windows.Media;

namespace FineCodeCoverage.Impl
{
    internal interface IItemCoverageColours : IEquatable<IItemCoverageColours>
    {
        Color Foreground { get; }
        Color Background { get; }

    }
}
