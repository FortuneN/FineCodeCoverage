using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IVsHasCoverageMarkersLogic))]
    internal class VsHasCoverageMarkersLogic : IVsHasCoverageMarkersLogic
    {
        private readonly IReadOnlyConfigSettingsStoreProvider readOnlyConfigSettingsStoreProvider;

        [ImportingConstructor]
        public VsHasCoverageMarkersLogic(
            IReadOnlyConfigSettingsStoreProvider readOnlyConfigSettingsStoreProvider
        ) => this.readOnlyConfigSettingsStoreProvider = readOnlyConfigSettingsStoreProvider;

        public bool HasCoverageMarkers()
        {
            Microsoft.VisualStudio.Settings.SettingsStore readOnlySettingsStore = this.readOnlyConfigSettingsStoreProvider.Provide();
            return readOnlySettingsStore.CollectionExists(@"Text Editor\External Markers\{b4ee9ead-e105-11d7-8a44-00065bbd20a4}");
        }
    }
}
