using Moq;
using NUnit.Framework;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using AutoMoq;
using System.Threading;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Impl;
using FineCodeCoverage.Engine;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using FineCodeCoverageTests.Test_helpers;
using FineCodeCoverage.Engine.ReportGenerator;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverageRunSettingsService_Collect_Tests
    {
        private AutoMoqer autoMocker;

        [Test]
        public async Task Should_FCCEngine_RunAndProcessReport_With_CoberturaResults()
        {
            var resultsUris = new List<Uri>()
            {
                new Uri(@"C:\SomePath\result1.cobertura.xml", UriKind.Absolute),
                new Uri(@"C:\SomePath\result2.cobertura.xml", UriKind.Absolute),
                new Uri(@"C:\SomePath\result3.xml", UriKind.Absolute),
            };

            var expectedCoberturaFiles = new string[] { @"C:\SomePath\result1.cobertura.xml", @"C:\SomePath\result2.cobertura.xml" };
            await RunAndProcessReportAsync(resultsUris, expectedCoberturaFiles);
        }

        [Test]
        public async Task Should_Not_Throw_If_No_Results()
        {
            await RunAndProcessReportAsync(null, Array.Empty<string>());
        }

        [Test]
        public async Task Should_Combined_Log_When_No_Cobertura_Files()
        {
            await RunAndProcessReportAsync(null, Array.Empty<string>());
            autoMocker.Verify<ILogger>(logger => logger.Log("No cobertura files for ms code coverage."));
            autoMocker.Verify<IReportGeneratorUtil>(
                reportGenerator => reportGenerator.LogCoverageProcess("No cobertura files for ms code coverage.")
            );

        }

        private async Task RunAndProcessReportAsync(IEnumerable<Uri> resultsUris,string[] expectedCoberturaFiles)
        {
            autoMocker = new AutoMoqer();
            var mockToolFolder = autoMocker.GetMock<IToolFolder>();
            mockToolFolder.Setup(tf => tf.EnsureUnzipped(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ZipDetails>(), 
                It.IsAny<CancellationToken>()
            )).Returns("ZipDestination");
            
            var msCodeCoverageRunSettingsService = autoMocker.Create<MsCodeCoverageRunSettingsService>();
            msCodeCoverageRunSettingsService.threadHelper = new TestThreadHelper();

            var mockFccEngine = new Mock<IFCCEngine>();
            msCodeCoverageRunSettingsService.Initialize("", mockFccEngine.Object, CancellationToken.None);

            var mockOperation = new Mock<IOperation>();
            mockOperation.Setup(operation => operation.GetRunSettingsDataCollectorResultUri(new Uri(RunSettingsHelper.MsDataCollectorUri))).Returns(resultsUris);

            var mockTestOperation = new Mock<ITestOperation>();
            mockTestOperation.Setup(testOperation => testOperation.GetCoverageProjectsAsync()).ReturnsAsync(new List<ICoverageProject>());
            await msCodeCoverageRunSettingsService.CollectAsync(mockOperation.Object, mockTestOperation.Object);
            
            mockFccEngine.Verify(engine => engine.RunAndProcessReport(
                    It.Is<string[]>(coberturaFiles => !expectedCoberturaFiles.Except(coberturaFiles).Any() && !coberturaFiles.Except(expectedCoberturaFiles).Any()), It.IsAny<Action>()
                )
            );
        }
    }
}
