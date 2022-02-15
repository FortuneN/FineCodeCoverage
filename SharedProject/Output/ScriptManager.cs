using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Output
{
    public interface IScriptManager
    {
        event EventHandler ClearFCCWindowLogsEvent;
        event EventHandler ShowFCCOutputPaneEvent;
    }

    public class ReportFocusedMessage
    {

    }

    [Export]
    [Export(typeof(IScriptManager))]
    [ComVisible(true)] // do not change the accessibility - needs to be public class
    public class ScriptManager : IScriptManager
    {
        internal const string payPal = "https://paypal.me/FortuneNgwenya";
        internal const string githubIssues = "https://github.com/FortuneN/FineCodeCoverage/issues";
        internal const string marketPlaceRateAndReview = "https://marketplace.visualstudio.com/items?itemName=FortuneNgwenya.FineCodeCoverage&ssr=false#review-details";
        private readonly ISourceFileOpener sourceFileOpener;
        private readonly IProcess process;
        private readonly IEventAggregator eventAggregator;
        internal System.Threading.Tasks.Task openFileTask;
        public event EventHandler ClearFCCWindowLogsEvent;
        public event EventHandler ShowFCCOutputPaneEvent;

        [ImportingConstructor]
        internal ScriptManager(ISourceFileOpener sourceFileOpener, IProcess process, IEventAggregator eventAggregator)
        {
            this.sourceFileOpener = sourceFileOpener;
            this.process = process;
            this.eventAggregator = eventAggregator;
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
            eventAggregator.SendMessage(new ReportFocusedMessage());
        }

        public void ClearFCCWindowLogs()
        {
            ClearFCCWindowLogsEvent?.Invoke(this, EventArgs.Empty);
        }

        public void ShowFCCOutputPane()
        {
            ShowFCCOutputPaneEvent?.Invoke(this, EventArgs.Empty);
        }

    }
}