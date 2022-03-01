using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Reflection;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    [Export(typeof(IReportColoursProvider))]
    internal class ReportColoursProvider : IReportColoursProvider
    {
        private readonly IThemeResourceKeyProvider themeResourceKeyProvider;

        public event EventHandler<IReportColours> ColoursChanged;

        private static readonly PropertyInfo[] propertyInfos;
        static ReportColoursProvider()
        {
            propertyInfos = typeof(ReportColours).GetProperties();
        }

        private ReportColours lastReportColours;

        [ImportingConstructor]
        public ReportColoursProvider(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider,
            IThemeResourceKeyProvider themeResourceKeyProvider
            )
        {
            this.themeResourceKeyProvider = themeResourceKeyProvider;
            var colorThemeService = serviceProvider.GetService(typeof(SVsColorThemeService));
            lastReportColours = GetReportColours();
            VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        }

        private void VSColorTheme_ThemeChanged(ThemeChangedEventArgs e)
        {
            var newColours = GetReportColours();
            if (lastReportColours == null || !SameColours(lastReportColours, newColours))
            {
                lastReportColours = newColours;
                ColoursChanged?.Invoke(this, newColours);
            }

        }

        private static bool SameColours(ReportColours oldColours, ReportColours newColours)
        {
            var same = true;
            foreach (var propertyInfo in propertyInfos)
            {
                var oldColor = (Color)propertyInfo.GetValue(oldColours);
                var newColor = (Color)propertyInfo.GetValue(newColours);
                same = oldColor.ToString() == newColor.ToString();
                if (!same)
                {
                    break;
                }
            }
            return same;
        }

        public IReportColours GetColours()
        {
            return GetReportColours();
        }

        private ReportColours GetReportColours()
        {
            var reportColours = new ReportColours();
            foreach (var propertyInfo in propertyInfos)
            {
                var color = VSColorTheme.GetThemedColor(themeResourceKeyProvider.Provide(propertyInfo.Name));
                propertyInfo.SetValue(reportColours, color);
            }
            return reportColours;
        }
    }
}
