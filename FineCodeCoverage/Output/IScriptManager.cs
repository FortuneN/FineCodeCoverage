using System;
using System.Runtime.InteropServices;

namespace FineCodeCoverage.Output
{
    [ComVisible(true)]
    public interface IScriptManager
    {
        void BuyMeACoffee();
        void DocumentFocused();
        void LogIssueOrSuggestion();
        void OpenFile(string assemblyName, string qualifiedClassName, int file, int line);
        void RateAndReview();
        Action FocusCallback { get; set; }
    }
}