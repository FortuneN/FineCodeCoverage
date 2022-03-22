using NUnit.Framework;
using AutoMoq;
using FineCodeCoverage.Engine.MsTestPlatform.CodeCoverage;
using System.Threading;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverageTests.MsCodeCoverage
{
    internal class MsCodeCoverageRunSettingsService_Initialize_Tests
    {
        [Test]
        public void Should_Ensure_Microsoft_CodeCoverage_Is_Unzipped_To_The_Tool_Folder()
        {
            var autoMocker = new AutoMoqer();
            var msCodeCoverageRunSettingsService  = autoMocker.Create<MsCodeCoverageRunSettingsService>();

            var cancellationToken = CancellationToken.None;

            var zipDetails = new ZipDetails();
            var mockToolZipProvider = autoMocker.GetMock<IToolZipProvider>();
            mockToolZipProvider.Setup(toolZipProvider => toolZipProvider.ProvideZip("microsoft.codecoverage")).Returns(zipDetails);

            var mockToolFolder = autoMocker.GetMock<IToolFolder>();
            mockToolFolder.Setup(toolFolder => 
                toolFolder.EnsureUnzipped("AppDataFolder", "msCodeCoverage", zipDetails, cancellationToken)).Returns("ZipDestination");
            
            msCodeCoverageRunSettingsService.Initialize("AppDataFolder", null, cancellationToken);
            mockToolFolder.VerifyAll();
        }
    }
}
