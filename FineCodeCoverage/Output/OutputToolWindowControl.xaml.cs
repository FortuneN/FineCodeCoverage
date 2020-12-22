﻿using EnvDTE;
using System.IO;
using System.Windows;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Engine;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FineCodeCoverage.Output
{
	/// <summary>
	/// Interaction logic for OutputToolWindowControl.
	/// </summary>
	public partial class OutputToolWindowControl : UserControl
	{
		private DTE Dte;
		private Events Events;
		private SolutionEvents SolutionEvents;
		private readonly ScriptManager ScriptManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
		/// </summary>
		[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
		public OutputToolWindowControl()
		{
			InitializeComponent();

			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				Dte = (DTE)await OutputToolWindowCommand.Instance.ServiceProvider.GetServiceAsync(typeof(DTE));

				Events = Dte.Events;
				SolutionEvents = Events.SolutionEvents;
				SolutionEvents.Opened += () => Clear();
				SolutionEvents.AfterClosing += () => Clear();
			});

			ScriptManager = new ScriptManager(Dte);
			FCCOutputBrowser.ObjectForScripting = ScriptManager;
			
			TestContainerDiscoverer.UpdateOutputWindow += (sender, args) =>
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

	[ComVisible(true)]
	public class ScriptManager
	{
		private readonly DTE _dte;

		public ScriptManager(DTE dte)
		{
			_dte = dte;
		}

		[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
		[SuppressMessage("Style", "IDE0060:Remove unused parameter")]
		public void OpenFile(string assemblyName, string qualifiedClassName, int file, int line)
		{
			// Note : There may be more than one file; e.g. in the case of partial classes

			var sourceFiles = FCCEngine.GetSourceFiles(assemblyName, qualifiedClassName);

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

				_dte.MainWindow.Activate();

				foreach (var sourceFile in sourceFiles)
				{
					_dte.ItemOperations.OpenFile(sourceFile, Constants.vsViewKindCode);

					if (line != 0)
					{
						((TextSelection)_dte.ActiveDocument.Selection).GotoLine(line, false);
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
	}
}