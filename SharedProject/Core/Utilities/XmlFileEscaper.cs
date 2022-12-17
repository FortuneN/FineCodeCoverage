namespace FineCodeCoverage.Core.Utilities
{
    internal static class XmlFileEscaper
    {
        public static string Escape(string filePath)
        {
            return filePath.Replace("&", "&#38;").Replace("'", "&#39;");
        }
    }
}
