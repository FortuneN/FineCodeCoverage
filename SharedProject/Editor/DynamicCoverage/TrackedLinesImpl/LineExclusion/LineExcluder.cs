using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ILineExcluder))]
    internal class LineExcluder : ILineExcluder
    {
        private static readonly string[] cSharpExclusions = new string[] { "//", "#", "using" };
        private static readonly string[] vbExclusions = new string[] { "REM", "'", "#" };

        public bool ExcludeIfNotCode(string text, bool isCSharp)
        {
            string trimmedLineText = text.Trim();
            return trimmedLineText.Length == 0 || this.StartsWithExclusion(trimmedLineText, isCSharp);
        }

        private bool StartsWithExclusion(string text, bool isCSharp)
        {
            string[] languageExclusions = isCSharp ? cSharpExclusions : vbExclusions;
            return languageExclusions.Any(languageExclusion => text.StartsWith(languageExclusion));
        }
    }
}
