using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Reflection;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    [Export(typeof(IReportColoursProvider))]
    internal class ReportColoursProvider : IReportColoursProvider
    {
        private readonly IThemeResourceKeyProvider themeResourceKeyProvider;

        public event EventHandler<IReportColours> ColoursChanged;

        private static PropertyInfo[] propertyInfos;
        static ReportColoursProvider()
        {
            propertyInfos = typeof(ReportColours).GetProperties();
        }

        [ImportingConstructor]
        public ReportColoursProvider(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IThemeResourceKeyProvider themeResourceKeyProvider
            )
        {
            var colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
            this.themeResourceKeyProvider = themeResourceKeyProvider;
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            var newColours = GetColours();
            ColoursChanged?.Invoke(this, newColours);
        }

        public IReportColours GetColours()
        {
            var reportColours = new ReportColours();
            foreach(var propertyInfo in propertyInfos)
            {
                var color = VSColorTheme.GetThemedColor(themeResourceKeyProvider.Provide(propertyInfo.Name));
                propertyInfo.SetValue(reportColours, color);
            }
            return reportColours;
        }
    }
}
