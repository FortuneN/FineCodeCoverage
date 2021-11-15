using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    internal interface IThemeResourceKeyProvider
    {
        ThemeResourceKey Provide(string reportPart);
    }

    // could watch
    [Export(typeof(IThemeResourceKeyProvider))]
    internal class ThemeResourceKeyProvider : IThemeResourceKeyProvider
    {
        private XElement root;
        public ThemeResourceKeyProvider()
        {
            var fccExtensionsDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fccResourcesDirectory = Path.Combine(fccExtensionsDirectory, "Resources");
            var reportPartsPath = Path.Combine(fccResourcesDirectory, "reportparts.xml");
            root = XElement.Load(reportPartsPath);
        }
        public ThemeResourceKey Provide(string reportPart)
        {
            var matchingElement = root.Elements("ReportPart").First(reportPartElement => reportPartElement.Attribute("Name").Value == reportPart);
            if (matchingElement != null)
            {
                // probably should change the attribute name
                var themeResourceKeyString = matchingElement.Attribute("SelectedThemeColourName").Value;
                var parts = themeResourceKeyString.Split('.');
                if(parts.Length == 2 && !String.IsNullOrWhiteSpace(parts[1]))
                {
                    var @class = parts[0];
                    Type classType = null;
                    switch (@class)
                    {
                        case "EnvironmentColors":
                            classType = typeof(EnvironmentColors);
                            break;
                        case "CommonControlsColors":
                            classType = typeof(CommonControlsColors);
                            break;
                    }
                    if(classType != null)
                    {
                        //try ?
                        var themeResourceKeyProperty = classType.GetProperty(parts[1], BindingFlags.Public | BindingFlags.Static);
                        if(themeResourceKeyProperty != null)
                        {
                            return themeResourceKeyProperty.GetValue(null) as ThemeResourceKey;
                        }
                    }
                }
            }
            return null;
        }
    }

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
