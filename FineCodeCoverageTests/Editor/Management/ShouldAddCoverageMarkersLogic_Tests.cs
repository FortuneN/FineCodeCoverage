using AutoMoq;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Options;
using NUnit.Framework;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class ShouldAddCoverageMarkersLogic_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void Should_Add_If_The_VS_Provided_External_Markers_Are_Not_In_The_Store(bool inTheStore)
        {
            var autoMoqer = new AutoMoqer();
            var mockReadOnlyConfigSettingsStoreProvider = autoMoqer.GetMock<IReadOnlyConfigSettingsStoreProvider>();
            var vsMarkerCollectionPath = @"Text Editor\External Markers\{b4ee9ead-e105-11d7-8a44-00065bbd20a4}";
            mockReadOnlyConfigSettingsStoreProvider.Setup(readOnlyConfigSettingsStoreProvider => readOnlyConfigSettingsStoreProvider.Provide().CollectionExists(vsMarkerCollectionPath)).Returns(inTheStore);
            
            var shouldAddCoverageMarkersLogic = autoMoqer.Create<ShouldAddCoverageMarkersLogic>();

            Assert.That(shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers(), Is.EqualTo(!inTheStore));
        }
    }
}