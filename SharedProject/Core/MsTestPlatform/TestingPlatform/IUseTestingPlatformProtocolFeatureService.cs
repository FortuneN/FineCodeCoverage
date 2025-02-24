using System.Threading.Tasks;

namespace FineCodeCoverage.Engine.MsTestPlatform.TestingPlatform
{
    internal interface IUseTestingPlatformProtocolFeatureService
    {
        Task<bool?> GetAsync();
    }

}
