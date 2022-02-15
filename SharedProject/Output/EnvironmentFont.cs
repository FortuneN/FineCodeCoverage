using Microsoft.VisualStudio.Shell;
using System;
using System.Windows;
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
}
