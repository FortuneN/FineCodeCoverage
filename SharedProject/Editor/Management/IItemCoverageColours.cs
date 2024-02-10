using System;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Management
{
    internal interface IItemCoverageColours : IEquatable<IItemCoverageColours>
    {
        Color Foreground { get; }
        Color Background { get; }

    }
}
