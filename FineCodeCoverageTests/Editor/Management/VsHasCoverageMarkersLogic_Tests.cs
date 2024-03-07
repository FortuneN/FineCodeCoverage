using AutoMoq;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Options;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class VsHasCoverageMarkersLogic_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_HasCoverageMarkers_When_External_Markers_Are_Not_In_The_Store(bool inTheStore)
        {
            var autoMoqer = new AutoMoqer();
            var mockReadOnlyConfigSettingsStoreProvider = autoMoqer.GetMock<IReadOnlyConfigSettingsStoreProvider>();
            var vsMarkerCollectionPath = @"Text Editor\External Markers\{b4ee9ead-e105-11d7-8a44-00065bbd20a4}";
            mockReadOnlyConfigSettingsStoreProvider.Setup(readOnlyConfigSettingsStoreProvider => 
                readOnlyConfigSettingsStoreProvider.Provide().CollectionExists(vsMarkerCollectionPath)).Returns(inTheStore);
            
            var vsHasCoverageMarkersLogic = autoMoqer.Create<VsHasCoverageMarkersLogic>();

            Assert.That(vsHasCoverageMarkersLogic.HasCoverageMarkers(), Is.EqualTo(inTheStore));
        }
    }
}