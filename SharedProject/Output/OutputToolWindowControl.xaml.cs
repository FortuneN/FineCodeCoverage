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
		private bool hasLoaded;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
        /// </summary>
        public OutputToolWindowControl(ScriptManager scriptManager,IFCCEngine fccEngine)
		{
			InitializeComponent();
            this.Loaded += OutputToolWindowControl_Loaded;

			FCCOutputBrowser.ObjectForScripting = scriptManager;
			scriptManager.ScriptInvoker = this;
			
			fccEngine.UpdateOutputWindow += (args) =>
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					FCCOutputBrowser.NavigateToString(args.HtmlContent);
				});
			};
			
			
            this.fccEngine = fccEngine;
        }

        private void OutputToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!hasLoaded)
            {
				fccEngine.ReadyForReport();
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
	}
}