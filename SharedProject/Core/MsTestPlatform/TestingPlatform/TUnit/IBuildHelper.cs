using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal class BuildStartEndArgs
    {
        public BuildStartEndArgs(bool isStart)
        {
            IsStart = isStart;
        }

        public bool IsStart { get; }
    }
    internal interface IBuildHelper
    {
        event EventHandler<BuildStartEndArgs> ExternalBuildEvent;
        Task<bool> BuildAsync(List<IVsHierarchy> projects, System.Threading.CancellationToken cancellationToken);
    }
}
