using FineCodeCoverage.Engine.MsTestPlatform;
using Moq;
using NUnit.Framework;
using System.Xml;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.IO;
using System.Globalization;
using System.Xml.XPath;
using System;
using System.Collections.Generic;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using AutoMoq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverageRunSettingsService_IRunSettingsService_Tests
    {
        private AutoMoqer autoMocker;
        private MsCodeCoverageRunSettingsService msCodeCoverageRunSettingsService;
        [SetUp]
        public void CreateSut()
        {
            autoMocker = new AutoMoqer();
            msCodeCoverageRunSettingsService = autoMocker.Create<MsCodeCoverageRunSettingsService>();
        }

        [TestCase(RunSettingConfigurationInfoState.Discovery)]
        [TestCase(RunSettingConfigurationInfoState.None)]
        public void Should_Not_Process_When_Not_Test_Execution(RunSettingConfigurationInfoState state)
        {
            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.RequestState).Returns(state);
            Assert.IsNull(msCodeCoverageRunSettingsService.AddRunSettings(null, mockRunSettingsConfigurationInfo.Object, null));
        }

        [Test]
        public void Should_Not_Process_When_Runsettings_Created_From_Template()
        {
            var xPathNavigable = new Mock<IXPathNavigable>().Object;
            autoMocker.GetMock<IBuiltInRunSettingsTemplate>().Setup(brst => brst.FCCGenerated(xPathNavigable)).Returns(true);
            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.RequestState).Returns(RunSettingConfigurationInfoState.Execution);
            Assert.IsNull(msCodeCoverageRunSettingsService.AddRunSettings(xPathNavigable, mockRunSettingsConfigurationInfo.Object, null));
        }

        [Test]
        public void Should_UserRunSettingsService_AddFCCRunSettings_When_Execute_And_UserRunSettings()
        {
            var inputRunSettingDocument = new Mock<IXPathNavigable>().Object;
            var mockBuiltInRunSettingsTemplate = autoMocker.GetMock<IBuiltInRunSettingsTemplate>();
            mockBuiltInRunSettingsTemplate.Setup(brst => brst.FCCGenerated(inputRunSettingDocument)).Returns(false);

            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.RequestState).Returns(RunSettingConfigurationInfoState.Execution);
            var testContainers = new List<ITestContainer>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.TestContainers).Returns(testContainers);

            var ct = CancellationToken.None;
            autoMocker.GetMock<IToolFolder>().Setup(toolFolder => toolFolder.EnsureUnzipped(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ZipDetails>(), ct)).Returns("ZipDestination");
            var msCodeCoveragePath = Path.Combine("ZipDestination", "build", "netstandard1.0");
            msCodeCoverageRunSettingsService.Initialize(null, null, ct);

            // collecting
            var mockCoverageProject = new Mock<ICoverageProject>();
            mockCoverageProject.Setup(cp => cp.TestDllFile).Returns("Test.dll");
            var userRunSettingsProjectDetails = msCodeCoverageRunSettingsService.SetUserRunSettingsProjectDetails(new List<ICoverageProject>
            {
                mockCoverageProject.Object
            });

            var runSettingsTemplateReplacements = new Mock<IRunSettingsTemplateReplacements>().Object;
            var mockRunSettingsTemplateReplacementsFactory = autoMocker.GetMock<IRunSettingsTemplateReplacementsFactory>();
            mockRunSettingsTemplateReplacementsFactory.Setup(f => f.Create(testContainers, userRunSettingsProjectDetails, msCodeCoveragePath)).Returns(runSettingsTemplateReplacements);

            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            var fccRunSettingDocument = new Mock<IXPathNavigable>().Object;
            mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.AddFCCRunSettings(mockBuiltInRunSettingsTemplate.Object, runSettingsTemplateReplacements, inputRunSettingDocument)).Returns(fccRunSettingDocument);

            Assert.AreSame(fccRunSettingDocument,msCodeCoverageRunSettingsService.AddRunSettings(inputRunSettingDocument, mockRunSettingsConfigurationInfo.Object, null));
        }
    }
}
