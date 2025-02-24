using FineCodeCoverage.Options;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Engine.MsTestPlatform.TestingPlatform
{
    [Export(typeof(IUseTestingPlatformProtocolFeatureService))]
    internal class UseTestingPlatformProtocolFeatureService : IUseTestingPlatformProtocolFeatureService
    {
        private readonly IReadOnlyUserSettingsStoreProvider readableUserSettingsStoreProvider;

        [ImportingConstructor]
        public UseTestingPlatformProtocolFeatureService(
            IReadOnlyUserSettingsStoreProvider readableUserSettingsStoreProvider
        )
        {
            this.readableUserSettingsStoreProvider = readableUserSettingsStoreProvider;
        }
        public async System.Threading.Tasks.Task<bool?> GetAsync()
        {
            var store = await readableUserSettingsStoreProvider.ProvideAsync();

            try
            {
                var value = store.GetInt32("FeatureFlags\\TestingTools\\UnitTesting\\UseTestingPlatformProtocol", "Value");
                return value == 1;
            }
            catch
            {
                return null;
            }
        }
    }
}
