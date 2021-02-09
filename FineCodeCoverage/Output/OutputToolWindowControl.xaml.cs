using EnvDTE;
using System.IO;
using System.Windows;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Engine;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft;
using System;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Output
{
	/// <summary>
	/// Interaction logic for OutputToolWindowControl.
	/// </summary>
	internal partial class OutputToolWindowControl : UserControl
	{
		private DTE Dte;
		private Events Events;
		private SolutionEvents SolutionEvents;

		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
		/// </summary>
		[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
		public OutputToolWindowControl(ScriptManager scriptManager,IFCCEngine fccEngine)
		{
			InitializeComponent();

			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Dte = (DTE)await OutputToolWindowCommand.Instance.ServiceProvider.GetServiceAsync(typeof(DTE));
				Assumes.Present(Dte);
				Events = Dte.Events;
				SolutionEvents = Events.SolutionEvents;
				SolutionEvents.Opened += () => Clear();
				SolutionEvents.AfterClosing += () => Clear();
			});

			FCCOutputBrowser.ObjectForScripting = scriptManager;
			
			fccEngine.UpdateOutputWindow += (args) =>
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					if (string.IsNullOrWhiteSpace(args?.HtmlContent))
					{
						Clear();
						return;
					}
					
					FCCOutputBrowser.NavigateToString(args.HtmlContent);
					FCCOutputBrowser.Visibility = Visibility.Visible;
				});
			};
		}

		private void Clear()
		{
			FCCOutputBrowser.Visibility = Visibility.Hidden;
		}
	}
    
    [Export]
    [ComVisible(true)]
    public class ScriptManager
    {
        private DTE _dte;
        private readonly SVsServiceProvider serviceProvider;
        private readonly IFCCEngine fccEngine;

        public Action FocusCallback { get; set; }

        [ImportingConstructor]
        internal ScriptManager(SVsServiceProvider serviceProvider,IFCCEngine fccEngine)
        {
            this.serviceProvider = serviceProvider;
            this.fccEngine = fccEngine;
        }
        private DTE Dte {
            get
            {
                if(_dte == null)
                {
                    ThreadHelper.JoinableTaskFactory.Run(async () =>
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        _dte = (DTE)serviceProvider.GetService(typeof(DTE));
                    });
                }
                return _dte;
            }
        }

        [SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter")]
        public void OpenFile(string assemblyName, string qualifiedClassName, int file, int line)
        {
            // Note : There may be more than one file; e.g. in the case of partial classes

            var sourceFiles = fccEngine.GetSourceFiles(assemblyName, qualifiedClassName);

            if (!sourceFiles.Any())
            {
                var message = $"Source File(s) Not Found : [{ assemblyName }]{ qualifiedClassName }";
                Logger.Log(message);
                MessageBox.Show(message);
                return;
            }

            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                Dte.MainWindow.Activate();

                foreach (var sourceFile in sourceFiles)
                {
                    Dte.ItemOperations.OpenFile(sourceFile, Constants.vsViewKindCode);

                    if (line != 0)
                    {
                        ((TextSelection)Dte.ActiveDocument.Selection).GotoLine(line, false);
                    }
                }
            });
        }

        public void BuyMeACoffee()
        {
            System.Diagnostics.Process.Start("https://paypal.me/FortuneNgwenya");
        }

        public void LogIssueOrSuggestion()
        {
            System.Diagnostics.Process.Start("https://github.com/FortuneN/FineCodeCoverage/issues");
        }

        public void RateAndReview()
        {
            System.Diagnostics.Process.Start("https://marketplace.visualstudio.com/items?itemName=FortuneNgwenya.FineCodeCoverage&ssr=false#review-details");
        }

        public void DocumentFocused()
        {
            FocusCallback();
        }
    }
}