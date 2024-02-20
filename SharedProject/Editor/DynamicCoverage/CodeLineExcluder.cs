using Microsoft.VisualStudio.Text;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [Export(typeof(ICodeLineExcluder))]
    internal class CodeLineExcluder : ICodeLineExcluder
    {
        private static readonly string[] cSharpExclusions = new string[] { "//", "#","using" };
        private static readonly string[] vbExclusions = new string[] { "REM", "'", "#" };
        public static bool ExcludeIfNotCode(SnapshotSpan lineSpan, bool isCSharp)
        {
            return Exclude(lineSpan.GetText(), isCSharp);
        }
        public bool ExcludeIfNotCode(string text, bool isCSharp)
        {
            return Exclude(text, isCSharp);
        }
        public static bool Exclude(string text, bool isCSharp)
        {
            var lineExclusionCharacters = isCSharp ? cSharpExclusions : vbExclusions;
            var trimmedLineText = text.Trim();
            return trimmedLineText.Length == 0 || lineExclusionCharacters.Any(lineExclusionCharacter => trimmedLineText.StartsWith(lineExclusionCharacter));
        }
    }
}
