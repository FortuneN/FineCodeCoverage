using FineCodeCoverage.Options;
using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(ICoverageProjectSettingsManager))]
    internal class CoverageProjectSettingsManager : ICoverageProjectSettingsManager
    {
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ILogger logger;
        private readonly IVsBuildFCCSettingsProvider vsBuildFCCSettingsProvider;

        [ImportingConstructor]
        public CoverageProjectSettingsManager(
            IAppOptionsProvider appOptionsProvider,
            ILogger logger,
            IVsBuildFCCSettingsProvider vsBuildFCCSettingsProvider
        )
        {
            this.appOptionsProvider = appOptionsProvider;
            this.logger = logger;
            this.vsBuildFCCSettingsProvider = vsBuildFCCSettingsProvider;
        }

        private bool TypeMatch(Type type, params Type[] otherTypes)
        {
            return (otherTypes ?? new Type[0]).Any(ot => type == ot);
        }

        private async Task<XElement> GetSettingsElementAsync(ICoverageProject coverageProject)
        {
            var settingsElement = SettingsElementFromFCCLabelledPropertyGroup(coverageProject);
            if (settingsElement == null)
            {
                settingsElement = await vsBuildFCCSettingsProvider.GetSettingsAsync(coverageProject.Id);
            }
            return settingsElement;
        }

        private XElement SettingsElementFromFCCLabelledPropertyGroup(ICoverageProject coverageProject)
        {
            /*
            <PropertyGroup Label="FineCodeCoverage">
                ...
            </PropertyGroup>
            */
            return coverageProject.ProjectFileXElement.XPathSelectElement($"/PropertyGroup[@Label='{Vsix.Code}']");
        }

        public async Task<IAppOptions> GetSettingsAsync(ICoverageProject coverageProject)
        {
            // get global settings

            var settings = appOptionsProvider.Get();

            var settingsElement = await GetSettingsElementAsync(coverageProject);

            if (settingsElement != null)
            {
                foreach (var property in settings.GetType().GetProperties())
                {
                    try
                    {
                        var xproperty = settingsElement.Descendants().FirstOrDefault(x => x.Name.LocalName.Equals(property.Name, StringComparison.OrdinalIgnoreCase));

                        if (xproperty == null)
                        {
                            continue;
                        }

                        var strValue = xproperty.Value;

                        if (string.IsNullOrWhiteSpace(strValue))
                        {
                            continue;
                        }

                        var strValueArr = strValue.Split('\n', '\r').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToArray();

                        if (!strValue.Any())
                        {
                            continue;
                        }

                        if (TypeMatch(property.PropertyType, typeof(string)))
                        {
                            property.SetValue(settings, strValueArr.FirstOrDefault());
                        }
                        else if (TypeMatch(property.PropertyType, typeof(string[])))
                        {
                            property.SetValue(settings, strValueArr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(bool), typeof(bool?)))
                        {
                            if (bool.TryParse(strValueArr.FirstOrDefault(), out bool value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(bool[]), typeof(bool?[])))
                        {
                            var arr = strValueArr.Where(x => bool.TryParse(x, out var _)).Select(x => bool.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(int), typeof(int?)))
                        {
                            if (int.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(int[]), typeof(int?[])))
                        {
                            var arr = strValueArr.Where(x => int.TryParse(x, out var _)).Select(x => int.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(short), typeof(short?)))
                        {
                            if (short.TryParse(strValueArr.FirstOrDefault(), out var vaue))
                            {
                                property.SetValue(settings, vaue);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(short[]), typeof(short?[])))
                        {
                            var arr = strValueArr.Where(x => short.TryParse(x, out var _)).Select(x => short.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(long), typeof(long?)))
                        {
                            if (long.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(long[]), typeof(long?[])))
                        {
                            var arr = strValueArr.Where(x => long.TryParse(x, out var _)).Select(x => long.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(decimal), typeof(decimal?)))
                        {
                            if (decimal.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(decimal[]), typeof(decimal?[])))
                        {
                            var arr = strValueArr.Where(x => decimal.TryParse(x, out var _)).Select(x => decimal.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(double), typeof(double?)))
                        {
                            if (double.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(double[]), typeof(double?[])))
                        {
                            var arr = strValueArr.Where(x => double.TryParse(x, out var _)).Select(x => double.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(float), typeof(float?)))
                        {
                            if (float.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(float[]), typeof(float?[])))
                        {
                            var arr = strValueArr.Where(x => float.TryParse(x, out var _)).Select(x => float.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else if (TypeMatch(property.PropertyType, typeof(char), typeof(char?)))
                        {
                            if (char.TryParse(strValueArr.FirstOrDefault(), out var value))
                            {
                                property.SetValue(settings, value);
                            }
                        }
                        else if (TypeMatch(property.PropertyType, typeof(char[]), typeof(char?[])))
                        {
                            var arr = strValueArr.Where(x => char.TryParse(x, out var _)).Select(x => char.Parse(x));
                            if (arr.Any()) property.SetValue(settings, arr);
                        }

                        else
                        {
                            throw new Exception($"Cannot handle '{property.PropertyType.Name}' yet");
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.Log($"Failed to override '{property.Name}' setting", exception);
                    }
                }
            }

            return settings;
        }
    }

}
