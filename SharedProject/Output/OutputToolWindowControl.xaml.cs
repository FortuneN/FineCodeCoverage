using EnvDTE;
using System.Windows;
using FineCodeCoverage.Engine;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft;
using System;

namespace FineCodeCoverage.Output
{
    /// <summary>
    /// Interaction logic for OutputToolWindowControl.
    /// </summary>
    internal partial class OutputToolWindowControl : UserControl, IScriptInvoker
	{
        private readonly IFCCEngine fccEngine;
        private DTE Dte;
		private Events Events;
		private SolutionEvents SolutionEvents;
		private bool hasLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
        /// </summary>
        public OutputToolWindowControl(ScriptManager scriptManager,IFCCEngine fccEngine)
		{
			InitializeComponent();
            this.Loaded += OutputToolWindowControl_Loaded;
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Dte = (DTE)await OutputToolWindowCommand.Instance.ServiceProvider.GetServiceAsync(typeof(DTE));
				Assumes.Present(Dte);
				Events = Dte.Events;
				SolutionEvents = Events.SolutionEvents;
				SolutionEvents.AfterClosing += () => Clear(false);
			});
			FCCOutputBrowser.ObjectForScripting = scriptManager;
			scriptManager.ScriptInvoker = this;
			
			fccEngine.UpdateOutputWindow += (args) =>
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					if (string.IsNullOrWhiteSpace(args?.HtmlContent))
					{
						Clear(true);
						return;
					}
					
					FCCOutputBrowser.NavigateToString(args.HtmlContent);
				});
			};
			
			
            this.fccEngine = fccEngine;
        }

        private void OutputToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasLoaded)
            {
				Clear(true);
				hasLoaded = true;
				FCCOutputBrowser.Visibility = Visibility.Visible;
            }
        }

        public object InvokeScript(string scriptName, params object[] args)
        {
			if (FCCOutputBrowser.Document != null)
			{
				return FCCOutputBrowser.InvokeScript(scriptName, args);
			}
            return null;
		}

        private void Clear(bool withHistory)
		{
			var report = fccEngine.BlankReport(withHistory);
			FCCOutputBrowser.NavigateToString(report);
		}
	}
}