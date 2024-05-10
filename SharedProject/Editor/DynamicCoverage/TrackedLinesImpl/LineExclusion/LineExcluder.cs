using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ILineExcluder))]
    internal class LineExcluder : ILineExcluder
    {
        private readonly string[] startsWithExclusions;

        public LineExcluder(string[] startsWithExclusions) => this.startsWithExclusions = startsWithExclusions;

        public bool ExcludeIfNotCode(string text)
        {
            string trimmedLineText = text.Trim();
            return trimmedLineText.Length == 0 || this.StartsWithExclusion(trimmedLineText);
        }

        private bool StartsWithExclusion(string text) 
            => this.startsWithExclusions.Any(languageExclusion => text.StartsWith(languageExclusion));
    }
}
