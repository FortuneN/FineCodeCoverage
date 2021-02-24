using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Model;

namespace FineCodeCoverage.Engine.Coverlet
{

    [Export(typeof(ICoverletUtil))]
    internal class CoverletUtil:ICoverletUtil
	{
        private readonly ICoverletDataCollectorUtil coverletDataCollectorUtil;
        private readonly ICoverletGlobalUtil coverletGlobalUtil;

        [ImportingConstructor]
		public CoverletUtil(ICoverletDataCollectorUtil coverletDataCollectorUtil, ICoverletGlobalUtil coverletGlobalUtil)
        {
            this.coverletDataCollectorUtil = coverletDataCollectorUtil;
            this.coverletGlobalUtil = coverletGlobalUtil;
        }
		public void Initialize(string appDataFolder)
		{
			coverletGlobalUtil.Initialize(appDataFolder);
			coverletDataCollectorUtil.Initialize(appDataFolder);
		}

		
		public Task<bool> RunCoverletAsync(ICoverageProject project, bool throwError = false)
		{
            if (coverletDataCollectorUtil.CanUseDataCollector(project))
            {
				return coverletDataCollectorUtil.RunAsync(throwError);
            }
			return coverletGlobalUtil.RunAsync(project, throwError);
		}
	}
}
