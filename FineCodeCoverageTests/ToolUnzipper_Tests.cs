using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverageTests
{
    internal class ToolUnzipper_Tests
    {
        [Test]
        public void Should_Delegate_To_ToolFolder_With_the_Provided_ZipDetails()
        {
            var mocker = new AutoMoqer();
            var toolUnzipper = mocker.Create<ToolUnzipper>();
            var zipDetails = new ZipDetails { Path = "path", Version = "version" };
            mocker.Setup<IToolZipProvider,ZipDetails>(toolZipProvider => toolZipProvider.ProvideZip("zipPrefix")).Returns(zipDetails);

            var ct = CancellationToken.None;
            toolUnzipper.EnsureUnzipped("appDataFolder", "ownFolderName", "zipPrefix", ct);

            mocker.Verify<IToolFolder>(toolFolder => toolFolder.EnsureUnzipped("appDataFolder", "ownFolderName", zipDetails, ct));
        }
    }
}
