using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;

namespace FineCodeCoverage.Engine
{
    [Export(typeof(ICoverageUtilManager))]
    internal class CoverageUtilManager : ICoverageUtilManager
    {
        private readonly IOpenCoverUtil openCoverUtil;
        private readonly ICoverletUtil coverletUtil;

        [ImportingConstructor]
        public CoverageUtilManager(IOpenCoverUtil openCoverUtil, ICoverletUtil coverletUtil)
        {
            this.openCoverUtil = openCoverUtil;
            this.coverletUtil = coverletUtil;
        }
        
        public void Initialize(string appDataFolder)
        {
            openCoverUtil.Initialize(appDataFolder);
            coverletUtil.Initialize(appDataFolder);
        }

        public Task<bool> RunCoverageAsync(ICoverageProject project, bool throwError = false)
        {
            if (project.IsDotNetSdkStyle())
            {
                return coverletUtil.RunCoverletAsync(project, throwError);
            }
            else
            {
                return openCoverUtil.RunOpenCoverAsync(project, throwError);
            }
        }
    }
}
