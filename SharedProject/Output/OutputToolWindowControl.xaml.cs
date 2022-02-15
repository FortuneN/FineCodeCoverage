using EnvDTE;
using System.Windows;
using FineCodeCoverage.Engine;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft;
using System;
using System.Windows.Media;

namespace FineCodeCoverage.Output
{
	public class FontDetails
	{
		public FontDetails(double size, FontFamily fontFamily)
		{
			Size = size;
			Family = fontFamily;
		}
		public double Size { get; }

		public FontFamily Family { get; }
	}

	public class EnvironmentFont : DependencyObject
	{
		
		private static DependencyProperty EnvironmentFontSizeProperty;

		private static DependencyProperty EnvironmentFontFamilyProperty;

		private double Size { get; set; }

		private FontFamily Family { get; set; }


		public event EventHandler<FontDetails> Changed;

		public void Initialize(FrameworkElement frameworkElement)
		{
			RegisterDependencyProperties(frameworkElement.GetType());
			frameworkElement.SetResourceReference(EnvironmentFontSizeProperty, VsFonts.EnvironmentFontSizeKey);
			frameworkElement.SetResourceReference(EnvironmentFontFamilyProperty, VsFonts.EnvironmentFontFamilyKey);
		}

		private void RegisterDependencyProperties(Type controlType)
		{
			EnvironmentFontSizeProperty = DependencyProperty.Register("EnvironmentFontSize", typeof(double), controlType, new PropertyMetadata((obj, args) =>
			{
				Size = (double)args.NewValue;
				ValueChanged();
			}));

			EnvironmentFontFamilyProperty = DependencyProperty.Register("EnvironmentFontFamily", typeof(FontFamily), controlType, new PropertyMetadata((obj, args) =>
			{
				Family = (FontFamily)args.NewValue;
				ValueChanged();
			}));
		}

		private void ValueChanged()
		{
			if (Family != null && Size != default)
			{
				Changed?.Invoke(this, new FontDetails(Size, Family));
			}
		}
	}
	
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
			fccEngine.Dpi = VisualTreeHelper.GetDpi(this);
			var environmentFont = new EnvironmentFont();
			environmentFont.Changed += (sender, fontDetails) =>
			{
				fccEngine.EnvironmentFontDetails = fontDetails;
			};
			environmentFont.Initialize(this);
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

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
		{
			base.OnDpiChanged(oldDpi, newDpi);
			fccEngine.Dpi = newDpi;
		}

		private void OutputToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
			if (!hasLoaded)
			{
				fccEngine.ReadyForReport();
				FCCOutputBrowser.Visibility = Visibility.Visible;
				hasLoaded = true;
			}
        }

        public object InvokeScript(string scriptName, params object[] args)
        {
			if (FCCOutputBrowser.Document != null)
			{
				try
				{
					// Can use FCCOutputBrowser.IsLoaded but 
					// it is possible for this to be successful when IsLoaded false.
					return FCCOutputBrowser.InvokeScript(scriptName, args);
				}
				catch { 
					// todo what to do about missed 
				}
			}
            return null;
		}
	}
}