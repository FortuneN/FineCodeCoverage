using Microsoft.VisualStudio.Text;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    internal static class CodeLineExcluder
    {
        private static readonly string[] cSharpExclusions = new string[] { "//", "#","using" };
        private static readonly string[] vbExclusions = new string[] { "REM", "'", "#" };
        public static bool ExcludeIfNotCode(SnapshotSpan lineSpan, bool isCSharp)
        {
            var lineExclusionCharacters = isCSharp ? cSharpExclusions : vbExclusions;
            var trimmedLineText = lineSpan.GetText().Trim();
            return trimmedLineText.Length == 0 || lineExclusionCharacters.Any(lineExclusionCharacter => trimmedLineText.StartsWith(lineExclusionCharacter));
        }
    }
}
