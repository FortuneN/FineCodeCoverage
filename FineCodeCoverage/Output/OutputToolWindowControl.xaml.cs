using System;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using EnvDTE;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Windows;
using System.Collections.Generic;
using FineCodeCoverage.Engine;

namespace FineCodeCoverage.Output
{
	/// <summary>
	/// Interaction logic for OutputToolWindowControl.
	/// </summary>
	public partial class OutputToolWindowControl : UserControl
	{
		private static DTE Dte;
		private static Events Events;
		private static ScriptManager ScriptManager;
		private static SolutionEvents SolutionEvents;
		private static OutputToolWindowControl Instance;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
		/// </summary>
		[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
		public OutputToolWindowControl()
		{
			Instance = this;

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
		}

		public static void Clear()
		{
			Instance.FCCOutputBrowser.Visibility = Visibility.Hidden;
		}
		
		[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
		public static void SetFilePath(string filePath)
		{
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				if (string.IsNullOrWhiteSpace(filePath))
				{
					Clear();
				}
				else
				{
					Instance.FCCOutputBrowser.NavigateToString(File.ReadAllText(filePath));
					Instance.FCCOutputBrowser.Visibility = Visibility.Visible;
				}
			});
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
		public void OpenFile(string htmlFilePath, int file, int line)
		{
			var htmlFileName = Path.GetFileNameWithoutExtension(htmlFilePath);
			var csFileName = FCCEngine.GetSourceFileNameFromReportGeneratorHtmlFileName(htmlFileName);

			if (string.IsNullOrWhiteSpace(csFileName))
			{
				var message = $"Not Found : { htmlFileName }";
				Logger.Log(message);
				MessageBox.Show(message);
				return;
			}

			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_dte.MainWindow.Activate();

				_dte.ItemOperations.OpenFile(csFileName, Constants.vsViewKindCode);

				if (line != 0)
				{
					((TextSelection)_dte.ActiveDocument.Selection).GotoLine(line, false);
				}
			});
		}

		public void BuyMeABeer()
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