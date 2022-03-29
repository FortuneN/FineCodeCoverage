namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    internal static class MsCodeCoverageRegex
    {
        public static string RegexEscapePath(string path)
        {
            return path.Replace(@"\", @"\\");
        }

        public static string RegexModuleName(string moduleName)
        {
            return $".*\\\\{moduleName}.dll^";
        }
    }

}
