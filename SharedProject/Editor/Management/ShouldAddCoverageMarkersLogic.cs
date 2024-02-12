using FineCodeCoverage.Options;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IShouldAddCoverageMarkersLogic))]
    class ShouldAddCoverageMarkersLogic : IShouldAddCoverageMarkersLogic
    {
        private readonly IReadOnlyConfigSettingsStoreProvider readOnlyConfigSettingsStoreProvider;

        [ImportingConstructor]
        public ShouldAddCoverageMarkersLogic(
            IReadOnlyConfigSettingsStoreProvider readOnlyConfigSettingsStoreProvider
        )
        {
            this.readOnlyConfigSettingsStoreProvider = readOnlyConfigSettingsStoreProvider;
        }

        public bool ShouldAddCoverageMarkers()
        {
            var readOnlySettingsStore = readOnlyConfigSettingsStoreProvider.Provide();
            return  !readOnlySettingsStore.CollectionExists(@"Text Editor\External Markers\{b4ee9ead-e105-11d7-8a44-00065bbd20a4}");
        }
    }

}
