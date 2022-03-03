using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using System.Windows.Media;
using FineCodeCoverage.Core.Utilities;
using System;

namespace FineCodeCoverage.Output
{
	/// <summary>
	/// Interaction logic for OutputToolWindowControl.
	/// </summary>
	internal partial class OutputToolWindowControl : 
		UserControl, IListener<NewReportMessage>, IListener<InvokeScriptMessage>, IListener<ObjectForScriptingMessage>
	{
        private readonly IEventAggregator eventAggregator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OutputToolWindowControl"/> class.
        /// </summary>
        public OutputToolWindowControl(IEventAggregator eventAggregator)
		{
			this.eventAggregator = eventAggregator;
			InitializeComponent();
			eventAggregator.SendMessage(new DpiChangedMessage { DpiScale = VisualTreeHelper.GetDpi(this) });
			var environmentFont = new EnvironmentFont();
			environmentFont.Changed += (sender, fontDetails) =>
			{
				eventAggregator.SendMessage(new EnvironmentFontDetailsChangedMessage { FontDetails = fontDetails });
			};
			environmentFont.Initialize(this);
			this.Loaded += OutputToolWindowControl_Loaded;

			eventAggregator.AddListener(this);
			eventAggregator.SendMessage(new ReadyForReportMessage());
		}

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
		{
			base.OnDpiChanged(oldDpi, newDpi);
			eventAggregator.SendMessage(new DpiChangedMessage { DpiScale = newDpi });
		}

		private void OutputToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
			FCCOutputBrowser.Visibility = Visibility.Visible;
        }

        private void InvokeScript(string scriptName, params object[] args)
        {
			try
			{
				if (FCCOutputBrowser.Document != null)
				{
					try
					{
						// Can use FCCOutputBrowser.IsLoaded but 
						// it is possible for this to be successful when IsLoaded false.
						FCCOutputBrowser.InvokeScript(scriptName, args);
					}
					catch
					{
						// missed are not important.  Important go through NewReportMessage and NavigateToString 
					}
				}
			}
			catch (ObjectDisposedException) { }
		}

        public void Handle(NewReportMessage message)
        {
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				FCCOutputBrowser.NavigateToString(message.Report);
			});
		}

        public void Handle(InvokeScriptMessage message)
        {
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				InvokeScript(message.ScriptName, message.Arguments);
			});
		}

        public void Handle(ObjectForScriptingMessage message)
        {
			FCCOutputBrowser.ObjectForScripting = message.ObjectForScripting;
		}
    }
}