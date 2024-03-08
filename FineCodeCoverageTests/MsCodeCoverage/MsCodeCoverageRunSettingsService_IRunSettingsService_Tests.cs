using Moq;
using NUnit.Framework;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.IO;
using System.Xml.XPath;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using AutoMoq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System;

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

        [Test]
        public void Should_Have_A_Name()
        {
            Assert.False(string.IsNullOrWhiteSpace(msCodeCoverageRunSettingsService.Name));
        }

        [TestCase(RunSettingConfigurationInfoState.Discovery)]
        [TestCase(RunSettingConfigurationInfoState.None)]
        public void Should_Not_Delegate_To_UserRunSettingsService_When_Not_Test_Execution(RunSettingConfigurationInfoState state)
        {
            SetuserRunSettingsProjectDetailsLookup(false);
            msCodeCoverageRunSettingsService.collectionStatus = MsCodeCoverageCollectionStatus.Collecting;

            ShouldNotDelegateToUserRunSettingsService(state);
        }

        [TestCase(MsCodeCoverageCollectionStatus.NotCollecting)]
        [TestCase(MsCodeCoverageCollectionStatus.Error)]
        public void Should_Not_Delegate_To_UserRunSettingsService_When_Is_Not_Collecting(MsCodeCoverageCollectionStatus status)
        {
            msCodeCoverageRunSettingsService.collectionStatus = status;
            SetuserRunSettingsProjectDetailsLookup(false);
            
            ShouldNotDelegateToUserRunSettingsService(RunSettingConfigurationInfoState.Execution);
        }

        [Test]
        public void Should_Not_Delegate_To_UserRunSettingsService_When_No_User_RunSettings()
        {
            msCodeCoverageRunSettingsService.collectionStatus = MsCodeCoverageCollectionStatus.Collecting;
            SetuserRunSettingsProjectDetailsLookup(true);

            ShouldNotDelegateToUserRunSettingsService(RunSettingConfigurationInfoState.Execution);
        }

        private void ShouldNotDelegateToUserRunSettingsService(RunSettingConfigurationInfoState state)
        {
            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.RequestState).Returns(state);

            autoMocker.GetMock<IUserRunSettingsService>()
                .Setup(userRunSettingsService => userRunSettingsService.AddFCCRunSettings(
                    It.IsAny<IXPathNavigable>(),
                    It.IsAny<IRunSettingsConfigurationInfo>(),
                    It.IsAny<Dictionary<string, IUserRunSettingsProjectDetails>>(),
                    It.IsAny<string>()
                )).Returns(new Mock<IXPathNavigable>().Object);
            Assert.IsNull(msCodeCoverageRunSettingsService.AddRunSettings(null, mockRunSettingsConfigurationInfo.Object, null));
        }



        private void SetuserRunSettingsProjectDetailsLookup(bool empty)
        {
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>();
            if (!empty)
            {
                userRunSettingsProjectDetailsLookup.Add("", null); // an entry
            }
            msCodeCoverageRunSettingsService.userRunSettingsProjectDetailsLookup = userRunSettingsProjectDetailsLookup;
        }

        [Test]
        public void Should_Delegate_To_UserRunSettingsService_With_UserRunSettingsProjectDetailsLookup_And_FCC_Ms_TestAdapter_Path_When_Applicable()
        {
            var inputRunSettingDocument = new Mock<IXPathNavigable>().Object;

            var mockRunSettingsConfigurationInfo = new Mock<IRunSettingsConfigurationInfo>();
            mockRunSettingsConfigurationInfo.Setup(ci => ci.RequestState).Returns(RunSettingConfigurationInfoState.Execution);
            var runSettingsConfigurationInfo = mockRunSettingsConfigurationInfo.Object;

            var fccMsTestAdapter = GetFCCMsTestAdapterPath();

            // IsCollecting would set this
            var userRunSettingsProjectDetailsLookup = new Dictionary<string, IUserRunSettingsProjectDetails>
            {
                { "",null} // an entry
            };
            msCodeCoverageRunSettingsService.userRunSettingsProjectDetailsLookup = userRunSettingsProjectDetailsLookup;
            msCodeCoverageRunSettingsService.collectionStatus = MsCodeCoverageCollectionStatus.Collecting;


            var mockUserRunSettingsService = autoMocker.GetMock<IUserRunSettingsService>();
            var fccRunSettingDocument = new Mock<IXPathNavigable>().Object;
            var addFCCRunSettingsSetup = mockUserRunSettingsService.Setup(userRunSettingsService => userRunSettingsService.AddFCCRunSettings(
                inputRunSettingDocument, 
                runSettingsConfigurationInfo,
                It.IsAny<Dictionary<string,IUserRunSettingsProjectDetails>>(), 
                fccMsTestAdapter)
            ).Returns(fccRunSettingDocument);

            Assert.AreSame(fccRunSettingDocument, msCodeCoverageRunSettingsService.AddRunSettings(inputRunSettingDocument, mockRunSettingsConfigurationInfo.Object, null));

            var addFCCRunSettingsInvocation = mockUserRunSettingsService.Invocations[0];
            Assert.AreSame(userRunSettingsProjectDetailsLookup, addFCCRunSettingsInvocation.Arguments[2]);
        }

        private string GetFCCMsTestAdapterPath()
        {
            autoMocker.GetMock<IToolUnzipper>()
                .Setup(
                    toolFolder => toolFolder.EnsureUnzipped(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<CancellationToken>())
                )
                .Returns("ZipDestination");

            msCodeCoverageRunSettingsService.Initialize(null, null, CancellationToken.None);
            return Path.Combine("ZipDestination", "build", "netstandard2.0");
        }
    
    }
}
