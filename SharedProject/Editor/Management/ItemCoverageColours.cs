using System.Windows.Media;

namespace FineCodeCoverage.Editor.Management
{
    internal class ItemCoverageColours : IItemCoverageColours
    {
        public ItemCoverageColours(Color foreground, Color background)
        {
            this.Foreground = foreground;
            this.Background = background;
        }

        public Color Foreground { get; }
        public Color Background { get; }

        public bool Equals(IItemCoverageColours other)
        {
            return Foreground == other.Foreground && Background == other.Background;
        }
    }

}
