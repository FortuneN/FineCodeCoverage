using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage
{
    [Export(typeof(IUserRunSettingsService))]
    internal class UserRunSettingsService : IUserRunSettingsService
    {
        private readonly IRunSettingsTemplate runSettingsTemplate;
        private readonly IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory;
        private readonly IFileUtil fileUtil;
        private XDocument runSettingsDoc;
        private string fccMsTestAdapterPath;

        private class UserRunSettingsAnalysisResult : IUserRunSettingsAnalysisResult
        {
            public bool Suitable { get; set; }

            public bool SpecifiedMsCodeCoverage { get; set; }

            public List<ICoverageProject> ProjectsWithFCCMsTestAdapter { get; set; } = new List<ICoverageProject>();
        }

        [ImportingConstructor]
        public UserRunSettingsService(
            IFileUtil fileUtil, 
            IRunSettingsTemplate runSettingsTemplate, 
            IRunSettingsTemplateReplacementsFactory runSettingsTemplateReplacementsFactory
        )
        {
            this.fileUtil = fileUtil;
            this.runSettingsTemplate = runSettingsTemplate;
            this.runSettingsTemplateReplacementsFactory = runSettingsTemplateReplacementsFactory;
        }

        #region analysis
        public IUserRunSettingsAnalysisResult Analyse(IEnumerable<ICoverageProject> coverageProjectsWithRunSettings, bool useMsCodeCoverage, string fccMsTestAdapterPath)
        {
            this.fccMsTestAdapterPath = fccMsTestAdapterPath;
            List<ICoverageProject> projectsWithFCCMsTestAdapter = new List<ICoverageProject>();
            var specifiedMsCodeCoverage = false;
            foreach (var coverageProject in coverageProjectsWithRunSettings)
            {
                var (suitable, projectSpecifiedMsCodeCoverage) = ValidateUserRunSettings(coverageProject.RunSettingsFile, useMsCodeCoverage);
                
                if (!suitable)
                {
                    return new UserRunSettingsAnalysisResult();
                }

                if (projectSpecifiedMsCodeCoverage)
                {
                    specifiedMsCodeCoverage = true;
                }

                if (ProjectHasFCCMsTestAdapter())
                {
                    projectsWithFCCMsTestAdapter.Add(coverageProject);
                }
            }

            return new UserRunSettingsAnalysisResult { Suitable = true,SpecifiedMsCodeCoverage = specifiedMsCodeCoverage, ProjectsWithFCCMsTestAdapter = projectsWithFCCMsTestAdapter };
        }

        private bool ProjectHasFCCMsTestAdapter()
        {
            var testAdaptersPathElement = runSettingsDoc.GetStrictDescendant("RunSettings/RunConfiguration/TestAdaptersPaths");
            // given that add a replaceable
            if ( testAdaptersPathElement == null)
            {
                return true;
            }

            var testAdaptersPaths = testAdaptersPathElement.Value;
            if (runSettingsTemplate.HasReplaceableTestAdapter(testAdaptersPaths))
            {
                return true;
            }

            var paths = testAdaptersPaths.Split(';');
            return paths.Any(path => path == fccMsTestAdapterPath);
        }

        internal (bool Suitable, bool SpecifiedMsCodeCoverage) ValidateUserRunSettings(string userRunSettingsFile, bool useMsCodeCoverage)
        {
            try
            {
                var runSettings = fileUtil.ReadAllText(userRunSettingsFile);
                runSettingsDoc = XDocument.Parse(runSettings);
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

        #region AddFCCRunSettings
        
        public IXPathNavigable AddFCCRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup, string fccMsTestAdapterPath)
        {
            if (!runSettingsTemplate.FCCGenerated(inputRunSettingDocument))
            {
                return AddFCCRunSettingsActual(inputRunSettingDocument,configurationInfo,userRunSettingsProjectDetailsLookup,fccMsTestAdapterPath);
            }
            return null;
        }

        private IXPathNavigable AddFCCRunSettingsActual(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, Dictionary<string, IUserRunSettingsProjectDetails> userRunSettingsProjectDetailsLookup, string fccMsTestAdapterPath)
        {
            var navigator = inputRunSettingDocument.CreateNavigator();
            navigator.MoveToFirstChild();
            var clonedNavigator = navigator.Clone();
            var replacements = runSettingsTemplateReplacementsFactory.Create(
                configurationInfo.TestContainers, 
                userRunSettingsProjectDetailsLookup, 
                fccMsTestAdapterPath
            );
            EnsureTestAdaptersPathsAndReplace(navigator, replacements);
            EnsureCorrectMsDataCollectorAndReplace(clonedNavigator, replacements);
            return navigator;
        }

        private void EnsureTestAdaptersPathsAndReplace(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
        {
            var movedToRunConfiguration = xpathNavigator.MoveToChild("RunConfiguration", "");
            if (movedToRunConfiguration)
            {
                if (!xpathNavigator.HasChild("TestAdaptersPaths"))
                {
                    xpathNavigator.AppendChild(runSettingsTemplate.TestAdaptersPathElement);
                }
                // todo ResultsDirectory ?

            }
            else
            {
                xpathNavigator.PrependChild(runSettingsTemplate.RunConfigurationElement);
                xpathNavigator.MoveToChild("RunConfiguration", "");
            }

            var replaced = runSettingsTemplate.Replace(xpathNavigator.OuterXml, replacements);
            xpathNavigator.OuterXml = replaced;
        }

        private void EnsureCorrectMsDataCollectorAndReplace(XPathNavigator xpathNavigator, IRunSettingsTemplateReplacements replacements)
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
                        EnsureCorrectCoberturaFormat(msDataCollectorNavigator);
                        ReplaceExcludesIncludes(msDataCollectorNavigator.Clone(), replacements);
                    }
                    else
                    {
                        xpathNavigator.AppendChild(runSettingsTemplate.MsDataCollectorElement);
                    }
                }
                else
                {
                    xpathNavigator.AppendChild(runSettingsTemplate.DataCollectorsElement);
                }
            }
            else
            {
                xpathNavigator.AppendChild(runSettingsTemplate.DataCollectionRunSettingsElement);
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
            var replaced = runSettingsTemplate.Replace(toReplace, replacements);
            msDataCollectorNavigator.OuterXml = replaced;
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
        
        #endregion
    }

}
