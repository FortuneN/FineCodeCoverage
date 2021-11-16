using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    [Export(typeof(IThemeResourceKeyProvider))]
    internal class ThemeResourceKeyProvider : IThemeResourceKeyProvider
    {
        private XElement root;
        public ThemeResourceKeyProvider()
        {
            var fccExtensionsDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var fccResourcesDirectory = Path.Combine(fccExtensionsDirectory, "Resources");
            var reportPartsPath = Path.Combine(fccResourcesDirectory, "reportparts.xml");
            if (!File.Exists(reportPartsPath))
            {
                throw new Exception($"Cannot find {reportPartsPath}");
            }
            root = XElement.Load(reportPartsPath);
        }

        public ThemeResourceKey Provide(string reportPart)
        {
            var matchingElement = root.Elements("ReportPart").FirstOrDefault(reportPartElement => reportPartElement.Attribute("Name").Value == reportPart);
            if (matchingElement != null)
            {
                // probably should change the attribute name
                var themeResourceKeyString = matchingElement.Attribute("SelectedThemeColourName").Value;
                var parts = themeResourceKeyString.Split('.');
                if(parts.Length != 2 || String.IsNullOrWhiteSpace(parts[1]))
                {
                    throw new Exception($"report part {reportPart} not of format ClassName.PropertyName");
                }
                
                
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
                if (classType != null)
                {
                    var themeResourceKeyProperty = classType.GetProperty(parts[1], BindingFlags.Public | BindingFlags.Static);
                    if (themeResourceKeyProperty != null)
                    {
                        var themeResourceKey = themeResourceKeyProperty.GetValue(null);
                        if(themeResourceKey is ThemeResourceKey)
                        {
                            return themeResourceKey as ThemeResourceKey;
                        }
                        else
                        {
                            throw new Exception($"report part {reportPart}: property {parts[1]} is not of type ThemeResourceKey");
                        }
                    }
                    else
                    {
                        throw new Exception($"report part {reportPart}: class {@class} does not have property {parts[1]}");
                    }
                }
                else
                {
                    throw new Exception($"report part {reportPart} class name should be EnvironmentColors | CommonControlsColors");
                }
                
            }
            else
            {
                throw new Exception($"Cannot find report part {reportPart}");
            }
        }
    }

}
