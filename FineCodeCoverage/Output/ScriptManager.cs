using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Output
{
    [Export]
    [ComVisible(true)] // do not change the accessibility - needs to be public class
    public class ScriptManager
    {
        internal const string payPal = "https://paypal.me/FortuneNgwenya";
        internal const string githubIssues = "https://github.com/FortuneN/FineCodeCoverage/issues";
        internal const string marketPlaceRateAndReview = "https://marketplace.visualstudio.com/items?itemName=FortuneNgwenya.FineCodeCoverage&ssr=false#review-details";
        private readonly ISourceFileOpener sourceFileOpener;
        private readonly IProcess process;
        internal System.Threading.Tasks.Task openFileTask;

        public Action FocusCallback { get; set; }

        [ImportingConstructor]
        internal ScriptManager(ISourceFileOpener sourceFileOpener, IProcess process)
        {
            this.sourceFileOpener = sourceFileOpener;
            this.process = process;
        }

        public void OpenFile(string assemblyName, string qualifiedClassName, int file, int line)
        {
            openFileTask = sourceFileOpener.OpenFileAsync(assemblyName, qualifiedClassName, file, line);
        }

        public void BuyMeACoffee()
        {
            process.Start(payPal);
        }

        public void LogIssueOrSuggestion()
        {
            process.Start(githubIssues);
        }

        public void RateAndReview()
        {
            process.Start(marketPlaceRateAndReview);
        }

        public void DocumentFocused()
        {
            FocusCallback();
        }
    }
}