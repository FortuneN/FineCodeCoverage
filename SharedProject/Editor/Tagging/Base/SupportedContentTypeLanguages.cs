namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal enum Language { CSharp, VB, CPP }
    internal class SupportedContentTypeLanguages
    {
        public const string CSharp = "CSharp";
        public const string VisualBasic = "Basic";
        public const string CPP = "C/C++";
        public static Language GetLanguage(string contentType)
        {
            if (contentType == CSharp)
            {
                return Language.CSharp;
            }
            if (contentType == VisualBasic)
            {
                return Language.VB;
            }
            return Language.CPP;
        }
    }
}
