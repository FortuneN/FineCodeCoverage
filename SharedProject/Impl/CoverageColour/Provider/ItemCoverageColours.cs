using System.Windows.Media;

namespace FineCodeCoverage.Impl
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
            if (other == this) return true;
            if (other == null) return false;
            return Foreground == other.Foreground && Background == other.Background;

        }
    }

}
