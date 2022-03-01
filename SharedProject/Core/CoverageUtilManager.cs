using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using FineCodeCoverage.Engine.Coverlet;
using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.OpenCover;
using FineCodeCoverage.Engine.ReportGenerator;

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

        public string CoverageToolName(ICoverageProject project)
        {
            return project.IsDotNetSdkStyle() ? "Coverlet" : "OpenCover";
        }

        public void Initialize(string appDataFolder)
        {
            openCoverUtil.Initialize(appDataFolder);
            coverletUtil.Initialize(appDataFolder);
        }

        public async Task<bool> RunCoverageAsync(ICoverageProject project, bool throwError = false)
        {
            bool result;
            if (project.IsDotNetSdkStyle())
            {
                result = await coverletUtil.RunCoverletAsync(project, throwError);
            }
            else
            {
                result = await openCoverUtil.RunOpenCoverAsync(project, throwError);
            }
            return result;
        }
    }
}
