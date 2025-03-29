using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{

    [Export(typeof(ISolutionProjectsProvider))]
    internal class SolutionProjectsProvider : ISolutionProjectsProvider
    {
        private readonly IServiceProvider serviceProvider;

        [ImportingConstructor]
        public SolutionProjectsProvider(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
        )
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<List<IVsHierarchy>> GetLoadedProjectsAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            return GetProjects(vsSolution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);
        }

        public async Task<bool> IsSolutionOpenAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(vsSolution);
            vsSolution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isSolutionOpen);
            return (bool)isSolutionOpen;
        }

        private List<IVsHierarchy> GetProjects(IVsSolution vsSolution, __VSENUMPROJFLAGS flags)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var projects = new List<IVsHierarchy>();
            var result = vsSolution.GetProjectEnum((uint)flags, Guid.Empty, out var enumHierarchies);
            if (result == VSConstants.S_OK)
            {
                IVsHierarchy[] rgelt = new IVsHierarchy[1];
                uint fetched = 0;
                while (enumHierarchies.Next(1, rgelt, out fetched) == VSConstants.S_OK && fetched > 0)
                {
                    int hr = rgelt[0].GetGuidProperty(
                        VSConstants.VSITEMID_ROOT,
                        (int)__VSHPROPID.VSHPROPID_TypeGuid,
                        out var typeGuid
                    );

                    if (typeGuid != VSConstants.GUID_ItemType_VirtualFolder)
                    {
                        projects.Add(rgelt[0]);
                    }
                }
            }
            return projects;
        }
    }

}
