using FineCodeCoverage.Core.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using System.Xml.XPath;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IUserRunSettingsService))]
    internal class UserRunSettingsService : IUserRunSettingsService
    {
        private IBuiltInRunSettingsTemplate builtInRunSettingsTemplate;
        private readonly IFileUtil fileUtil;

        [ImportingConstructor]
        public UserRunSettingsService(IFileUtil fileUtil)
        {
            this.fileUtil = fileUtil;
        }

        #region suitability
        public (bool Suitable, bool SpecifiedMsCodeCoverage) CheckUserRunSettingsSuitability(IEnumerable<string> userRunSettingsFiles, bool useMsCodeCoverage)
        {
            var specifiedMsCodeCoverage = false;
            foreach (var userRunSettingsFile in userRunSettingsFiles)
            {
                var (suitable, projectSpecifiedMsCodeCoverage) = ValidateUserRunSettings(fileUtil.ReadAllText(userRunSettingsFile), useMsCodeCoverage);
                if (!suitable)
                {
                    return (false, false);
                }
                if (projectSpecifiedMsCodeCoverage)
                {
                    specifiedMsCodeCoverage = true;
                }
            }

            return (true, specifiedMsCodeCoverage);
        }

        internal static (bool Suitable, bool SpecifiedMsCodeCoverage) ValidateUserRunSettings(string runSettings, bool useMsCodeCoverage)
        {
            try
            {
                var runSettingsDoc = XDocument.Parse(runSettings);
                var dataCollectorsElement = runSettingsDoc.GetStrictDescendant("RunSettings/DataCollectionRunSettings/DataCollectors");
                if (dataCollectorsElement == null)
                {
                    return (useMsCodeCoverage, false);
                }

                var msDataCollectorElement = RunSettingsHelper.FindMsDataCollector(dataCollectorsElement);

                if (msDataCollectorElement == null)
                {
                    return (useMsCodeCoverage, false);
                }

                if (HasCoberturaFormat(msDataCollectorElement))
                {
                    return (true, true);
                }

                return (useMsCodeCoverage, true);
            }
            catch
            {
                return (false, false);
            }
        }

        private static bool HasCoberturaFormat(XElement msDataCollectorElement)
        {
            var formatElement = msDataCollectorElement.GetStrictDescendant("Configuration/Format");
            if (formatElement == null)
            {
                return false;
            }
            return formatElement.Value == "Cobertura";
        }

        #endregion

        public IXPathNavigable AddFCCRunSettings(IBuiltInRunSettingsTemplate builtInRunSettingsTemplate, IRunSettingsTemplateReplacements replacements, IXPathNavigable inputRunSettingDocument)
        {
            this.builtInRunSettingsTemplate = builtInRunSettingsTemplate;
            var navigator = inputRunSettingDocument.CreateNavigator();
            navigator.MoveToFirstChild();
            var clonedNavigator = navigator.Clone();
            ConfigureRunConfiguration(navigator, replacements);
            EnsureMsDataCollector(clonedNavigator, replacements);
            return navigator;
        }

        private void ConfigureRunConfiguration(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var movedToRunConfiguration = xpathNavigator.MoveToChild("RunConfiguration", "");
            if (movedToRunConfiguration)
            {
                if (!xpathNavigator.HasChild("TestAdaptersPaths"))
                {
                    xpathNavigator.AppendChild(builtInRunSettingsTemplate.TestAdaptersPathElement);
                }
                // todo ResultsDirectory ?

            }
            else
            {
                xpathNavigator.PrependChild(builtInRunSettingsTemplate.RunConfigurationElement);
                xpathNavigator.MoveToChild("RunConfiguration", "");
            }

            xpathNavigator.OuterXml = builtInRunSettingsTemplate.Replace(xpathNavigator.OuterXml, replacements);
        }

        private void EnsureMsDataCollector(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var addedMsDataCollector = true;
            var movedToDataCollectionRunSettings = xpathNavigator.MoveToChild("DataCollectionRunSettings", "");
            if (movedToDataCollectionRunSettings)
            {
                var movedToDataCollectors = xpathNavigator.MoveToChild("DataCollectors", "");
                if (movedToDataCollectors)
                {
                    XPathNavigator msDataCollectorNavigator = MoveToMsDataCollectorFromDataCollectors(xpathNavigator);

                    if (msDataCollectorNavigator != null)
                    {
                        addedMsDataCollector = false;
                        FixUpMsDataCollector(msDataCollectorNavigator, replacements);
                    }
                    else
                    {
                        xpathNavigator.AppendChild(builtInRunSettingsTemplate.MsDataCollectorElement);
                    }
                }
                else
                {
                    xpathNavigator.AppendChild(builtInRunSettingsTemplate.DataCollectorsElement);
                }
            }
            else
            {
                xpathNavigator.AppendChild(builtInRunSettingsTemplate.DataCollectionRunSettingsElement);
            }

            if (addedMsDataCollector)
            {
                xpathNavigator.MoveToRoot();
                var dataCollectorsNavigator = xpathNavigator.SelectSingleNode("/RunSettings/DataCollectionRunSettings/DataCollectors");
                var msDataCollectorNavigator = MoveToMsDataCollectorFromDataCollectors(dataCollectorsNavigator);
                ReplaceExcludesIncludes(msDataCollectorNavigator, replacements);
            }

        }

        private XPathNavigator MoveToMsDataCollectorFromDataCollectors(XPathNavigator navigator)
        {
            var friendlyNameXPath = $"{RunSettingsHelper.FriendlyNameAttributeName}='{RunSettingsHelper.MsDataCollectorFriendlyName}'";
            var uriXPath = $"{RunSettingsHelper.UriAttributeName}='{RunSettingsHelper.MsDataCollectorUri}'";

            return navigator.SelectSingleNode($"DataCollector[@{friendlyNameXPath} or @{uriXPath}]");
        }

        private void ReplaceExcludesIncludes(XPathNavigator msDataCollectorNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var toReplace = msDataCollectorNavigator.OuterXml;
            var replaced = builtInRunSettingsTemplate.Replace(toReplace, replacements);
            msDataCollectorNavigator.OuterXml = replaced;
        }

        private void FixUpMsDataCollector(XPathNavigator navigator, IRunSettingsTemplateReplacements replacements)
        {
            EnsureCorrectCoberturaFormat(navigator);
            ReplaceExcludesIncludes(navigator.Clone(), replacements);
        }

        private void EnsureCorrectCoberturaFormat(XPathNavigator navigator)
        {
            var movedToConfiguration = navigator.MoveToChild("Configuration", "");
            if (movedToConfiguration)
            {
                var movedToFormat = navigator.MoveToChild("Format", "");
                if (movedToFormat)
                {
                    if (navigator.InnerXml != "Cobertura")
                    {
                        navigator.InnerXml = "Cobertura";
                    }
                }
                else
                {
                    navigator.AppendChild("<Format>Cobertura</Format>");
                }
            }
            else
            {
                navigator.AppendChild("<Configuration><Format>Cobertura</Format></Configuration>");
            }
        }
    }

}
