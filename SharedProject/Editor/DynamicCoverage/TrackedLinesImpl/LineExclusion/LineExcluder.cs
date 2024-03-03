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
            var lineExclusionCharacters = isCSharp ? cSharpExclusions : vbExclusions;
            var trimmedLineText = text.Trim();
            return trimmedLineText.Length == 0 || lineExclusionCharacters.Any(lineExclusionCharacter => trimmedLineText.StartsWith(lineExclusionCharacter));
        }

    }
}
