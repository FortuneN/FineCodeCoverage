using AutoMoq;
using FineCodeCoverage.Core.Initialization;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverageTests
{
    internal class PackageLoader_Tests
    {
        private AutoMoqer mocker;
        private PackageLoader packageLoader;

        [SetUp]
        public void SetUp()
        {
            mocker = new AutoMoqer();
            packageLoader = mocker.Create<PackageLoader>();
        }



        [Test]
#pragma warning disable VSTHRD200 // Use "Async" suffix for async methods
        public void Should_Not_Be_InitializedFromTestContainerDiscoverer_If_Not_LoadPackageAsync()
#pragma warning restore VSTHRD200 // Use "Async" suffix for async methods
        {
            Assert.That(packageLoader.InitializedFromTestContainerDiscoverer, Is.False);
        }

        [Test]
        public async Task Should_Be_InitializedFromTestContainerDiscoverer_If_LoadPackageAsync()
        {
            await packageLoader.LoadPackageAsync(CancellationToken.None);
            Assert.That(packageLoader.InitializedFromTestContainerDiscoverer, Is.True);
        }

        [Test]
        public async Task It_Should_Load_The_Package_If_LoadPackageAsync()
        {
            await packageLoader.LoadPackageAsync(CancellationToken.None);

            mocker.Verify<IShellPackageLoader>(x => x.LoadPackageAsync());
        }
    }
}
