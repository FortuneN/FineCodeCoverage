using Microsoft.VisualStudio.Text.Editor;

namespace FineCodeCoverage.Impl
{
    internal class GlyphTag : IGlyphTag
    {
        public bool IsCovered { get; }

        public GlyphTag(bool isCovered)
        {
            IsCovered = isCovered;
        }
    }
}
